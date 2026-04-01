using ErrorSimulatorAPI.DTOs;
using ErrorSimulatorAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErrorSimulatorAPI.Controllers
{
    [ApiController]
    [Route("api/transfer")]
    public class TransferController : ControllerBase
    {
        private readonly ITransferService _service;
        private readonly ILogger<TransferController> _logger;

        public TransferController(ITransferService service, ILogger<TransferController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferRequest request)
        {
            _logger.LogInformation(
                "API TRANSFER REQUEST: {From} → {To} | Amount: {Amount}",
                request.FromAccountNumber, request.ToAccountNumber, request.Amount);

            var result = await _service.TransferAsync(request);

            // 🔁 Handle responses properly
            switch (result.Status)
            {
                case "SUCCESS":
                    return Ok(result);

                case "DUPLICATE":
                    _logger.LogWarning("API DUPLICATE: {From} → {To}", request.FromAccountNumber, request.ToAccountNumber);
                    return Conflict(result); // 409

                case "TIMEOUT":
                    _logger.LogWarning("API TIMEOUT: {From} → {To}", request.FromAccountNumber, request.ToAccountNumber);
                    return StatusCode(408, result); // 408

                case "FAILED":
                default:
                    _logger.LogError("API FAILED: {From} → {To} | Reason: {Reason}", request.FromAccountNumber, request.ToAccountNumber, result.FailureReason);
                    return StatusCode(500, result);
            }
        }
    }
}