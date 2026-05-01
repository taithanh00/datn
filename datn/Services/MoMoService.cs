using System.Security.Cryptography;
using System.Text;
using datn.Models;
using System.Text.Json;

namespace datn.Services
{
    public class MoMoService : IMoMoService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public MoMoService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<string> CreatePaymentAsync(Tuition tuition, decimal amount, string orderInfo)
        {
            var momoSettings = _config.GetSection("MoMo");
            var partnerCode = momoSettings["PartnerCode"];
            var accessKey = momoSettings["AccessKey"];
            var secretKey = momoSettings["SecretKey"];
            var paymentUrl = momoSettings["PaymentUrl"];
            var returnUrl = momoSettings["ReturnUrl"];
            var ipnUrl = momoSettings["IpnUrl"];

            string amountStr = ((long)amount).ToString();
            string orderId = $"TUITION_{tuition.Id}_{DateTime.UtcNow.Ticks}";
            string requestId = Guid.NewGuid().ToString();
            string extraData = $"TuitionId={tuition.Id}";

            // Raw hash string format: accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet
            string rawHash = $"accessKey={accessKey}&amount={amountStr}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
            
            string signature = ComputeHmacSha256(rawHash, secretKey);

            var requestData = new
            {
                partnerCode = partnerCode,
                partnerName = "SenHồng",
                storeId = "MomoTestStore",
                requestId = requestId,
                amount = (long)amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = "captureWallet",
                signature = signature
            };

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(paymentUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (responseData.TryGetProperty("payUrl", out var payUrl))
            {
                return payUrl.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        public bool ValidateSignature(string signature, string rawHash)
        {
            var secretKey = _config["MoMo:SecretKey"];
            string myChecksum = ComputeHmacSha256(rawHash, secretKey);
            return myChecksum.Equals(signature, StringComparison.InvariantCultureIgnoreCase);
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
