using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VerifyShopPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private ObservableCollection<MarketModel> _markets = new ObservableCollection<MarketModel>();
        private MarketModel _selectedMarket;
        private string _shopNo;
        private bool _isBusy;
        private bool _hasResult;
        private ShopVerificationModel _verificationResult;
        private Color _statusColor = Color.Black;

        public ObservableCollection<MarketModel> Markets { get => _markets; set { _markets = value; OnPropertyChanged(); } }
        public MarketModel SelectedMarket { get => _selectedMarket; set { _selectedMarket = value; OnPropertyChanged(); } }
        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public Color StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(); } }

        public ShopVerificationModel VerificationResult
        {
            get => _verificationResult;
            set { _verificationResult = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }

        public VerifyShopPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
            VerifyCommand = new Command(async () => await ExecuteVerify());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Markets.Count == 0)
            {
                var list = await _shopService.GetMarketsAsync("agent@example.com");
                foreach (var item in list) Markets.Add(item);
            }
        }

        private async Task ExecuteVerify()
        {
            if (SelectedMarket == null || string.IsNullOrWhiteSpace(ShopNo))
            {
                await DisplayAlert("Validation", "Select a market and enter the shop number", "OK");
                return;
            }

            IsBusy = true;
            HasResult = false;
            try
            {
                var result = await _shopService.VerifyShopAsync(ShopNo.Trim(), SelectedMarket.Id);
                if (result != null)
                {
                    VerificationResult = result;
                    StatusColor = result.StatusCode == "00" ? Color.Green : Color.Red;
                    HasResult = true;
                }
                else
                {
                    await DisplayAlert("Not Found", "Shop record not found or server error.", "OK");
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