namespace ErrorSimulatorAPI.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Balance { get; set; }
        public decimal ReservedBalance { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
