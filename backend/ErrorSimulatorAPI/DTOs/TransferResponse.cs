namespace ErrorSimulatorAPI.DTOs
{
    public class TransferResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
