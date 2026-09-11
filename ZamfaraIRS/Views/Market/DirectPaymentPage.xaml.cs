using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    public partial class DirectPaymentPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopNumber;
        private string _portalMessage;
        private string _monthsInput = "1";
        private string _amount;
        private string _pinInput;
        private bool _isBusy;

        // Bottom Sheet Properties
        private bool _showSuccessSheet;
        private bool _showErrorSheet;
        private string _errorMessage;
        private string _successTransactionRef;

        // Properties passed from Verification (Optional)
        public int MarketId { get; set; }
        public string ShopCategory { get; set; }
        public string OccupantName { get; set; }

        public string ShopNumber { get => _shopNumber; set { _shopNumber = value; OnPropertyChanged(); } }
        public string PortalMessage { get => _portalMessage; set { _portalMessage = value; OnPropertyChanged(); } }
        public string MonthsInput { get => _monthsInput; set { _monthsInput = value; OnPropertyChanged(); } }
        public string Amount { get => _amount; set { _amount = value; OnPropertyChanged(); } }
        public string PinInput { get => _pinInput; set { _pinInput = value; OnPropertyChanged(); } }

        public bool ShowSuccessSheet { get => _showSuccessSheet; set { _showSuccessSheet = value; OnPropertyChanged(); } }
        public bool ShowErrorSheet { get => _showErrorSheet; set { _showErrorSheet = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand ProcessPaymentCommand { get; }
        public ICommand DismissSheetsCommand { get; }
        public ICommand PrintReceiptCommand { get; }

        public DirectPaymentPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;

            ProcessPaymentCommand = new Command(async () => await ExecutePayment());
            DismissSheetsCommand = new Command(() => { ShowSuccessSheet = false; ShowErrorSheet = false; });
            PrintReceiptCommand = new Command(async () => await ExecutePrintReceipt());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SessionManager.Instance.UpdateActivity();
            if (string.IsNullOrEmpty(Amount) && !string.IsNullOrEmpty(ShopNumber))
            {
                await CalculateTotalAsync();
            }
        }

        private async void OnMonthsChanged(object sender, TextChangedEventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            await CalculateTotalAsync();
        }

        private async Task CalculateTotalAsync()
        {
            if (string.IsNullOrWhiteSpace(ShopNumber)) return;

            if (!int.TryParse(MonthsInput, out int months) || months <= 0)
                months = 1;

            IsBusy = true;
            try
            {
                Amount = await _shopService.CalculateAmountOwedAsync(ShopNumber.Trim(), months);
            }
            catch
            {
                Amount = "0.00";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecutePayment()
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(Amount) || Amount == "0.00")
            {
                ErrorMessage = "Amount cannot be zero.";
                ShowErrorSheet = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4)
            {
                ErrorMessage = "A valid 4-digit PIN is required.";
                ShowErrorSheet = true;
                return;
            }

            IsBusy = true;
            try
            {
                decimal.TryParse(Amount.Replace(",", ""), out decimal amountPaid);
                string refNo = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                string agentEmail = SessionService.SavedEmail ?? "agent@example.com";

                RepaymentResponseModel response = null;

                // Smart Fallback Logic:
                // If we have full details, use ShopRepay. Otherwise, use General Collection to ensure success.
                if (MarketId > 0 && !string.IsNullOrEmpty(OccupantName))
                {
                    response = await _shopService.SubmitShopRepaymentAsync(
                        agentEmail, ShopNumber, MarketId, ShopCategory, OccupantName, amountPaid, PinInput, refNo, "Direct"
                    );
                }
                else
                {
                    response = await _shopService.SubmitGeneralCollectionAsync(
                        agentEmail, "Direct Shop Payment", amountPaid, ShopNumber, PinInput, "ZIRS-MERCHANT"
                    );
                }

                // API responds with "00" on success[cite: 8]
                if (response != null && response.RespondCode == "00")
                {
                    _successTransactionRef = response.TransactionNo ?? refNo;
                    ShowSuccessSheet = true; // Show success UI
                }
                else
                {
                    // "06" is duplicate/agent issue, "09" is missing shop data[cite: 8]
                    ErrorMessage = response?.Message ?? response?.ResponseMessage ?? "Payment processing failed. Please try again.";
                    ShowErrorSheet = true; // Show error UI
                }
            }
            catch (Exception ex)
            {
                // Prevents crash on network failure
                ErrorMessage = $"Connection Error: {ex.Message}. Please check your network and try again.";
                ShowErrorSheet = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecutePrintReceipt()
        {
            decimal.TryParse(Amount.Replace(",", ""), out decimal amountPaid);

            // Invoke the ShopReceiptPrinter method we previously structured
            await ShopReceiptPrinter.PrintPaymentReceiptAsync(
                shopNo: ShopNumber,
                marketName: null,
                occupantName: OccupantName ?? "Verified Occupant",
                amountPaid: amountPaid,
                agentEmail:SessionService.SavedEmail,
                balanceRemaining: 0.00m,
                refNo: _successTransactionRef,
                date: DateTime.Now.ToString("dd-MMM-yyyy HH:mm"),
                isReprint: false
            );

            await Navigation.PopToRootAsync();
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            _ = ExecutePayment();
        }
    }
}