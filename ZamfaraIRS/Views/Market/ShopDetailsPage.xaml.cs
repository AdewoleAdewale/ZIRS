using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views.Market;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopDetailsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private ShopItemModel _shopData;
        private bool _isBusy;

        public ShopItemModel ShopData
        {
            get => _shopData;
            set { _shopData = value; OnPropertyChanged(); }
        }

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ShopDetailsPage(ShopItemModel selectedShop)
        {
            InitializeComponent();
            ShopData = selectedShop;
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
        }

        private async void OnPaymentHistoryClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            var historyPage = new ShopPaymentHistoryPage();
            historyPage.ShopId = ShopData.ShopNo;
            await Navigation.PushAsync(historyPage);
        }

        private async void OnMakeRepaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            // Route directly to the payment page, passing the shop data to auto-fill
            await Navigation.PushAsync(new ShopRepaymentPaymentPage(ShopData));
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}