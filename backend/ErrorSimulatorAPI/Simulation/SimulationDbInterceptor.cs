using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using ErrorSimulatorAPI.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ErrorSimulatorAPI.Simulation
{
    /// <summary>
    /// EF Core interceptor that fires on every DB command.
    /// System pressure (CPU, memory, DB state) is applied here so that
    /// failures come from the real ADO.NET / MySQL layer — no manual throws.
    /// </summary>
    public class SimulationDbInterceptor : DbCommandInterceptor
    {
        private readonly SimulationService _simulation;
        private readonly ILogger<SimulationDbInterceptor> _logger;
        private readonly Random _random = new();

        public SimulationDbInterceptor(
            SimulationService simulation,
            ILogger<SimulationDbInterceptor> logger)
        {
            _simulation = simulation;
            _logger = logger;
        }

        // ── SELECT queries (AnyAsync, FirstOrDefaultAsync, ToListAsync …) ──────
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await ApplyPressure(command, cancellationToken);
            return result; // let EF Core execute — real exception if connection was closed
        }

        // ── INSERT / UPDATE / DELETE (SaveChangesAsync) ───────────────────────
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await ApplyPressure(command, cancellationToken);
            return result;
        }

        // ── Scalar (COUNT, EXISTS …) ───────────────────────────────────────────
        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            await ApplyPressure(command, cancellationToken);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Core pressure logic — runs before every DB command execution
        // ─────────────────────────────────────────────────────────────────────
        private async Task ApplyPressure(DbCommand command, CancellationToken cancellationToken)
        {
            // ── 1. CPU HIGH LOAD ────────────────────────────────────────────────
            // Perform real SHA-256 CPU work before each DB command.
            // Under high CPU (spinning threads consuming all cores), this loop takes
            // significantly longer → DB operations slow down naturally → eventual timeout.
            // No delay is injected — it's purely a CPU competition effect.
            if (_simulation.IsCpuRunning)
            {
                _logger.LogDebug("Interceptor: CPU-bound pre-work (competing with CPU threads)");
                var sw = Stopwatch.StartNew();
                // ~500 ms of CPU work at normal load; much longer under saturated CPU
                while (sw.ElapsedMilliseconds < 500)
                {
                    _ = SHA256.HashData(Encoding.UTF8.GetBytes(command.CommandText));
                }
            }

            // ── 2. MEMORY PRESSURE ──────────────────────────────────────────────
            // With 2 GB allocated, forcing a full GC causes real stop-the-world pauses.
            // Under memory pressure the GC runs longer, blocking all managed threads
            // including the one executing the DB operation.
            if (MemoryManager.GetAllocatedChunks() > 0)
            {
                _logger.LogDebug("Interceptor: full GC under memory pressure");
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }

            // ── 3. DB HARD DOWN ─────────────────────────────────────────────────
            // Actually close the underlying MySQL connection before the command runs.
            // MySqlConnector / ADO.NET will throw the real exception when EF Core
            // tries to execute on the now-closed connection.
            // Exception type: MySqlConnector.MySqlException or InvalidOperationException
            if (_simulation.CurrentDbFailure == DbFailureMode.HardDown)
            {
                if (command.Connection?.State == ConnectionState.Open)
                {
                    _logger.LogWarning("Interceptor: closing connection (DB HARD DOWN)");
                    await command.Connection.CloseAsync();
                    // Return without suppressing → real exception from MySQL layer
                }
                return;
            }

            // ── 4. DB INTERMITTENT ──────────────────────────────────────────────
            // 50% of DB operations fail by physically closing the connection.
            // Remaining 50% succeed normally — simulates a flapping DB.
            if (_simulation.CurrentDbFailure == DbFailureMode.Intermittent
                && _random.Next(100) < 50)
            {
                if (command.Connection?.State == ConnectionState.Open)
                {
                    _logger.LogWarning("Interceptor: closing connection (DB INTERMITTENT)");
                    await command.Connection.CloseAsync();
                }
                return;
            }

            // ── 5. DB TIMEOUT / SLOW MODE ───────────────────────────────────────
            // Add a 12-second delay using the cancellation token passed from
            // SaveChangesAsync(ct) / query methods — which is our 10-second CTS.
            // After 10 s the CTS cancels → Task.Delay throws OperationCanceledException.
            // This is a REAL OperationCanceledException from Task.Delay, not a manual throw.
            if (_simulation.CurrentDbFailure == DbFailureMode.Timeout
                || _simulation.IsSlowRunning)
            {
                _logger.LogWarning("Interceptor: injecting 12s real delay (CTS will fire at 10s)");
                await Task.Delay(12_000, cancellationToken);
            }
        }
    }
}
