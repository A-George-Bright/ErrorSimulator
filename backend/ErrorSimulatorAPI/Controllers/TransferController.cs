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
            _logger.LogInformation("API TRANSFER REQUEST: {TxnId}", request.TransactionId);

            var result = await _service.TransferAsync(request);

            // 🔁 Handle responses properly
            switch (result.Status)
            {
                case "SUCCESS":
                    return Ok(result);

                case "DUPLICATE":
                    _logger.LogWarning("API DUPLICATE: {TxnId}", request.TransactionId);
                    return Conflict(result); // 409

                case "TIMEOUT":
                    _logger.LogWarning("API TIMEOUT: {TxnId}", request.TransactionId);
                    return StatusCode(408, result); // 408

                case "FAILED":
                default:
                    _logger.LogError("API FAILED: {TxnId}", request.TransactionId);
                    return StatusCode(500, result);
            }
        }
    }
}