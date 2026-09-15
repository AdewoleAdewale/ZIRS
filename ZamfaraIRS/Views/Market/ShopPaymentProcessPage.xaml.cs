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
    public partial class ShopPaymentProcessPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private readonly IReceiptPrintService _printService;
        private ObservableCollection<MarketModel> _markets = new ObservableCollection<MarketModel>();
        private MarketModel _selectedMarket;
        private string _shopNo;
        private string _occupantName;
        private string _months = "1";
        private string _totalCalculated = "0.00";
        private bool _isVerified;
        private bool _showSuccessSheet;
        private bool _showErrorSheet;
        private string _errorMessage;
        private string _currentTransactionRef;

        public ObservableCollection<MarketModel> Markets { get => _markets; set { _markets = value; OnPropertyChanged(); } }
        public MarketModel SelectedMarket { get => _selectedMarket; set { _selectedMarket = value; OnPropertyChanged(); } }
        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public string OccupantName { get => _occupantName; set { _occupantName = value; OnPropertyChanged(); } }
        public string Months { get => _months; set { _months = value; OnPropertyChanged(); } }
        public string TotalCalculated { get => _totalCalculated; set { _totalCalculated = value; OnPropertyChanged(); } }
        public bool IsVerified { get => _isVerified; set { _isVerified = value; OnPropertyChanged(); } }
        public bool ShowSuccessSheet { get => _showSuccessSheet; set { _showSuccessSheet = value; OnPropertyChanged(); } }
        public bool ShowErrorSheet { get => _showErrorSheet; set { _showErrorSheet = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        public ICommand VerifyAndCalculateCommand { get; }
        public ICommand CompletePaymentCommand { get; }
        public ICommand PrintReceiptCommand { get; }
        public ICommand DismissSheetsCommand { get; }

        public ShopPaymentProcessPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;

            VerifyAndCalculateCommand = new Command(async () => await ExecuteVerify());
            CompletePaymentCommand = new Command(async () => await ExecutePayment());
            PrintReceiptCommand = new Command(async () => await ExecutePrintReceipt(isReprint: true));
            DismissSheetsCommand = new Command(() => { ShowSuccessSheet = false; ShowErrorSheet = false; });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Markets.Clear();
            SessionManager.Instance.UpdateActivity(); 
            SessionService.EnsureSessionRestored(); 
            string email = SessionService.SavedEmail ?? "agent@example.com";
            var list = await _shopService.GetMarketsAsync("agent@example.com");
            foreach (var item in list) Markets.Add(item);
        }

        private async Task ExecuteVerify()
        {
            SessionManager.Instance.UpdateActivity();

            if (SelectedMarket == null || string.IsNullOrWhiteSpace(ShopNo) || string.IsNullOrWhiteSpace(OccupantName))
            {
                await DisplayAlert("Required", "Please fill in all verification details", "OK");
                return;
            }

            var repay = await _shopService.VerifyShopRepayAsync(ShopNo.Trim(), SelectedMarket.Id, OccupantName.Trim());
            if (repay != null)
            {
                int.TryParse(Months, out int m);
                TotalCalculated = await _shopService.CalculateAmountOwedAsync(ShopNo.Trim(), Math.Max(1, m));
                IsVerified = true;
            }
            else
            {
                ErrorMessage = "Shop number, market, and occupant name do not match records.";
                ShowErrorSheet = true;
            }
        }

    
     


        private async Task ExecutePayment()
        {
            SessionManager.Instance.UpdateActivity();

            // 1. Check Bluetooth State First
            var btManager = Xamarin.Forms.DependencyService.Get<IBluetoothManager>();
            if (btManager != null && !btManager.IsBluetoothEnabled())
            {
                await DisplayAlert("Bluetooth Off", "Please turn on your Bluetooth and connect to the printer before processing a transaction.", "OK");
                return; // Halt the transaction entirely
            }

            // 2. Proceed with Payment Logic
            _currentTransactionRef = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            await Task.Delay(500);
            ShowSuccessSheet = true;

            // Automatically print the original receipt upon successful payment
            await ExecutePrintReceipt(isReprint: false);
        }
        private async Task ExecutePrintReceipt(bool isReprint)
        {
            decimal.TryParse(TotalCalculated, out decimal amt);
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrEmpty(_currentTransactionRef))
            {
                _currentTransactionRef = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            }
            await ShopReceiptPrinter.PrintPaymentReceiptAsync(
               ShopNo,
                SelectedMarket?.Market_Plaza ?? "Market",
                OccupantName,
                amt,
                0.00m, _currentTransactionRef,
                DateTime.Now.ToString("dd-MMM-yyyy HH:mm"),
                date: DateTime.Now.ToString("dd-MMM-yyyy HH:mm"),
                isReprint: isReprint
            );
        }


        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}