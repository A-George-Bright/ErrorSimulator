namespace ErrorSimulatorAPI.Services

{

    using Microsoft.Data.SqlClient;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public class SimulationService
    {
        private readonly string _connectionString;
        private CancellationTokenSource _cpuCts = new();
        private readonly List<Task> _cpuTasks = new();
        private readonly object _cpuLock = new();
        private int _cpuLoadPercent = 0;

        public SimulationService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public void HighCpu()
        {
            // Each click increases load by 40%, capped at 100%
            _cpuLoadPercent = Math.Min(100, _cpuLoadPercent + 40);

            // If CPU workers are running, they use _cpuLoadPercent dynamically.
            // Otherwise, start them now.
            if (_cpuTasks.Count > 0)
                return;

            _cpuCts.Cancel();
            List<Task> previousTasks;
            lock (_cpuLock)
            {
                previousTasks = new List<Task>(_cpuTasks);
                _cpuTasks.Clear();
            }
            try
            {
                if (previousTasks.Count > 0)
                    Task.WaitAll(previousTasks.ToArray(), 1000);
            }
            catch { /* ignore */ }

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
                                result += Math.Sqrt(i) * Math.Sin(i) * Math.Cos(i);
                            }
                        }

                        if (ct.IsCancellationRequested)
                            break;

                        if (idleMs > 0)
                        {
                            try
                            {
                                Task.Delay((int)idleMs, ct).Wait(ct);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                        }
                    }

                    if (result == double.MinValue) Console.WriteLine(result);
                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                lock (_cpuLock)
                {
                    _cpuTasks.Add(task);
                }
            }
        }

        public void StopCpu()
        {
            _cpuLoadPercent = 0;
            // Cancel and wait briefly for worker threads to finish
            _cpuCts.Cancel();
            List<Task> tasksToWait;
            lock (_cpuLock)
            {
                tasksToWait = new List<Task>(_cpuTasks);
                _cpuTasks.Clear();
            }
            try
            {
                if (tasksToWait.Count > 0)
                    Task.WaitAll(tasksToWait.ToArray(), 2000);
            }
            catch { }
            // Create a new token source for future use
            _cpuCts = new CancellationTokenSource();
        }

        public void StopAll()
        {
            StopCpu();
            ErrorSimulatorAPI.MemoryManager.ReleaseAll();
        }

        public async Task Slow()
        {
            await Task.Delay(15000);
        }

        public void DbFailure()
        {
            var wrong = "Server=wrong;Database=fail;User Id=bad;Password=bad;";
            using var conn = new SqlConnection(wrong);
            conn.Open();
        }

        public void Log(string type, string message)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(
                "INSERT INTO SimulationLogs (Type, Message) VALUES (@t,@m)", conn);

            cmd.Parameters.AddWithValue("@t", type);
            cmd.Parameters.AddWithValue("@m", message);

            cmd.ExecuteNonQuery();
        }



        public object GetSystemStats()
        {
            // Managed heap
            long managedBytes = GC.GetTotalMemory(false);
            var gcInfo = GC.GetGCMemoryInfo();

            // Process-level memory
            using var proc = Process.GetCurrentProcess();
            long workingSet = proc.WorkingSet64;
            long privateBytes = proc.PrivateMemorySize64;
            long virtualBytes = proc.VirtualMemorySize64;

            // Process CPU percent sampled over a short interval to match Task Manager's view
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
                    // Normalize by processor count to get percentage similar to Task Manager
                    processCpuPercent = (endCpu - startCpu).TotalMilliseconds / elapsed / Environment.ProcessorCount * 100.0;
                }
            }
            catch
            {
                // ignore sampling errors
            }

            // System-level CPU and available RAM (best-effort)
            double systemCpu = 0;
            float ramAvailable = 0;
            try
            {
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                cpuCounter.NextValue();
                Thread.Sleep(500);
                systemCpu = cpuCounter.NextValue();
                ramAvailable = ramCounter.NextValue();
            }
            catch { }

            // MemoryManager allocations (each chunk is ~512 MB)
            int allocatedChunks = 0;
            try
            {
                allocatedChunks = ErrorSimulatorAPI.MemoryManager.GetAllocatedChunks();
            }
            catch { }

            const long ChunkBytes = 512L * 1024L * 1024L;

            return new
            {
                // Managed
                managedBytes,
                gcHeapSizeBytes = gcInfo.HeapSizeBytes,
                gcCommittedBytes = gcInfo.TotalCommittedBytes,

                // Process
                processWorkingSet = workingSet,
                processPrivateBytes = privateBytes,
                processVirtualBytes = virtualBytes,
                processCpuPercent = Math.Round(processCpuPercent, 2),

                // System
                systemCpu = Math.Round(systemCpu, 2),
                ramAvailableMb = Math.Round(ramAvailable, 2),

                // Simulator allocations
                allocatedChunks,
                allocatedBytesEstimated = allocatedChunks * ChunkBytes
            };
        }
    }
}
