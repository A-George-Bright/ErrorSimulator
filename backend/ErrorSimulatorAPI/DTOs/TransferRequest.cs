namespace ErrorSimulatorAPI.DTOs
{
    public class TransferRequest
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
    }
}
