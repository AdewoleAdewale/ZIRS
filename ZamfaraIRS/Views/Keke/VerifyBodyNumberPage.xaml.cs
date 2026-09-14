using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class VerifyBodyNumberPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IKekeService _kekeService;
        private string _bodyNumberInput;
        private bool _isBusy;
        private bool _hasResult;
        private KekeStatusResponse _verificationResult;

        // Payment Sheet Properties
        private bool _showPaymentSheet;
        private bool _showErrorSheet;
        private bool _showSuccessSheet;
        private int _selectedDays = 1;
        private decimal _totalAmountToPay;
        private string _pinInput;
        private string _errorMessage;
        private string _successMessage;
        private string _transactionNo;

        public List<int> DaysOptions { get; } = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        public string BodyNumberInput { get => _bodyNumberInput; set { _bodyNumberInput = value; OnPropertyChanged(); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public KekeStatusResponse VerificationResult { get => _verificationResult; set { _verificationResult = value; OnPropertyChanged(); } }

        public bool ShowPaymentSheet { get => _showPaymentSheet; set { _showPaymentSheet = value; OnPropertyChanged(); } }
        public bool ShowErrorSheet { get => _showErrorSheet; set { _showErrorSheet = value; OnPropertyChanged(); } }
        public bool ShowSuccessSheet { get => _showSuccessSheet; set { _showSuccessSheet = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public string SuccessMessage { get => _successMessage; set { _successMessage = value; OnPropertyChanged(); } }
        public string PinInput { get => _pinInput; set { _pinInput = value; OnPropertyChanged(); } }

        public int SelectedDays
        {
            get => _selectedDays;
            set
            {
                _selectedDays = value;
                OnPropertyChanged();
                CalculateTotal();
            }
        }

        public decimal TotalAmountToPay
        {
            get => _totalAmountToPay;
            set { _totalAmountToPay = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }
        public ICommand OpenPaymentSheetCommand { get; }
        public ICommand CloseSheetsCommand { get; }
        public ICommand ProcessPaymentCommand { get; }
        public ICommand PrintReceiptCommand { get; }

        public VerifyBodyNumberPage()
        {
            InitializeComponent();
            _kekeService = new KekeService();
            BindingContext = this;

            VerifyCommand = new Command(async () => await ExecuteVerifyAsync());
            OpenPaymentSheetCommand = new Command(() => {
                SelectedDays = 1;
                PinInput = string.Empty;
                ShowPaymentSheet = true;
            });
            CloseSheetsCommand = new Command(() => {
                ShowPaymentSheet = false;
                ShowErrorSheet = false;
                ShowSuccessSheet = false;
            });
            ProcessPaymentCommand = new Command(async () => await ExecutePaymentAsync());
            PrintReceiptCommand = new Command(async () => await ExecutePrintReceiptAsync());
        }

        private async Task ExecuteVerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(BodyNumberInput)) return;

            IsBusy = true;
            HasResult = false;
            try
            {
                // Concode is mandatory for the GetKekeCount lookup[cite: 2]
                string concode = MainPage.Super_Agent ?? "UNKNOWN_CONCODE";
                var result = await _kekeService.GetKekeStatusAsync(BodyNumberInput.Trim().ToUpper(), concode);

                if (result != null && (result.Status == "00" || result.Status == "01")) // 00 = Not owing, 01 = Owing[cite: 2]
                {
                    VerificationResult = result;
                    CalculateTotal();
                    HasResult = true;
                }
                else
                {
                    await DisplayAlert("Verification Failed", result?.Message ?? "Record not found.", "OK");
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

        private void CalculateTotal()
        {
            if (VerificationResult != null && decimal.TryParse(VerificationResult.ServiceAmt, out decimal rate))
            {
                TotalAmountToPay = rate * SelectedDays;
            }
        }

        private async Task ExecutePaymentAsync()
        {
            if (SelectedDays < 1 || SelectedDays > 12)
            {
                ErrorMessage = "Please select a valid number of days (1-12).";
                ShowErrorSheet = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4)
            {
                ErrorMessage = "Valid 4-digit PIN is required to proceed.";
                ShowErrorSheet = true;
                return;
            }

            IsBusy = true;
            ShowPaymentSheet = false; // Hide form while processing

            try
            {
                string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
                string concode = MainPage.Super_Agent ?? "UNKNOWN_CONCODE";

                // Post via KekeTransactions/Post/v3/KekeTransact[cite: 2]
                var response = await _kekeService.SubmitKekeTransactionAsync(
                    VerificationResult.ServiceName,
                    agentEmail,
                    TotalAmountToPay,
                    VerificationResult.KekeNo,
                    PinInput,
                    concode
                );

                // Check for "00" success code[cite: 2]
                if (response != null && response.RespondCode == "00")
                {
                    _transactionNo = response.TransactionNo;
                    SuccessMessage = $"Successfully paid ₦{TotalAmountToPay:N2} for {SelectedDays} day(s).";
                    ShowSuccessSheet = true;
                }
                else
                {
                    // RespondCode "02", "06", or others trigger error messages[cite: 2]
                    ErrorMessage = response?.ResponseMessage ?? "Insufficient Super Agent wallet balance or network error.";
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

        private async Task ExecutePrintReceiptAsync()
        {
            string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";

            // Print Keke receipt using the existing SDK methodology implemented previously[cite: 6]
            bool success = await ShopReceiptPrinter.PrintKekeReceiptAsync(
                transactionNo: _transactionNo ?? $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                vehiclePlateNo: VerificationResult.KekeNo,
                serviceName: VerificationResult.ServiceName,
                amountPaid: TotalAmountToPay,
                lga: "ZIRS Collection", // Or VerificationResult.Lga if available
                agentEmail: agentEmail,
                isReprint: false
            );

            if (success)
            {
                CloseSheetsCommand.Execute(null);
                await Navigation.PopToRootAsync();
            }
            else
            {
                await DisplayAlert("Printer Error", "Ensure the printer is connected via Bluetooth.", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}