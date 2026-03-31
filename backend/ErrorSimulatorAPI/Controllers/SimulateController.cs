using ErrorSimulatorAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErrorSimulatorAPI.Controllers
{
    [ApiController]
    [Route("api/simulate")]
    public class SimulateController : ControllerBase
    {
        private readonly SimulationService _service;
        private readonly ILogger<SimulateController> _logger;

        public SimulateController(SimulationService service, ILogger<SimulateController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // 🔥 CPU START
        [HttpPost("cpu")]
        public IActionResult Cpu()
        {
            _logger.LogInformation("SIM CPU START REQUEST");
            _service.HighCpu();
            return Ok(new { message = "CPU started" });
        }

        // 🔥 CPU STOP
        [HttpGet("cpu/stop")]
        public IActionResult StopCpu()
        {
            _logger.LogInformation("SIM CPU STOP REQUEST");
            _service.StopCpu();
            return Ok(new { message = "CPU stopped" });
        }

        // 🔥 CPU REDUCER
        [HttpPost("cpu/reduce")]
        public async Task<IActionResult> ReduceCpu()
        {
            _logger.LogInformation("SIM CPU REDUCER REQUEST");
            await _service.ReduceCpuGradually();
            return Ok(new { message = "CPU load reduced gradually" });
        }

        // 🐢 FIXED SLOW
        [HttpPost("slow")]
        public async Task<IActionResult> Slow()
        {
            _logger.LogInformation("SIM SLOW START");
            await _service.Slow();
            _logger.LogInformation("SIM SLOW END");
            return Ok(new { message = "Slow simulation completed" });
        }

        // 🎲 RANDOM SLOW (5–10 sec)
        [HttpGet("slow-random")]
        public async Task<IActionResult> SlowRandom()
        {
            var delay = new Random().Next(5000, 10000);

            _logger.LogInformation("SIM SLOW RANDOM START | Delay: {Delay}ms", delay);

            await Task.Delay(delay);

            _logger.LogInformation("SIM SLOW RANDOM END | Delay: {Delay}ms", delay);

            return Ok(new { message = $"Delayed {delay} ms" });
        }

        // 💥 DB HARD DOWN
        [HttpPost("db/down")]
        public IActionResult DbDown()
        {
            _logger.LogError("SIM DB HARD DOWN REQUEST");
            _service.DbDown();
            return Ok(new { message = "DB is down" });
        }

        // ⏱️ DB TIMEOUT
        [HttpPost("db/timeout")]
        public IActionResult DbTimeout()
        {
            _logger.LogError("SIM DB TIMEOUT REQUEST");
            _service.DbTimeout();
            return Ok(new { message = "DB timeout enabled" });
        }

        // 🎲 DB INTERMITTENT
        [HttpPost("db/intermittent")]
        public IActionResult DbIntermittent()
        {
            _logger.LogWarning("SIM DB INTERMITTENT REQUEST");
            _service.DbIntermittent();
            return Ok(new { message = "DB intermittent mode enabled" });
        }

        // 🔁 DB RESET
        [HttpPost("db/reset")]
        public IActionResult ResetDb()
        {
            _logger.LogInformation("SIM DB RESET REQUEST");
            _service.ResetDb();
            return Ok(new { message = "DB reset to normal" });
        }

        // ❌ MANUAL EXCEPTION
        [HttpPost("exception")]
        public IActionResult Exception()
        {
            _logger.LogError("SIM MANUAL EXCEPTION");
            return StatusCode(500, "Manual exception triggered");
        }

        // 🔥 STACK SIMULATION
        [HttpGet("stack")]
        public IActionResult StackSimulation()
        {
            try
            {
                _logger.LogInformation("SIM STACK START");

                SimulateStack(0);

                _logger.LogInformation("SIM STACK COMPLETED");

                return Ok(new { message = "Stack simulation completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError("SIM STACK FAILED | Reason: {Reason}", ex.Message);
                return StatusCode(500, "Stack overflow simulated");
            }
        }

        private void SimulateStack(int depth)
        {
            if (depth % 2000 == 0)
            {
                _logger.LogInformation("STACK DEPTH: {Depth}", depth);
            }

            if (depth > 10000) return;

            SimulateStack(depth + 1);
        }

        // 📊 SYSTEM STATS
        [HttpGet("stats")]
        public IActionResult Stats()
        {
            var data = _service.GetSystemStats();
            return Ok(data);
        }

        // 🧠 MEMORY START
        [HttpGet("memory/start")]
        public IActionResult StartMemory()
        {
            _logger.LogInformation("SIM MEMORY START");
            MemoryManager.Allocate2GB();
            return Ok(new { message = "Memory allocated" });
        }

        // 🧠 MEMORY STOP
        [HttpGet("memory/stop")]
        public IActionResult StopMemory()
        {
            _logger.LogInformation("SIM MEMORY STOP");
            MemoryManager.ReleaseAll();
            return Ok(new { message = "Memory released" });
        }

        // 🔁 RESET ALL
        [HttpGet("stop-all")]
        public IActionResult StopAll()
        {
            _logger.LogInformation("SIM RESET REQUEST");
            _service.StopAll();
            return Ok(new { message = "All simulations stopped" });
        }
    }
}