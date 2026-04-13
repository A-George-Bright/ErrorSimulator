using ErrorSimulatorAPI.DTOs;
using ErrorSimulatorAPI.Interfaces;
using ErrorSimulatorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ErrorSimulatorAPI.Services
{
    public class TransferService : ITransferService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TransferService> _logger;

        public TransferService(AppDbContext db, ILogger<TransferService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<TransferResponse> TransferAsync(TransferRequest request)
        {
            var txnId = Guid.NewGuid();

            // 10-second hard timeout for the entire transaction.
            // If ANY DB command takes too long (due to CPU load, slow mode, DB timeout),
            // this CTS fires and Task.Delay in the interceptor throws OperationCanceledException.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var ct = cts.Token;

            _logger.LogInformation(
                "Txn START: {TxnId} | {From} → {To} | Amount: {Amount}",
                txnId, request.FromAccountNumber, request.ToAccountNumber, request.Amount);

            // ── Open a real EF Core DB transaction ─────────────────────────────
            // Ensures atomic commit/rollback — no partial updates under any failure.
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                // ── Duplicate guard ────────────────────────────────────────────
                // Prevents double-processing if a previous attempt committed but
                // threw before returning (e.g. network blip after commit).
                if (await _db.Transactions.AnyAsync(x => x.TransactionId == txnId, ct))
                {
                    _logger.LogInformation("Txn DUPLICATE (retry guard): {TxnId}", txnId);
                    await tx.RollbackAsync(CancellationToken.None);
                    return new TransferResponse
                    {
                        Success = false,
                        Status = "DUPLICATE",
                        Message = "Transaction already processed",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // ── Load accounts ──────────────────────────────────────────────
                var sender = await _db.Users.FirstOrDefaultAsync(
                    u => u.AccountNumber == request.FromAccountNumber, ct);
                var receiver = await _db.Users.FirstOrDefaultAsync(
                    u => u.AccountNumber == request.ToAccountNumber, ct);

                // ── Validate ───────────────────────────────────────────────────
                if (sender == null || receiver == null)
                    throw new ArgumentException("Invalid account number");

                if (!sender.IsActive || !receiver.IsActive)
                    throw new ArgumentException("Account is inactive");

                if (sender.Balance < request.Amount)
                    throw new ArgumentException("Insufficient balance");

                // ── Apply debit / credit ───────────────────────────────────────
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

                // ── Persist — interceptor fires here for every SQL command ─────
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(CancellationToken.None);

                _logger.LogInformation("Txn SUCCESS: {TxnId} | Ref: {Ref}", txnId, reference);

                return new TransferResponse
                {
                    Success = true,
                    Status = "SUCCESS",
                    Message = "Transaction completed successfully",
                    Reference = reference,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                // Always rollback — even if rollback itself fails (e.g. connection closed)
                try { await tx.RollbackAsync(CancellationToken.None); } catch { }
                throw;
            }
        }
    }
}
