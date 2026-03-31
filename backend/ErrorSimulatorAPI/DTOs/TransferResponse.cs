namespace ErrorSimulatorAPI.DTOs
{
    public class TransferResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
