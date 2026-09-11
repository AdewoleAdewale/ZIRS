using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopTaxRepaymentPage : ContentPage, INotifyPropertyChanged
    {
        private ShopRepaymentVerificationModel _shopData;
        private string _selectedPaymentChannel = "Select Payment Channel";
        private bool _isPopupVisible;
        private string _paymentReference;
        private string _amountToPay;
        private string _transactionPin;

        public ShopRepaymentVerificationModel ShopData
        {
            get => _shopData;
            set { _shopData = value; OnPropertyChanged(); }
        }

        public string SelectedPaymentChannel
        {
            get => _selectedPaymentChannel;
            set { _selectedPaymentChannel = value; OnPropertyChanged(); }
        }

        public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set { _isPopupVisible = value; OnPropertyChanged(); }
        }

        public string PaymentReference
        {
            get => _paymentReference;
            set { _paymentReference = value; OnPropertyChanged(); }
        }

        public string AmountToPay
        {
            get => _amountToPay;
            set { _amountToPay = value; OnPropertyChanged(); }
        }

        public string TransactionPin
        {
            get => _transactionPin;
            set { _transactionPin = value; OnPropertyChanged(); }
        }

        public ShopTaxRepaymentPage(ShopRepaymentVerificationModel verificationData)
        {
            InitializeComponent();
            ShopData = verificationData;

            // Clean up currency symbols and pre-populate owed balance[cite: 1, 4]
            string cleanedOwed = verificationData.AmountOwed?.Replace("₦", string.Empty).Replace(",", string.Empty).Trim() ?? "0.00";
            AmountToPay = cleanedOwed;
            PaymentReference = $"REF-{DateTime.Now:yyyyMMddHHmmss}";
            BindingContext = this;
        }

        private void OnSelectChannelTapped(object sender, EventArgs e) => IsPopupVisible = true;
        private void OnClosePopupTapped(object sender, EventArgs e) => IsPopupVisible = false;

        private void OnChannelOptionTapped(object sender, EventArgs e)
        {
            if (sender is Label lbl && lbl.GestureRecognizers[0] is TapGestureRecognizer tap)
            {
                SelectedPaymentChannel = tap.CommandParameter?.ToString();
                IsPopupVisible = false;
            }
        }

        private async void OnMakePaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (SelectedPaymentChannel == "Select Payment Channel")
            {
                await DisplayAlert("Validation", "Please select a payment channel (Remita or PayZamfara).", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountToPay) || string.IsNullOrWhiteSpace(TransactionPin))
            {
                await DisplayAlert("Validation", "Amount to pay and 4-digit PIN are required.", "OK");
                return;
            }

            if (!string.IsNullOrEmpty(MainPage.Pin) && TransactionPin != MainPage.Pin)
            {
                await DisplayAlert("Security", "Incorrect Agent PIN. Please try again.", "OK");
                return;
            }

            try
            {
                decimal.TryParse(AmountToPay, out decimal amount);
                decimal.TryParse(ShopData.AmountOwed?.Replace("₦", string.Empty).Replace(",", string.Empty), out decimal balanceRemaining);
                balanceRemaining = Math.Max(0, balanceRemaining - amount);

                await DisplayAlert("Processing", $"Processing {SelectedPaymentChannel} collection of ₦{amount:N2} for shop {ShopData.ShopNo}...", "OK");

                // Execute official thermal printing[cite: 2, 4]
                await ShopReceiptPrinter.PrintPaymentReceiptAsync(
                    ShopData.ShopNo,
                    ShopData.Market,
                    ShopData.Owner,
                    amount,
                    balanceRemaining,
                    PaymentReference,
                    DateTime.Now.ToString("dd-MMM-yyyy HH:mm"),
                    isReprint: false
                );

                await DisplayAlert("Success", "Payment processed and receipt printed successfully!", "Done");
                await Navigation.PopToRootAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Payment failed: {ex.Message}", "OK");
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}