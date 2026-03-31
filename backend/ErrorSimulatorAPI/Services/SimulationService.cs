namespace ErrorSimulatorAPI.Services
{
    using System.Diagnostics;
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

        public DbFailureMode CurrentDbFailure { get; private set; } = DbFailureMode.None;
        public bool IsSlowRunning { get; private set; }
        public bool IsCpuRunning { get; private set; }

        public SimulationService(IConfiguration config, ILogger<SimulationService> logger)
        {
            _logger = logger;
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

        // 📊 STATS
        public object GetSystemStats()
        {
            using var proc = Process.GetCurrentProcess();

            long workingSet = proc.WorkingSet64;

            double processCpuPercent = 0;

            try
            {
                var startCpu = proc.TotalProcessorTime;
                var start = DateTime.UtcNow;

                Thread.Sleep(500);

                using var proc2 = Process.GetCurrentProcess();
                var endCpu = proc2.TotalProcessorTime;

                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

                if (elapsed > 0)
                {
                    processCpuPercent = (endCpu - startCpu).TotalMilliseconds
                        / elapsed / Environment.ProcessorCount * 100.0;
                }
            }
            catch { }

            return new
            {
                processCpuPercent = Math.Round(processCpuPercent, 2),
                ramAvailableMb = Math.Round(workingSet / (1024.0 * 1024.0), 2)
            };
        }
    }
}