using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Services
{
    public interface IShopService
    {
        Task<ApiResponse> RegisterMarketAsync(string email, string marketPlaza, string mktCode);
        Task<List<MarketModel>> GetMarketsAsync(string agentEmail);
        Task<List<ShopCategoryModel>> GetShopCategoriesAsync(int mktId, string agentEmail);
        Task<List<ShopItemModel>> GetMarketShopsAsync(int mktId);
        Task<ApiResponse> EnumerateShopAsync(string recordedBy, int marketId, string shopCat, string shopNo);
        Task<ShopVerificationModel> VerifyShopAsync(string shopNo, int mktId);
        Task<List<ShopPaymentHistoryModel>> GetShopPaymentsAsync(string shopNo, string dateFrom = null, string dateTo = null);
        Task<string> CalculateAmountOwedAsync(string shopNo, int months);
        Task<ShopRepaymentVerificationModel> VerifyShopRepayAsync(string shopNo, int mktId, string occupant);
        Task<ShopNoVerificationModel> VerifyShopNoAsync(string shopNo, string occupant);
        Task<List<ShopItemModel>> GetShopListAsync(int mktId, string agentEmail);
    }

    public class ShopService : IShopService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://zamfara.osoftpay.net/";

        public ShopService(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? SslHandler.GetInsecureHttpClient();

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }
        }

        // Helper to grab the securely saved email from the current session
        private string GetActiveAgentEmail()
        {
            return !string.IsNullOrEmpty(MainPage.ValidUserMail)
                ? MainPage.ValidUserMail
                : SessionService.SavedEmail;
        }

        public async Task<ApiResponse> RegisterMarketAsync(string email, string marketPlaza, string mktCode)
        {
            using (var content = new MultipartFormDataContent())
            {
                content.Add(new StringContent(marketPlaza ?? string.Empty), "Market_Plaza");
                content.Add(new StringContent(mktCode ?? string.Empty), "MktCode");

                // Intercept and use session email
                string activeEmail = GetActiveAgentEmail();
                var response = await _httpClient.PostAsync($"api/Shops/{Uri.EscapeDataString(activeEmail)}/NewMarket", content);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(json);
            }
        }

        public async Task<List<MarketModel>> GetMarketsAsync(string agentEmail)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/Shops/GetMarket");
            request.Headers.Add("Agent", GetActiveAgentEmail());

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<MarketModel>();

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<MarketModel>>(json);

            if (list != null && list.Count == 1 && string.Equals(list[0].Market_Plaza, "Wrong Agent email", StringComparison.OrdinalIgnoreCase))
            {
                return new List<MarketModel>();
            }
            return list ?? new List<MarketModel>();
        }

        public async Task<List<ShopCategoryModel>> GetShopCategoriesAsync(int mktId, string agentEmail)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/Shops/{mktId}/GetShopCat");
            request.Headers.Add("Agent", GetActiveAgentEmail());

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<ShopCategoryModel>();

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<ShopCategoryModel>>(json);

            if (list != null && list.Count == 1 && string.Equals(list[0].ShopCategoryName, "Wrong Agent email", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ShopCategoryModel>();
            }
            return list ?? new List<ShopCategoryModel>();
        }

        public async Task<List<ShopItemModel>> GetMarketShopsAsync(int mktId)
        {
            var response = await _httpClient.GetAsync($"api/Shops/{mktId}/GetMktShops");
            if (!response.IsSuccessStatusCode) return new List<ShopItemModel>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ShopItemModel>>(json) ?? new List<ShopItemModel>();
        }

        public async Task<ApiResponse> EnumerateShopAsync(string recordedBy, int marketId, string shopCat, string shopNo)
        {
            using (var content = new MultipartFormDataContent())
            {
                // Intercept and use session email for recordedBy
                content.Add(new StringContent(GetActiveAgentEmail()), "RecordedBy");
                content.Add(new StringContent(marketId.ToString()), "MarketId");
                content.Add(new StringContent(shopCat ?? string.Empty), "ShopCat");
                content.Add(new StringContent(shopNo ?? string.Empty), "ShopNo");

                var response = await _httpClient.PostAsync("api/Shops/shopenumeration/AddNew", content);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(json);
            }
        }

        public async Task<ShopVerificationModel> VerifyShopAsync(string shopNo, int mktId)
        {
            var response = await _httpClient.GetAsync($"api/Shops/VerifyShop?ShopNo={Uri.EscapeDataString(shopNo)}&MktId={mktId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ShopVerificationModel>(json);
        }

        public async Task<List<ShopPaymentHistoryModel>> GetShopPaymentsAsync(string shopNo, string dateFrom = null, string dateTo = null)
        {
            string endpoint = $"api/Shops/GetShopPayments?ShopNo={Uri.EscapeDataString(shopNo)}";
            if (!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo))
            {
                endpoint += $"&DateFrom={Uri.EscapeDataString(dateFrom)}&DateTo={Uri.EscapeDataString(dateTo)}";
            }

            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return new List<ShopPaymentHistoryModel>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ShopPaymentHistoryModel>>(json) ?? new List<ShopPaymentHistoryModel>();
        }

        public async Task<string> CalculateAmountOwedAsync(string shopNo, int months)
        {
            var response = await _httpClient.GetAsync($"api/Shops/VerifyAmount?ShopNo={Uri.EscapeDataString(shopNo)}&NoofMnth={months}");
            if (!response.IsSuccessStatusCode) return "0.00";

            var json = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<MonthCalculationModel>(json);
            return res?.TotalAmt ?? "0.00";
        }

        public async Task<ShopRepaymentVerificationModel> VerifyShopRepayAsync(string shopNo, int mktId, string occupant)
        {
            var response = await _httpClient.GetAsync($"api/Shops/VerifyShopRePay?ShopNo={Uri.EscapeDataString(shopNo)}&MktId={mktId}&Occupant={Uri.EscapeDataString(occupant)}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ShopRepaymentVerificationModel>(json);
        }

        public async Task<ShopNoVerificationModel> VerifyShopNoAsync(string shopNo, string occupant)
        {
            var response = await _httpClient.GetAsync($"api/Shops/VerifyShopNo?ShopNo={Uri.EscapeDataString(shopNo)}&Occupant={Uri.EscapeDataString(occupant)}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ShopNoVerificationModel>(json);
        }

        public async Task<List<ShopItemModel>> GetShopListAsync(int mktId, string agentEmail)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/Shops/{mktId}/GetshopList");
            request.Headers.Add("Agent", GetActiveAgentEmail());

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<ShopItemModel>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ShopItemModel>>(json) ?? new List<ShopItemModel>();
        }
    }
}