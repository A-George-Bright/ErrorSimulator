namespace ErrorSimulatorAPI.Simulation
{
    public class FailureSimulator
    {
        private readonly Random _random = new();

        public void Simulate()
        {
            var chance = _random.Next(1, 100);

            if (chance < 20)
                throw new Exception("Simulated failure");

            if (chance < 40)
                Thread.Sleep(2000); // simulate delay
        }
    }
}
