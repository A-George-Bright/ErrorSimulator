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
            // Server-generated ID — never exposed to the caller
            var txnId = Guid.NewGuid();
            string failureCause = "";

            _logger.LogInformation(
                "Txn START: {TxnId} | {From} → {To} | Amount: {Amount}",
                txnId, request.FromAccountNumber, request.ToAccountNumber, request.Amount);

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var response = new TransferResponse { Timestamp = DateTime.UtcNow };

                    // 🔥 CPU → FAIL
                    if (_simulation.IsCpuRunning)
                    {
                        failureCause = "High CPU load";
                        _logger.LogWarning("CPU HIT: {TxnId}", txnId);
                        throw new Exception("High CPU load");
                    }

                    // ⏱️ Slow → TIMEOUT
                    if (_simulation.IsSlowRunning)
                    {
                        failureCause = "System too slow";
                        throw new OperationCanceledException();
                    }

                    // 💥 DB FAILURE MODES
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

                    // 🔁 DUPLICATE — guards against Polly retry edge cases where
                    //    a prior attempt committed but threw before returning
                    if (await _db.Transactions.AnyAsync(x => x.TransactionId == txnId))
                    {
                        _logger.LogInformation("Txn DUPLICATE (retry guard): {TxnId}", txnId);
                        response.Success = false;
                        response.Status = "DUPLICATE";
                        response.Message = "Transaction already processed";
                        return response;
                    }

                    // 👤 USERS
                    var sender = await _db.Users.FirstOrDefaultAsync(
                        u => u.AccountNumber == request.FromAccountNumber);
                    var receiver = await _db.Users.FirstOrDefaultAsync(
                        u => u.AccountNumber == request.ToAccountNumber);

                    if (sender == null || receiver == null)
                        throw new Exception("Invalid account number");

                    if (!sender.IsActive || !receiver.IsActive)
                        throw new Exception("Account is inactive");

                    if (sender.Balance < request.Amount)
                        throw new Exception("Insufficient balance");

                    // 💸 PROCESS
                    sender.Balance -= request.Amount;
                    receiver.Balance += request.Amount;

                    var now = DateTime.UtcNow;
                    var reference = $"TXN-{now:yyyyMMdd}-{txnId.ToString("N")[..8].ToUpper()}";

                    _db.Transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = txnId,
                        Reference = reference,
                        FromUserId = sender.Id,
                        ToUserId = receiver.Id,
                        Amount = request.Amount,
                        Currency = sender.Currency,
                        Status = "SUCCESS",
                        CreatedAt = now,
                        CompletedAt = now
                    });

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _db.SaveChangesAsync(cts.Token);

                    _logger.LogInformation("Txn SUCCESS: {TxnId} | Ref: {Ref}", txnId, reference);

                    response.Success = true;
                    response.Status = "SUCCESS";
                    response.Message = "Transaction completed successfully";
                    response.Reference = reference;

                    return response;
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Txn TIMEOUT: {TxnId} | Reason: {Reason}", txnId, failureCause);

                return new TransferResponse
                {
                    Success = false,
                    Status = "TIMEOUT",
                    Message = "Transaction timed out — please try again",
                    FailureReason = failureCause,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                var reason = !string.IsNullOrEmpty(failureCause) ? failureCause : ex.Message;

                _logger.LogError("Txn FAILED: {TxnId} | Reason: {Reason}", txnId, reason);

                return new TransferResponse
                {
                    Success = false,
                    Status = "FAILED",
                    Message = "Transaction failed — please try again",
                    FailureReason = reason,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}
