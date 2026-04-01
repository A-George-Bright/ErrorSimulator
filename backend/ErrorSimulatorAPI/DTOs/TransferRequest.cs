using System.ComponentModel.DataAnnotations;

namespace ErrorSimulatorAPI.DTOs
{
    public class TransferRequest
    {
        [Required]
        public string FromAccountNumber { get; set; } = string.Empty;

        [Required]
        public string ToAccountNumber { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}
