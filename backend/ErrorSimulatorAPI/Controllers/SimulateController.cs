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

        [HttpGet("cpu/stop")]
        public IActionResult StopCpu()
        {
            _service.StopCpu();
            _service.Log("CPU", "Stopped");
            return Ok(new { message = "CPU stopped" });
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

        [HttpGet("stats")]
        public IActionResult Stats()
        {
            var data = _service.GetSystemStats();
            return Ok(data);
        }

        [HttpGet("memory/start")]
        public IActionResult StartMemory()
        {
            MemoryManager.Allocate2GB();
            return Ok(new { message = "Allocated 2GB memory" });
        }

        [HttpGet("memory/stop")]
        public IActionResult StopMemory()
        {
            MemoryManager.ReleaseAll();
            return Ok(new { message = "Memory released" });
        }

        [HttpGet("stop-all")]
        public IActionResult StopAll()
        {
            _service.StopAll();
            _service.Log("StopAll", "CPU and memory released");
            return Ok(new { message = "All simulation load stopped" });
        }
    }
}

