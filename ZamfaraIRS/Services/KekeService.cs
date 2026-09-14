using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Services
{
    public interface IKekeService
    {
        Task<KekeStatusResponse> GetKekeStatusAsync(string kekeNo, string concode);
        Task<KekeTransactionResponse> SubmitKekeTransactionAsync(string serviceName, string email, decimal amount, string payerId, string pin, string concode);
        Task<List<KekeHistoryResponseModel>> GetKekeTransactionsAsync(string email, string searchFrom, string searchTo);

    }

    public class KekeService : IKekeService
    {
        private readonly HttpClient _httpClient;

        public KekeService(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? SslHandler.GetInsecureHttpClient();
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://zamfara.osoftpay.net/");
            }
        }

        public async Task<KekeStatusResponse> GetKekeStatusAsync(string kekeNo, string concode)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/KekeTransactions/GetKekeCount?KekeNo={Uri.EscapeDataString(kekeNo)}");
            request.Headers.Add("Concode", concode); 

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<KekeStatusResponse>(json);
        }

        public async Task<KekeTransactionResponse> SubmitKekeTransactionAsync(string serviceName, string email, decimal amount, string payerId, string pin, string concode)
        {
            using (var content = new MultipartFormDataContent())
            {
                content.Add(new StringContent(serviceName), "ServiceName");
                content.Add(new StringContent(email), "Email");
                content.Add(new StringContent(amount.ToString()), "Amount");
                content.Add(new StringContent(payerId), "Payer"); // Registration number or Driver ID
                content.Add(new StringContent(pin), "Pin");

                var request = new HttpRequestMessage(HttpMethod.Post, "api/KekeTransactions/Post/v3/KekeTransact")
                {
                    Content = content
                };
                request.Headers.Add("Concode", concode); // Required connection code

                // Note: The external service has a 10-minute timeout; apply reasonable client-side timeout
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<KekeTransactionResponse>(json);
            }
        }


        public async Task<List<KekeHistoryResponseModel>> GetKekeTransactionsAsync(string email, string searchFrom, string searchTo)
        {
            // Ensure the date format matches the API's expected format (dd-MM-yyyy)
            string url = $"api/TaskPayers/gettransaction?Email={Uri.EscapeDataString(email)}&SearchFrom={searchFrom}&SearchTo={searchTo}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return new List<KekeHistoryResponseModel>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<KekeHistoryResponseModel>>(json);
        }


    }
}