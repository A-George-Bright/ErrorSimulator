using ErrorSimulatorAPI.DTOs;
using ErrorSimulatorAPI.Interfaces;
using ErrorSimulatorAPI.Models;
using ErrorSimulatorAPI.Services;
using ErrorSimulatorAPI.Simulation;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace ErrorSimulatorAPI.Services
{
    public class TransferService : ITransferService
    {
        private readonly AppDbContext _db;
        private readonly FailureSimulator _simulator;
        private readonly SimulationService _simulation;
        private readonly ILogger<TransferService> _logger;

        // 🔁 Retry (no retry for timeout)
        private static readonly AsyncPolicy _retryPolicy =
            Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200));

        public TransferService(
            AppDbContext db,
            FailureSimulator simulator,
            SimulationService simulation,
            ILogger<TransferService> logger)
        {
            _db = db;
            _simulator = simulator;
            _simulation = simulation;
            _logger = logger;
        }

        public async Task<TransferResponse> TransferAsync(TransferRequest request)
        {
            string failureCause = "";

            _logger.LogInformation("Txn START: {TxnId}", request.TransactionId);

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var response = new TransferResponse
                    {
                        TransactionId = request.TransactionId,
                        Timestamp = DateTime.UtcNow
                    };

                    // 🔥 CPU → FAIL
                    if (_simulation.IsCpuRunning)
                    {
                        failureCause = "High CPU load";
                        _logger.LogWarning("CPU HIT: {TxnId}", request.TransactionId);
                        throw new Exception("High CPU load");
                    }

                    // ⏱️ Slow → TIMEOUT
                    if (_simulation.IsSlowRunning)
                    {
                        failureCause = "System too slow";
                        throw new OperationCanceledException();
                    }

                    // 💥 DB FAILURE MODES (UPDATED 🔥)
                    switch (_simulation.CurrentDbFailure)
                    {
                        case DbFailureMode.HardDown:
                            failureCause = "DB hard down";
                            throw new DbUpdateException("Database unreachable");

                        case DbFailureMode.Timeout:
                            failureCause = "DB timeout";
                            await Task.Delay(3000);
                            throw new OperationCanceledException("Database timeout");

                        case DbFailureMode.Intermittent:
                            if (new Random().Next(1, 100) < 50)
                            {
                                failureCause = "DB intermittent failure";
                                throw new DbUpdateException("Random DB failure");
                            }
                            break;
                    }

                    // 🎲 Random failure
                    _simulator.Simulate();

                    // 🔁 DUPLICATE
                    if (await _db.Transactions.AnyAsync(x => x.TransactionId == request.TransactionId))
                    {
                        _logger.LogInformation("Txn DUPLICATE: {TxnId}", request.TransactionId);

                        response.Success = false;
                        response.Status = "DUPLICATE";
                        response.Message = "Duplicate transaction";
                        return response;
                    }

                    // 👤 USERS
                    var sender = await _db.Users.FindAsync(request.FromUserId);
                    var receiver = await _db.Users.FindAsync(request.ToUserId);

                    if (sender == null || receiver == null)
                        throw new Exception("Invalid users");

                    if (sender.Balance < request.Amount)
                        throw new Exception("Insufficient balance");

                    // 💸 PROCESS
                    sender.Balance -= request.Amount;
                    receiver.Balance += request.Amount;

                    _db.Transactions.Add(new Transaction
                    {
                        TransactionId = request.TransactionId,
                        Amount = request.Amount,
                        Status = "SUCCESS"
                    });

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _db.SaveChangesAsync(cts.Token);

                    _logger.LogInformation("Txn SUCCESS: {TxnId}", request.TransactionId);

                    response.Success = true;
                    response.Status = "SUCCESS";
                    response.Message = "Transaction completed";

                    return response;
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Txn TIMEOUT: {TxnId} | Reason: {Reason}",
                    request.TransactionId,
                    failureCause
                );

                return new TransferResponse
                {
                    Success = false,
                    Status = "TIMEOUT",
                    Message = $"Transaction timeout ({failureCause})",
                    TransactionId = request.TransactionId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                var reason = !string.IsNullOrEmpty(failureCause)
                    ? failureCause
                    : ex.Message;

                _logger.LogError(
                    "Txn FAILED: {TxnId} | Reason: {Reason}",
                    request.TransactionId,
                    reason
                );

                return new TransferResponse
                {
                    Success = false,
                    Status = "FAILED",
                    Message = $"Transaction failed ({reason})",
                    TransactionId = request.TransactionId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}