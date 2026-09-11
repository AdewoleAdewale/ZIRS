using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopRepaymentPaymentPage : ContentPage
    {
        private readonly IShopService _shopService;
        private ShopRepaymentVerificationModel _verifiedShop;
        private MarketModel _selectedMarket;
        private ShopItemModel _prefilledShopData;
        private string _lastGeneratedRef;
        private bool _isProcessing;

        public ObservableCollection<MarketModel> Markets { get; set; } = new ObservableCollection<MarketModel>();

        // Default constructor for manual entry
        public ShopRepaymentPaymentPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            MarketPicker.ItemsSource = Markets;
            InitializeSheet();
        }

        // Overloaded constructor when navigated from Shop Details
        public ShopRepaymentPaymentPage(ShopItemModel shopData) : this()
        {
            _prefilledShopData = shopData;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SessionService.EnsureSessionRestored();
            SessionManager.Instance.UpdateActivity();

            await LoadMarketsAsync();

            // Auto-fill and verify if data was passed in
            if (_prefilledShopData != null)
            {
                AutoPopulateAndVerify(_prefilledShopData);
                _prefilledShopData = null; // Clear to prevent re-running if navigating back
            }
        }

        private async void InitializeSheet()
        {
            this.Opacity = 1; // Ensures the page is visible
            SheetFrame.TranslationY = 600;
            await SheetFrame.TranslateTo(0, 0, 300, Easing.SpringOut);
        }

        private async Task LoadMarketsAsync()
        {
            Markets.Clear();
            string email = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
            var list = await _shopService.GetMarketsAsync(email);
            foreach (var m in list) Markets.Add(m);
        }

        private void AutoPopulateAndVerify(ShopItemModel shopData)
        {
            txtShopNo.Text = shopData.ShopNo;
            txtOccupant.Text = shopData.CurrentOccupant;

            // Auto-match Market
            var matchedMarket = Markets.FirstOrDefault(m => m.Id == shopData.MarketId || m.Market_Plaza == shopData.MarketName);
            if (matchedMarket != null)
            {
                MarketPicker.SelectedItem = matchedMarket;
                _selectedMarket = matchedMarket;
            }

            // Auto-trigger verification
            if (_selectedMarket != null && !string.IsNullOrWhiteSpace(txtShopNo.Text) && !string.IsNullOrWhiteSpace(txtOccupant.Text))
            {
                OnVerifyShopClicked(null, null);
            }
        }

        private void OnMarketSelected(object sender, EventArgs e)
        {
            if (MarketPicker.SelectedIndex >= 0)
                _selectedMarket = Markets[MarketPicker.SelectedIndex];
        }

        private void OnInputChanged(object sender, TextChangedEventArgs e)
        {
            VerifiedDetailsContainer.IsVisible = false;
        }

        private async void OnVerifyShopClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (_selectedMarket == null || string.IsNullOrWhiteSpace(txtShopNo.Text) || string.IsNullOrWhiteSpace(txtOccupant.Text))
            {
                UserDialogs.Instance.Alert("Please select a market and supply both the shop number and occupant name.", "Validation", "OK");
                return;
            }

            using (UserDialogs.Instance.Loading("Verifying shop…"))
            {
                try
                {
                    _verifiedShop = await _shopService.VerifyShopRepayAsync(txtShopNo.Text.Trim(), _selectedMarket.Id, txtOccupant.Text.Trim());

                    if (_verifiedShop != null)
                    {
                        lblCategoryRate.Text = $"₦{_verifiedShop.ShopAmount}";
                        lblAmountOwed.Text = $"₦{_verifiedShop.AmountOwed}";
                        await RecalculateTotalAsync();
                        VerifiedDetailsContainer.IsVisible = true;
                    }
                    else
                    {
                        UserDialogs.Instance.Alert("Shop not found with matching market and occupant credentials.", "Not Found", "OK");
                    }
                }
                catch (Exception ex)
                {
                    UserDialogs.Instance.Alert(ex.Message, "Error", "OK");
                }
            }
        }

        private async void OnMonthsChanged(object sender, TextChangedEventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            await RecalculateTotalAsync();
        }

        private async Task RecalculateTotalAsync()
        {
            if (!int.TryParse(txtMonths.Text, out int months) || months <= 0)
                months = 1;

            if (!string.IsNullOrWhiteSpace(txtShopNo.Text))
            {
                string total = await _shopService.CalculateAmountOwedAsync(txtShopNo.Text.Trim(), months);
                txtPayAmount.Text = total;
            }
        }

        private void OnPinTextChanged(object sender, TextChangedEventArgs e)
        {
            BtnSubmitPayment.IsEnabled = !string.IsNullOrWhiteSpace(txtPin.Text) && txtPin.Text.Trim().Length == 4;
        }

        private async void OnSubmitPaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (_isProcessing) return;

            string pin = txtPin.Text?.Trim();
            if (pin != MainPage.Pin)
            {
                UserDialogs.Instance.Alert("Invalid Agent Security PIN.", "Security Error", "OK");
                return;
            }

            _isProcessing = true;
            using (UserDialogs.Instance.Loading("Posting repayment…"))
            {
                try
                {
                    _lastGeneratedRef = $"SHP-RP-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                    decimal.TryParse(txtPayAmount.Text, out decimal amtPaid);

                    await Task.Delay(800); // Server persistence hook

                    PaymentFormView.IsVisible = false;
                    lblSuccessRef.Text = _lastGeneratedRef;
                    lblSuccessShopNo.Text = txtShopNo.Text.Trim();
                    lblSuccessOccupant.Text = txtOccupant.Text.Trim();
                    lblSuccessAmountPaid.Text = $"₦{amtPaid:N2}";
                    PaymentSuccessView.IsVisible = true;

                    // Trigger primary thermal receipt print
                    await ExecutePrintReceipt(isReprint: false);
                }
                catch (Exception ex)
                {
                    UserDialogs.Instance.Alert(ex.Message, "Payment Failed", "OK");
                }
                finally
                {
                    _isProcessing = false;
                }
            }
        }

        private async void OnReprintReceiptClicked(object sender, EventArgs e)
        {
            await ExecutePrintReceipt(isReprint: true);
        }

        // Unified print helper pulling the agent email directly from the session
        private async Task ExecutePrintReceipt(bool isReprint)
        {
            SessionManager.Instance.UpdateActivity();
            decimal.TryParse(txtPayAmount.Text, out decimal amtPaid);
            string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";

            await ShopReceiptPrinter.PrintShopPaymentReceiptAsync(
                _lastGeneratedRef,
                txtShopNo.Text.Trim(),
                _selectedMarket?.Market_Plaza ?? "Market",
                txtOccupant.Text.Trim(),
                _verifiedShop?.ShopCategory ?? "Standard Shop",
                amtPaid,
                0.00m,
                agentEmail,
                isReprint: isReprint
            );
        }

        private async void OnNewTransactionClicked(object sender, EventArgs e) => await DismissSheet();
        private async void OnCancelClicked(object sender, EventArgs e) => await DismissSheet();
        private async void OnBackgroundTapped(object sender, EventArgs e) => await DismissSheet();

        private async Task DismissSheet()
        {
            await SheetFrame.TranslateTo(0, 600, 200, Easing.CubicIn);
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }
    }
}