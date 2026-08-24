using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterMarketPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _marketPlaza;
        private string _mktCode;
        private bool _isBusy;

        public string MarketPlaza { get => _marketPlaza; set { _marketPlaza = value; OnPropertyChanged(); } }
        public string MktCode { get => _mktCode; set { _mktCode = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand RegisterCommand { get; }

        public RegisterMarketPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            BindingContext = this;
            RegisterCommand = new Command(async () => await ExecuteRegister());
        }

        private async System.Threading.Tasks.Task ExecuteRegister()
        {
            if (string.IsNullOrWhiteSpace(MarketPlaza) || string.IsNullOrWhiteSpace(MktCode))
            {
                await DisplayAlert("Validation", "Please fill in all fields", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                string userEmail = "agent@example.com"; // Provide active user email context
                var result = await _shopService.RegisterMarketAsync(userEmail, MarketPlaza.Trim(), MktCode.Trim());

                if (result == null)
                {
                    await DisplayAlert("Error", "Server error or empty response.", "OK");
                    return;
                }

                switch (result.Status)
                {
                    case "00":
                        await DisplayAlert("Success", $"{result.BusinessName} registered successfully!", "OK");
                        MarketPlaza = string.Empty;
                        MktCode = string.Empty;
                        break;
                    case "01":
                        await DisplayAlert("Duplicate Error", "Failed: Business Complex Code already exists.", "OK");
                        break;
                    case "02":
                        await DisplayAlert("Duplicate Error", "Failed: Business Complex Name already exists.", "OK");
                        break;
                    default:
                        await DisplayAlert("Notice", result.Message, "OK");
                        break;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}