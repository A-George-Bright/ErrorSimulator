namespace ErrorSimulatorAPI.Services

{

    using Microsoft.Data.SqlClient;
    using System.Diagnostics;

    public class SimulationService
    {
        private readonly string _connectionString;

        public SimulationService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public void HighCpu()
        {
            Task.Run(() =>
            {
                for (int i = 0; i < 100000000; i++)
                {
                    var x = Math.Sqrt(i);
                }
            });
        }

        public void MemoryLeak()
        {
            Task.Run(() =>
            {
                List<byte[]> list = new();

                for (int i = 0; i < 50; i++)
                {
                    list.Add(new byte[5_000_000]);
                    Thread.Sleep(200);
                }
            });
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
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Warm up the CPU counter once; next value is sampled after a short delay.
            cpuCounter.NextValue();
            Thread.Sleep(800);

            float cpu = cpuCounter.NextValue();
            float ramAvailable = ramCounter.NextValue();

            return new
            {
                cpu = Math.Round(cpu, 2),
                ram = Math.Round(ramAvailable, 2)
            };
        }
    }
}
