using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeTaxCollectionPage : ContentPage
    {
        private readonly IKekeService _kekeService;
        private bool _isBusy;
        private bool _isVerified;

        // Modal States
        private bool _showVerifySuccessSheet;
        private bool _showErrorSheet;
        private string _errorMessage;

        public ServiceModel SelectedService { get; set; }
        public string PayerId { get; set; }
        public string PinInput { get; set; }

        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool IsVerified { get => _isVerified; set { _isVerified = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotVerified)); } }
        public bool IsNotVerified => !IsVerified;

        public bool ShowVerifySuccessSheet { get => _showVerifySuccessSheet; set { _showVerifySuccessSheet = value; OnPropertyChanged(); } }
        public bool ShowErrorSheet { get => _showErrorSheet; set { _showErrorSheet = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        public ICommand VerifyPayerCommand { get; }
        public ICommand ProcessPaymentCommand { get; }
        public ICommand CloseSheetsCommand { get; }

        public KekeTaxCollectionPage(ServiceModel service)
        {
            InitializeComponent();
            _kekeService = new KekeService();
            SelectedService = service;
            BindingContext = this;

            VerifyPayerCommand = new Command(async () => await ExecuteVerifyAsync());
            ProcessPaymentCommand = new Command(async () => await ExecutePaymentAsync());
            CloseSheetsCommand = new Command(() => { ShowVerifySuccessSheet = false; ShowErrorSheet = false; });
        }

        private async Task ExecuteVerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(PayerId)) return;

            IsBusy = true;
            try
            {
                string concode = "9LF299r0afwIXMN";
                // Verifies the KekeNo against the backend
                var status = await _kekeService.GetKekeStatusAsync(PayerId.Trim().ToUpper(), concode);

                if (status != null && (status.Status == "00" || status.Status == "01"))
                {
                    IsVerified = true;
                    ShowVerifySuccessSheet = true;
                }
                else
                {
                    ErrorMessage = "Payer ID could not be verified. Please check and try again.";
                    ShowErrorSheet = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Network Error: {ex.Message}";
                ShowErrorSheet = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecutePaymentAsync()
        {
            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4)
            {
                await DisplayAlert("Validation", "A valid 4-digit PIN is required.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
                string concode = MainPage.Super_Agent ?? "UNKNOWN_CONCODE";
                decimal.TryParse(SelectedService.ServiceAmount, out decimal amount);

                var response = await _kekeService.SubmitKekeTransactionAsync(
                    SelectedService.ServiceName,
                    agentEmail,
                    amount,
                    PayerId.Trim().ToUpper(),
                    PinInput,
                    concode
                );

                if (response != null && response.RespondCode == "00")
                {
                    await DisplayAlert("Payment Successful", response.Message, "OK");

                    // Trigger native ESC/POS thermal printing
                    await ShopReceiptPrinter.PrintKekeReceiptAsync(
                        transactionNo: response.TransactionNo ?? $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                        vehiclePlateNo: PayerId.Trim().ToUpper(),
                        serviceName: SelectedService.ServiceName,
                        amountPaid: amount,
                        lga: "ZIRS Collection",
                        agentEmail: agentEmail,
                        isReprint: false
                    );

                    await Navigation.PopToRootAsync();
                }
                else
                {
                    ErrorMessage = response?.ResponseMessage ?? "Insufficient Super Agent wallet balance to carry out this transaction";
                    ShowErrorSheet = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Transaction Failed: {ex.Message}";
                ShowErrorSheet = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await ExecutePaymentAsync();
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            ExecuteVerifyAsync();
        }
    }
}