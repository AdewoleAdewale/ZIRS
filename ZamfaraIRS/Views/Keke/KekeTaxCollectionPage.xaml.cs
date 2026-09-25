using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeTaxCollectionPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IKekeService _kekeService;
        private bool _isBusy;
        private bool _isVerified;

        // Modal States
        private bool _showVerifySuccessSheet;
        private bool _showErrorSheet;
        private string _errorMessage;

        // Verified Details Properties
        private string _verifiedKekeNo;
        private string _verifiedVehicleType;
        private string _verifiedStatusMessage;

        public ServiceModel SelectedService { get; set; }
        public string PayerId { get; set; }
        public string PinInput { get; set; }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool IsVerified
        {
            get => _isVerified;
            set
            {
                _isVerified = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotVerified));
            }
        }

        public bool IsNotVerified => !IsVerified;

        public bool ShowVerifySuccessSheet
        {
            get => _showVerifySuccessSheet;
            set { _showVerifySuccessSheet = value; OnPropertyChanged(); }
        }

        public bool ShowErrorSheet
        {
            get => _showErrorSheet;
            set { _showErrorSheet = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string VerifiedKekeNo
        {
            get => _verifiedKekeNo;
            set { _verifiedKekeNo = value; OnPropertyChanged(); }
        }

        public string VerifiedVehicleType
        {
            get => _verifiedVehicleType;
            set { _verifiedVehicleType = value; OnPropertyChanged(); }
        }

        public string VerifiedStatusMessage
        {
            get => _verifiedStatusMessage;
            set { _verifiedStatusMessage = value; OnPropertyChanged(); }
        }

        public KekeTaxCollectionPage(ServiceModel service)
        {
            InitializeComponent();
            _kekeService = new KekeService();
            SelectedService = service;
            BindingContext = this;
        }

        /// <summary>
        /// Strips currency symbols, thousands separators and stray whitespace before parsing,
        /// so a display-formatted amount (e.g. "₦1,000") never silently becomes 0.
        /// Requires the source property to actually hold an unformatted value at runtime —
        /// see the XAML fix (Mode=OneWay) that stops the display format being written back.
        /// </summary>
        private static bool TryParseAmount(string raw, out decimal amount)
        {
            amount = 0m;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // Keep only digits and a decimal point.
            var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.').ToArray());

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
                   && amount > 0;
        }

        private async Task ExecuteVerifyAsync()
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(PayerId))
            {
                await DisplayAlert("Validation", "Please enter a valid Payer ID / Body Number.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                string concode = MainPage.Super_Agent ?? "9LF299r0afwIXMN";
                var status = await _kekeService.GetKekeStatusAsync(PayerId.Trim().ToUpper(), concode);

                Device.BeginInvokeOnMainThread(() =>
                {
                    if (status != null && (status.Status == "00" || status.Status == "01"))
                    {
                        VerifiedKekeNo = status.KekeNo ?? PayerId.Trim().ToUpper();
                        VerifiedVehicleType = status.VehicleType ?? "Tricycle / Keke";
                        VerifiedStatusMessage = status.Message ?? "Verified Record Found";

                        IsVerified = true;
                        ShowVerifySuccessSheet = true;
                    }
                    else
                    {
                        ErrorMessage = status?.Message ?? "Payer ID could not be verified. Please check and try again.";
                        ShowErrorSheet = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ErrorMessage = $"Network Error: {ex.Message}";
                    ShowErrorSheet = true;
                });
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() => IsBusy = false);
            }
        }

        private async Task ExecutePaymentAsync()
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4)
            {
                await DisplayAlert("Validation", "A valid 4-digit PIN is required.", "OK");
                return;
            }

            if (!TryParseAmount(SelectedService?.ServiceAmount, out decimal amount))
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ErrorMessage = "Could not read a valid amount for this service. Please go back and re-select the service.";
                    ShowErrorSheet = true;
                });
                return;
            }

            if (amount < 50m)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ErrorMessage = "Invalid amount. Payments less than ₦50 are not allowed. Please choose another service or enter a valid amount.";
                    ShowErrorSheet = true;
                });
                return;
            }

            // concode must match a real, verified agent context — never fall back to a placeholder
            // that's guaranteed to fail server-side, since that produces a confusing error far from
            // its real cause (an expired/missing session).
            if (string.IsNullOrWhiteSpace(MainPage.Super_Agent))
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ErrorMessage = "Your session appears to have expired. Please log in again before making a payment.";
                    ShowErrorSheet = true;
                });
                return;
            }

            IsBusy = true;
            try
            {
                string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
                string concode = MainPage.Super_Agent;

                var response = await _kekeService.SubmitKekeTransactionAsync(
                    SelectedService.ServiceName,
                    agentEmail,
                    amount, // validated, non-zero, correctly parsed amount
                    PayerId.Trim().ToUpper(),
                    PinInput,
                    concode
                );

                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (response != null && response.RespondCode == "00")
                    {
                        await DisplayAlert("Payment Successful", response.Message, "OK");

                        // Receipt now correctly prints the actual amount paid
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
                        ErrorMessage = response?.ResponseMessage ?? "Transaction failed.";
                        ShowErrorSheet = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ErrorMessage = $"Transaction Failed: {ex.Message}";
                    ShowErrorSheet = true;
                });
            }
            finally
            {
                Device.BeginInvokeOnMainThread(() => IsBusy = false);
            }
        }

        // Direct Click handler to dismiss the popup reliably
        public void OnCloseSheetsClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            Device.BeginInvokeOnMainThread(() =>
            {
                ShowVerifySuccessSheet = false;
                ShowErrorSheet = false;
            });
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await ExecutePaymentAsync();
        }

        private async void Button_Clicked_2(object sender, EventArgs e)
        {
            await ExecuteVerifyAsync();
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // In KekeTaxCollectionPage.xaml.cs
        private async void OnTestPrintClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            IsBusy = true;
            try
            {
                bool success = await ShopReceiptPrinter.TestKekeWatermarkPrintAsync();
                if (success)
                {
                    await DisplayAlert("Success", "Test watermark receipt sent to printer.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Printer failed or is disconnected.", "OK");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}