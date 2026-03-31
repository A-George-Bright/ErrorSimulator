using ErrorSimulatorAPI.DTOs;

namespace ErrorSimulatorAPI.Interfaces
{
    public interface ITransferService
    {
        Task<TransferResponse> TransferAsync(TransferRequest request);
    }
}
