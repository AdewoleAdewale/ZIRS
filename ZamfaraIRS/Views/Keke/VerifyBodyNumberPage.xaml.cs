using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        private readonly IReceiptPrintService _printService;
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
        public string EnteredPin { get; set; }
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
            set { _selectedDays = value; OnPropertyChanged(); CalculateTotal(); }
        }

        public decimal TotalAmountToPay
        {
            get => _totalAmountToPay;
            set { _totalAmountToPay = value; OnPropertyChanged(); }
        }

        public VerifyBodyNumberPage()
        {
            InitializeComponent();
            _kekeService = new KekeService();
            _printService = new ReceiptPrintService();
            BindingContext = this;
        }

        private async void OnVerifyClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(BodyNumberInput)) return;

            IsBusy = true;
            HasResult = false;
            try
            {
                string concode = "9LF299r0afwIXMN";
                var result = await _kekeService.GetKekeStatusAsync(BodyNumberInput.Trim().ToUpper(), concode);

                if (result != null && (result.Status == "00" || result.Status == "01"))
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



        private void OnOpenPaymentSheetClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            ShowPaymentSheet = true;
            OnPropertyChanged(nameof(ShowPaymentSheet));
        }

        private void OnCloseSheetsClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            ShowPaymentSheet = false;
            ShowErrorSheet = false;
            ShowSuccessSheet = false;
        }

        private void CalculateTotal()
        {
            if (VerificationResult != null && decimal.TryParse(VerificationResult.ServiceAmt, out decimal rate))
            {
                TotalAmountToPay = rate * SelectedDays;
            }
        }

        private async void OnProcessPaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

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
            ShowPaymentSheet = false;

            try
            {
                string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
                string concode = "9LF299r0afwIXMN";

                var response = await _kekeService.SubmitKekeTransactionAsync(
                    VerificationResult.ServiceName,
                    agentEmail,
                    TotalAmountToPay,
                    VerificationResult.KekeNo,
                    PinInput,
                    concode
                );

                if (response != null && response.RespondCode == "00")
                {
                    _transactionNo = response.TransactionNo;
                    SuccessMessage = $"Successfully paid ₦{TotalAmountToPay:N2} for {SelectedDays} day(s).";
                    ShowSuccessSheet = true;
                }
                else
                {
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

        private async void OnPrintReceiptClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";

            try
            {
                bool success = await ShopReceiptPrinter.PrintKekeReceiptAsync(
                    transactionNo: _transactionNo ?? $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    vehiclePlateNo: VerificationResult.KekeNo,
                    serviceName: VerificationResult.ServiceName,
                    amountPaid: TotalAmountToPay,
                    lga: "ZIRS Collection",
                    agentEmail: agentEmail,
                    isReprint: false
                );

                if (success)
                {
                    OnCloseSheetsClicked(null, null);
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Printer Error", "Ensure the printer is connected via Bluetooth.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Printer Error", $"An error occurred while printing: {ex.Message}", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}