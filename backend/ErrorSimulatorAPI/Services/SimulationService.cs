namespace ErrorSimulatorAPI.Services
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    public enum DbFailureMode
    {
        None,
        HardDown,
        Timeout,
        Intermittent
    }

    public class SimulationService
    {
        private readonly ILogger<SimulationService> _logger;

        private CancellationTokenSource _cpuCts = new();
        private readonly List<Task> _cpuTasks = new();
        private readonly object _cpuLock = new();
        private int _cpuLoadPercent = 0;

        // System-wide CPU via PerformanceCounter (matches Task Manager)
        private readonly PerformanceCounter? _cpuCounter;
        private double _lastCpuReading = 0;
        private readonly Timer _cpuTimer;

        public DbFailureMode CurrentDbFailure { get; private set; } = DbFailureMode.None;
        public bool IsSlowRunning { get; private set; }
        public bool IsCpuRunning { get; private set; }

        // Native call for system memory info (matches Task Manager)
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public SimulationService(IConfiguration config, ILogger<SimulationService> logger)
        {
            _logger = logger;

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                // First call always returns 0, so prime it
                _cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not initialize CPU PerformanceCounter: {Message}", ex.Message);
            }

            // Update CPU reading every second in background (non-blocking)
            _cpuTimer = new Timer(_ =>
            {
                try
                {
                    if (_cpuCounter != null)
                        _lastCpuReading = _cpuCounter.NextValue();
                }
                catch { }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        // 🔥 CPU START
        public void HighCpu()
        {
            IsCpuRunning = true;
            _logger.LogInformation("SIM CPU STARTED | Load increasing");

            _cpuLoadPercent = Math.Min(100, _cpuLoadPercent + 40);

            if (_cpuTasks.Count > 0)
                return;

            _cpuCts.Cancel();

            lock (_cpuLock)
            {
                _cpuTasks.Clear();
            }

            _cpuCts = new CancellationTokenSource();
            int coreCount = Environment.ProcessorCount;
            var ct = _cpuCts.Token;

            for (int core = 0; core < coreCount; core++)
            {
                var task = Task.Factory.StartNew(() =>
                {
                    double result = 0;
                    var sw = Stopwatch.StartNew();

                    while (!ct.IsCancellationRequested)
                    {
                        int load = Interlocked.CompareExchange(ref _cpuLoadPercent, 0, 0);

                        if (load <= 0)
                        {
                            Thread.Sleep(250);
                            continue;
                        }

                        long busyMs = (long)Math.Max(1, 10 * load);
                        long idleMs = Math.Max(0, 1000 - busyMs);

                        sw.Restart();

                        while (sw.ElapsedMilliseconds < busyMs && !ct.IsCancellationRequested)
                        {
                            for (int i = 0; i < 5_000; i++)
                            {
                                result += Math.Sqrt(i);
                            }
                        }

                        if (idleMs > 0)
                        {
                            try
                            {
                                Task.Delay((int)idleMs, ct).Wait(ct);
                            }
                            catch { break; }
                        }
                    }
                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                lock (_cpuLock)
                {
                    _cpuTasks.Add(task);
                }
            }
        }

        // 🔥 CPU STOP
        public void StopCpu()
        {
            IsCpuRunning = false;
            _logger.LogInformation("SIM CPU STOPPED | System normalized");

            _cpuLoadPercent = 0;
            _cpuCts.Cancel();

            lock (_cpuLock)
            {
                _cpuTasks.Clear();
            }

            _cpuCts = new CancellationTokenSource();
        }

        // 🔥 CPU REDUCER
        public async Task ReduceCpuGradually()
        {
            _logger.LogInformation("SIM REDUCER START");

            while (_cpuLoadPercent > 0)
            {
                _cpuLoadPercent = Math.Max(0, _cpuLoadPercent - 10);

                _logger.LogInformation("SIM REDUCER STEP | Load: {Load}%", _cpuLoadPercent);

                await Task.Delay(1000);
            }

            IsCpuRunning = false;

            _logger.LogInformation("SIM REDUCER COMPLETED");
        }

        // 🔥 STOP ALL
        public void StopAll()
        {
            StopCpu();

            CurrentDbFailure = DbFailureMode.None;
            IsSlowRunning = false;
            IsCpuRunning = false;

            _logger.LogInformation("SIM RESET | All simulations stopped");

            ErrorSimulatorAPI.MemoryManager.ReleaseAll();
        }

        // ⏱️ Slow
        public async Task Slow()
        {
            IsSlowRunning = true;

            _logger.LogInformation("SIM SLOW START");

            await Task.Delay(15000);

            IsSlowRunning = false;

            _logger.LogInformation("SIM SLOW END");
        }

        // 💥 DB HARD DOWN
        public void DbDown()
        {
            CurrentDbFailure = DbFailureMode.HardDown;
            _logger.LogError("SIM DB HARD DOWN | Database unreachable");
        }

        // ⏱️ DB TIMEOUT
        public void DbTimeout()
        {
            CurrentDbFailure = DbFailureMode.Timeout;
            _logger.LogError("SIM DB TIMEOUT | Queries will timeout");
        }

        // 🎲 DB INTERMITTENT
        public void DbIntermittent()
        {
            CurrentDbFailure = DbFailureMode.Intermittent;
            _logger.LogWarning("SIM DB INTERMITTENT | Random failures");
        }

        // 🔁 DB RESET
        public void ResetDb()
        {
            CurrentDbFailure = DbFailureMode.None;
            _logger.LogInformation("SIM DB RESET | Connection restored");
        }

        // 📊 STATS (system-wide, matches Task Manager)
        public object GetSystemStats()
        {
            double systemCpu = Math.Round(_lastCpuReading, 1);

            // System memory via GlobalMemoryStatusEx (same source as Task Manager)
            double totalMemoryMb = 0;
            double usedMemoryMb = 0;
            uint memoryLoadPercent = 0;

            try
            {
                var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    totalMemoryMb = Math.Round(memStatus.ullTotalPhys / (1024.0 * 1024.0), 0);
                    double availMb = memStatus.ullAvailPhys / (1024.0 * 1024.0);
                    usedMemoryMb = Math.Round(totalMemoryMb - availMb, 0);
                    memoryLoadPercent = memStatus.dwMemoryLoad;
                }
            }
            catch { }

            return new
            {
                systemCpu,
                totalMemoryMb,
                usedMemoryMb,
                memoryLoadPercent
            };
        } 
    }
}