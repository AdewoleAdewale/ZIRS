using Acr.UserDialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private string _lastGeneratedRef;
        private bool _isProcessing;

        public ObservableCollection<MarketModel> Markets { get; set; } = new ObservableCollection<MarketModel>();

        public ShopRepaymentPaymentPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            MarketPicker.ItemsSource = Markets;
            InitializeSheet();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
           // SessionService.EnsureSessionRestored();
            await LoadMarketsAsync();
        }

        private async void InitializeSheet()
        {
            this.Opacity = 0;
            await this.FadeTo(1, 200);
            await SheetFrame.TranslateTo(0, 0, 300, Easing.SpringOut);
        }

        private async Task LoadMarketsAsync()
        {
            Markets.Clear();
            string email = MainPage.ValidUserMail ?? "agent@example.com"; 
            var list = await _shopService.GetMarketsAsync(email);
            foreach (var m in list) Markets.Add(m);
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
                    // Generate unique reference and simulated transaction
                    _lastGeneratedRef = $"SHP-RP-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                    decimal.TryParse(txtPayAmount.Text, out decimal amtPaid);

                    await Task.Delay(800); // Server persistence hook

                    // Success presentation
                    PaymentFormView.IsVisible = false;
                    lblSuccessRef.Text = _lastGeneratedRef;
                    lblSuccessShopNo.Text = txtShopNo.Text.Trim();
                    lblSuccessOccupant.Text = txtOccupant.Text.Trim();
                    lblSuccessAmountPaid.Text = $"₦{amtPaid:N2}";
                    PaymentSuccessView.IsVisible = true;

                    // Trigger primary thermal receipt print
                    await ShopReceiptPrinter.PrintShopPaymentReceiptAsync(
                        _lastGeneratedRef,
                        txtShopNo.Text.Trim(),
                        _selectedMarket.Market_Plaza,
                        txtOccupant.Text.Trim(),
                        _verifiedShop?.ShopCategory ?? "Standard Shop",
                        amtPaid,
                        0.00m,
                        MainPage.ValidUserMail ?? "agent@example.com", 
                        isReprint: false
                    );
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
            decimal.TryParse(txtPayAmount.Text, out decimal amtPaid);

            await ShopReceiptPrinter.PrintShopPaymentReceiptAsync(
                _lastGeneratedRef,
                txtShopNo.Text.Trim(),
                _selectedMarket.Market_Plaza,
                txtOccupant.Text.Trim(),
                _verifiedShop?.ShopCategory ?? "Standard Shop",
                amtPaid,
                0.00m,
                MainPage.ValidUserMail ?? "agent@example.com",
                isReprint: true
            );
        }

        private async void OnNewTransactionClicked(object sender, EventArgs e) => await DismissSheet();
        private async void OnCancelClicked(object sender, EventArgs e) => await DismissSheet();
        private async void OnBackgroundTapped(object sender, EventArgs e) => await DismissSheet();

        private async Task DismissSheet()
        {
            await SheetFrame.TranslateTo(0, 500, 200, Easing.CubicIn);
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }
    }
}