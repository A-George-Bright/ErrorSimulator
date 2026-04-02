namespace ErrorSimulatorAPI.Simulation
{
    public class FailureSimulator
    {
        private readonly Random _random = new();

        // Realistic DB-level exception messages that mirror what production systems throw
        private static readonly string[] ConnectionErrors =
        [
            "Connection pool exhausted: too many connections (max_connections=151)",
            "Unable to connect to any of the specified MySQL hosts",
            "Lost connection to MySQL server during query",
            "MySQL server has gone away (error 2006)",
        ];

        public void Simulate()
        {
            var chance = _random.Next(1, 100);

            if (chance < 20)
            {
                var msg = ConnectionErrors[_random.Next(ConnectionErrors.Length)];
                throw new Microsoft.EntityFrameworkCore.DbUpdateException(msg);
            }

            if (chance < 40)
                Thread.Sleep(2000); // simulate slow query under mild load
        }
    }
}
