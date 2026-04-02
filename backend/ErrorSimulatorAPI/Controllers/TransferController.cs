using System.Data.Common;
using ErrorSimulatorAPI.DTOs;
using ErrorSimulatorAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
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

            try
            {
                var result = await _service.TransferAsync(request);

                return result.Status switch
                {
                    "SUCCESS"   => Ok(result),
                    "DUPLICATE" => Conflict(result),           // 409
                    _           => StatusCode(500, result)
                };
            }
            // ── Real timeout from CancellationTokenSource (10 s) or DB delay ──
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("API TIMEOUT: {From} → {To} | {Msg}",
                    request.FromAccountNumber, request.ToAccountNumber, ex.Message);

                return StatusCode(408, new TransferResponse
                {
                    Success = false,
                    Status = "TIMEOUT",
                    Message = "Transaction failed due to timeout. Please try again.",
                    FailureReason = "Query execution timeout",
                    Timestamp = DateTime.UtcNow
                });
            }
            // ── Real EF Core / ADO.NET DB failure (connection closed, deadlock …) ──
            catch (DbException ex)
            {
                _logger.LogError("API DB ERROR: {From} → {To} | {Msg}",
                    request.FromAccountNumber, request.ToAccountNumber, ex.Message);

                return StatusCode(500, new TransferResponse
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Database unavailable. Please try after some time.",
                    FailureReason = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("API DB UPDATE ERROR: {From} → {To} | {Msg}",
                    request.FromAccountNumber, request.ToAccountNumber, ex.Message);

                return StatusCode(500, new TransferResponse
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Database unavailable. Please try after some time.",
                    FailureReason = ex.InnerException?.Message ?? ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            // ── Validation errors (bad account, inactive, insufficient balance) ─
            catch (ArgumentException ex)
            {
                _logger.LogWarning("API VALIDATION: {From} → {To} | {Msg}",
                    request.FromAccountNumber, request.ToAccountNumber, ex.Message);

                return BadRequest(new TransferResponse
                {
                    Success = false,
                    Status = "FAILED",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("API UNEXPECTED: {From} → {To} | {Msg}",
                    request.FromAccountNumber, request.ToAccountNumber, ex.Message);

                return StatusCode(500, new TransferResponse
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Transaction failed. Please try again.",
                    FailureReason = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
