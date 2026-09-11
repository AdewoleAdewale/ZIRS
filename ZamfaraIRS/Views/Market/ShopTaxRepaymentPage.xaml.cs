using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
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
        private ShopRepaymentVerificationModel verifiedRepay;
        private readonly IShopService _shopService;

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

        public ShopTaxRepaymentPage(ShopRepaymentVerificationModel verificationData, IShopService shopService)
        {
            InitializeComponent();
            ShopData = verificationData;

            // Clean up currency symbols and pre-populate owed balance[cite: 1, 4]
            string cleanedOwed = verificationData.AmountOwed?.Replace("₦", string.Empty).Replace(",", string.Empty).Trim() ?? "0.00";
            AmountToPay = cleanedOwed;
            PaymentReference = $"REF-{DateTime.Now:yyyyMMddHHmmss}";
            BindingContext = this;

            _shopService = shopService ?? throw new ArgumentNullException(nameof(shopService));
        }

        public ShopTaxRepaymentPage(ShopRepaymentVerificationModel verifiedRepay)
        {
            this.verifiedRepay = verifiedRepay;
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

        private  void OnMakePaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            ExecutePayment();


        }

        private async void ExecutePayment()
        {
            if (SelectedPaymentChannel == "Select Payment Channel")
            {
                await DisplayAlert("Validation", "Please select a valid payment channel.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountToPay) || string.IsNullOrWhiteSpace(TransactionPin))
            {
                await DisplayAlert("Validation", "Amount and PIN are required.", "OK");
                return;
            }

            // Grab the active session email
            string agentEmail = ZamfaraIRS.MainPage.ValidUserMail ?? ZamfaraIRS.Services.SessionService.SavedEmail ?? "agent@example.com";

            // Show loading
            Acr.UserDialogs.UserDialogs.Instance.ShowLoading("Processing Payment...");

            try
            {
                decimal.TryParse(AmountToPay, out decimal amount);
                int.TryParse(ShopData.MktId ?? "0", out int marketId);

                // 1. Submit to API
                var response = await _shopService.SubmitShopRepaymentAsync(
                    agentEmail,
                    ShopData.ShopNo,
                    marketId,
                    ShopData.ShopCategory,
                    ShopData.Owner,
                    amount,
                    TransactionPin,
                    PaymentReference,
                    SelectedPaymentChannel
                );

                Acr.UserDialogs.UserDialogs.Instance.HideLoading();

                // 2. Validate Response (Only "00" is successful)[cite: 5]
                if (response != null && response.RespondCode == "00")
                {
                    decimal.TryParse(response.Bal, out decimal remainingBalance);
                    string transactionRef = response.TransactionNo ?? PaymentReference;

                    await DisplayAlert("Success", "Payment processed successfully!", "OK");

                    // 3. Print the Shop specific receipt
                    await ShopReceiptPrinter.PrintShopPaymentReceiptAsync(
                        transactionRef,
                        ShopData.ShopNo,
                        ShopData.Market,
                        ShopData.Owner,
                        ShopData.ShopCategory,
                        amount,
                        remainingBalance,
                        agentEmail,
                        isReprint: false
                    );

                    await Navigation.PopToRootAsync();
                }
                else
                {
                    // API returned a failure code (e.g. 01, 06, 09)[cite: 5]
                    string errorMsg = response?.Message ?? response?.ResponseMessage ?? "Transaction failed.";
                    await DisplayAlert("Payment Error", errorMsg, "OK");
                }
            }
            catch (Exception ex)
            {
                Acr.UserDialogs.UserDialogs.Instance.HideLoading();
                await DisplayAlert("Network Error", $"Failed to connect: {ex.Message}", "OK");
            }
        }
        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}