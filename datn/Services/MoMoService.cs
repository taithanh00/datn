using System.Security.Cryptography;
using System.Globalization;
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

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // Special Vietnamese "đ/Đ" → "d/D" (not handled by NFD decomposition)
            text = text.Replace("đ", "d").Replace("Đ", "D");
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public async Task<string> CreatePaymentAsync(Tuition tuition, decimal amount, string orderInfo)
        {
            var momoSettings = _config.GetSection("MoMo");
            var partnerCode = momoSettings["PartnerCode"]?.Trim();
            var accessKey = momoSettings["AccessKey"]?.Trim();
            var secretKey = momoSettings["SecretKey"]?.Trim();
            var paymentUrl = momoSettings["PaymentUrl"]?.Trim();
            var returnUrl = momoSettings["ReturnUrl"]?.Trim();
            var ipnUrl = momoSettings["IpnUrl"]?.Trim();

            // Normalize orderInfo to ASCII-only to prevent signature mismatch
            // caused by Vietnamese diacritic encoding differences (NFD/NFC)
            orderInfo = RemoveDiacritics(orderInfo);
            // Replace spaces with underscores to avoid URL encoding issues in the signature
            orderInfo = orderInfo.Replace(" ", "_");
            string amountStr = ((long)amount).ToString();
            string orderId = $"TUITION_{tuition.Id}_{DateTime.UtcNow.Ticks}";
            string requestId = Guid.NewGuid().ToString();
            // MoMo requires extraData to be base64 encoded
            string plainExtraData = $"TuitionId={tuition.Id}";
            string extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainExtraData));

            // Raw hash string format: accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet
            string rawHash = $"accessKey={accessKey}&amount={amountStr}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
            
            string signature = ComputeHmacSha256(rawHash, secretKey);

            System.Diagnostics.Debug.WriteLine("=== MOMO DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"accessKey: '{accessKey}'");
            System.Diagnostics.Debug.WriteLine($"secretKey: '{secretKey}'");
            System.Diagnostics.Debug.WriteLine($"rawHash: '{rawHash}'");
            System.Diagnostics.Debug.WriteLine($"signature: '{signature}'");
            System.Diagnostics.Debug.WriteLine("==================");

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
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"MoMo API Error ({response.StatusCode}): {responseString}");
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (responseData.TryGetProperty("payUrl", out var payUrl))
            {
                return payUrl.GetString() ?? string.Empty;
            }
            else if (responseData.TryGetProperty("message", out var message))
            {
                throw new Exception($"MoMo Error: {message.GetString()}");
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
