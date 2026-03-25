using ErrorSimulatorAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErrorSimulatorAPI.Controllers
{


    [ApiController]
    [Route("api/simulate")]
    public class SimulateController : ControllerBase
    {
        private readonly SimulationService _service;

        public SimulateController(SimulationService service)
        {
            _service = service;
        }

        [HttpPost("cpu")]
        public IActionResult Cpu()
        {
            _service.HighCpu();
            _service.Log("CPU", "Started");
            return Ok(new { message = "CPU started" });
        }

        [HttpPost("memory")]
        public IActionResult Memory()
        {
            _service.MemoryLeak();
            _service.Log("Memory", "Started");
            return Ok(new { message = "Memory started" });
        }

        [HttpPost("slow")]
        public async Task<IActionResult> Slow()
        {
            _service.Log("Slow", "Started");

            await _service.Slow();

            _service.Log("Slow", "Completed");

            return Ok(new { message = "Slow done" });
        }

        [HttpPost("db")]
        public IActionResult Db()
        {
            try
            {
                _service.DbFailure();
            }
            catch (Exception ex)
            {
                _service.Log("DB", ex.Message);
                return StatusCode(500, ex.Message);
            }

            return Ok();
        }

        [HttpPost("exception")]
        public IActionResult Exception()
        {
            return StatusCode(500, "Manual exception triggered");
        }
    }
}

