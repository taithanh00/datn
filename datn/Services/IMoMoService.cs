using datn.Models;

namespace datn.Services
{
    public interface IMoMoService
    {
        Task<string> CreatePaymentAsync(Tuition tuition, decimal amount, string orderInfo);
        bool ValidateSignature(string signature, string rawHash);
    }
}
