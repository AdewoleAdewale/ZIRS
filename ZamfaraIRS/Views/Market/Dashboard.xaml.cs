using Android.Content.Res;
using Android.PrintServices;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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
    private readonly string _agentEmail = "agent@example.com";

    public string AgentName { get; set; } = "Zamfara User";
    public string MarketLocationName { get; set; } = "GUSAU CENTRAL MARKET";
    public string MarketLocationSub { get; set; } = "ZAMFARA • CENTRAL COMMERCE HUB";
    public decimal TodayCollected { get; set; } = 0.00m;
    public int PaymentsCount { get; set; } = 0;

    public ObservableCollection<ShopItemModel> RecentActivities { get; set; } = new ObservableCollection<ShopItemModel>();

    public ICommand NavNewPaymentCommand { get; }
    public ICommand NavEnumerateCommand { get; }
    public ICommand NavHistoryCommand { get; }
    public ICommand NavExploreCommand { get; }
    public ICommand TestPrintCommand { get; }
    public ICommand CenterFabActionCommand { get; }
    public ICommand OpenOptionsCommand { get; }
    public ICommand LogoutCommand { get; }

    public Dashboard()
    {
        InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            _printService = new ReceiptPrintService();
            BindingContext = this;

            NavNewPaymentCommand = new Command(async () => await Navigation.PushAsync(new ShopPaymentProcessPage()));
        NavEnumerateCommand = new Command(async () => await Navigation.PushAsync(new EnumerateShopPage()));
        NavHistoryCommand = new Command(async () => await Navigation.PushAsync(new ShopPaymentHistoryPage()));
        NavExploreCommand = new Command(async () => await Navigation.PushAsync(new MarketShopsPage()));
        TestPrintCommand = new Command(async () => await _printService.PrintTestReceiptAsync());

        CenterFabActionCommand = new Command(async () =>
        {
            string action = await DisplayActionSheet("Quick Actions", "Cancel", null, "New Market Complex", "Enumerate Shop", "Collect Repayment");
            if (action == "New Market Complex") await Navigation.PushAsync(new RegisterMarketPage());
            else if (action == "Enumerate Shop") await Navigation.PushAsync(new EnumerateShopPage());
            else if (action == "Collect Repayment") await Navigation.PushAsync(new ShopPaymentProcessPage());
        });

        OpenOptionsCommand = new Command(async () =>
        {
            string opt = await DisplayActionSheet("Account Settings", "Close", null, "Change Password", "Change PIN");
            if (opt == "Change Password") await Navigation.PushModalAsync(new ChangeSecurityPopup(_agentEmail, false));
            else if (opt == "Change PIN") await Navigation.PushModalAsync(new ChangeSecurityPopup(_agentEmail, true));
        });

        LogoutCommand = new Command(async () =>
        {
            bool exit = await DisplayAlert("Logout", "Do you want to log out?", "Yes", "No");
            if (exit) await Navigation.PopToRootAsync();
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRecentShopsAsync();
    }

    private async Task LoadRecentShopsAsync()
    {
        RecentActivities.Clear();
        var markets = await _shopService.GetMarketsAsync(_agentEmail);
        if (markets.Count > 0)
        {
            var shops = await _shopService.GetMarketShopsAsync(markets[0].Id);
            for (int i = 0; i < Math.Min(shops.Count, 5); i++)
            {
                RecentActivities.Add(shops[i]);
            }
        }
    }
}
}