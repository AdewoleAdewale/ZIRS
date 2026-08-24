using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Services
{
    public interface IAccountService
    {
        Task<ApiResponse> ChangePasswordAsync(string email, string newPassword);
        Task<ApiResponse> ChangePinAsync(string email, string newPin);
    }

    public class AccountService : IAccountService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://yobe.osoftpay.net/";

        public AccountService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? new HttpClient();
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }
        }

        public async Task<ApiResponse> ChangePasswordAsync(string email, string newPassword)
        {
            var response = await _httpClient.GetAsync($"api/TaskPayers/ChangePassword?UserName={Uri.EscapeDataString(email)}&NewPassword={Uri.EscapeDataString(newPassword)}");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<ApiResponse> ChangePinAsync(string email, string newPin)
        {
            var response = await _httpClient.GetAsync($"api/TaskPayers/ChangePin?UserName={Uri.EscapeDataString(email)}&NewPin={Uri.EscapeDataString(newPin)}");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}