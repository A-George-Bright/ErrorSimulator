namespace ErrorSimulatorAPI.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public User FromUser { get; set; } = null!;
        public User ToUser { get; set; } = null!;
    }
}
