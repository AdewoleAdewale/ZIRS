using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Dashboard : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private readonly IReceiptPrintService _printService;
        private bool _isNavigating = false;

        public string AgentName { get; set; } = "Zamfara User";
        public string MarketLocationName { get; set; } = "GUSAU CENTRAL MARKET";
        public string MarketLocationSub { get; set; } = "ZAMFARA • CENTRAL COMMERCE HUB";
        public decimal TodayCollected { get; set; } = 0.00m;
        public int PaymentsCount { get; set; } = 0;

        public ObservableCollection<ShopItemModel> RecentActivities { get; set; } = new ObservableCollection<ShopItemModel>();

        public Dashboard()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            _printService = new ReceiptPrintService();

            // Populate agent identity from active session
            if (!string.IsNullOrEmpty(MainPage.Name))
                AgentName = MainPage.Name;

            if (!string.IsNullOrEmpty(MainPage.CollectionPoint))
                MarketLocationName = MainPage.CollectionPoint;

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _isNavigating = false;
            SessionManager.Instance.UpdateActivity();
            await LoadRecentShopsAsync();
        }

        private async Task LoadRecentShopsAsync()
        {
            try
            {
                RecentActivities.Clear();
                string email = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
                var markets = await _shopService.GetMarketsAsync(email);
                if (markets != null && markets.Count > 0)
                {
                    var shops = await _shopService.GetMarketShopsAsync(markets[0].Id);
                    if (shops != null)
                    {
                        for (int i = 0; i < Math.Min(shops.Count, 5); i++)
                        {
                            RecentActivities.Add(shops[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard Error]: {ex.Message}");
            }
        }

        #region Smooth Navigation Helpers

        private async Task SafeNavigateAsync(Page targetPage)
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                SessionManager.Instance.UpdateActivity();

                if (Navigation != null)
                {
                    await Navigation.PushAsync(targetPage, true);
                }
                else if (Application.Current.MainPage is NavigationPage navPage)
                {
                    await navPage.PushAsync(targetPage, true);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", ex.Message, "OK");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnNewPaymentTapped(object sender, EventArgs e)
        {
            await SafeNavigateAsync(new ShopRepaymentPaymentPage());
        }

        private async void OnEnumerateTapped(object sender, EventArgs e)
        {
            await SafeNavigateAsync(new EnumerateShopPage());
        }

        private async void OnHistoryTapped(object sender, EventArgs e)
        {
            await SafeNavigateAsync(new ShopPaymentHistoryPage());
        }

        private async void OnExploreTapped(object sender, EventArgs e)
        {
            await SafeNavigateAsync(new MarketShopsPage());
        }

        private async void OnTestPrintTapped(object sender, EventArgs e)
        {
            if (_printService != null)
                await _printService.PrintTestReceiptAsync();
        }

        private async void OnSettingsTapped(object sender, EventArgs e)
        {
            await OpenOptionsSheetAsync();
        }

        private async void OnOptionsClicked(object sender, EventArgs e)
        {
            await OpenOptionsSheetAsync();
        }

        private async Task OpenOptionsSheetAsync()
        {
            string email = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
            string opt = await DisplayActionSheet("Account Settings", "Close", null, "Change Password", "Change PIN");

            if (opt == "Change Password")
                await Navigation.PushModalAsync(new ChangeSecurityPopup(email, false));
            else if (opt == "Change PIN")
                await Navigation.PushModalAsync(new ChangeSecurityPopup(email, true));
        }

        private async void OnCenterFabClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Quick Actions", "Cancel", null, "New Market Complex", "Enumerate Shop", "Collect Repayment");

            if (action == "New Market Complex")
                await SafeNavigateAsync(new RegisterMarketPage());
            else if (action == "Enumerate Shop")
                await SafeNavigateAsync(new EnumerateShopPage());
            else if (action == "Collect Repayment")
                await SafeNavigateAsync(new ShopRepaymentPaymentPage());
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool exit = await DisplayAlert("Logout", "Do you want to log out?", "Yes", "No");
            if (exit)
            {
                await SessionManager.Instance.LogoutAsync();
            }
        }

        #endregion

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}