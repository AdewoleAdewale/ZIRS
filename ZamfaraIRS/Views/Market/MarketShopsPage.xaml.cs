using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MarketShopsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private ObservableCollection<MarketModel> _markets = new ObservableCollection<MarketModel>();
        private ObservableCollection<ShopItemModel> _shops = new ObservableCollection<ShopItemModel>();
        private MarketModel _selectedMarket;
        private bool _isBusy;

        public ObservableCollection<MarketModel> Markets { get => _markets; set { _markets = value; OnPropertyChanged(); } }
        public ObservableCollection<ShopItemModel> Shops { get => _shops; set { _shops = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public MarketModel SelectedMarket
        {
            get => _selectedMarket;
            set
            {
                _selectedMarket = value;
                OnPropertyChanged();
                if (_selectedMarket != null) _ = LoadShopsAsync(_selectedMarket.Id);
            }
        }

        public MarketShopsPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Markets.Count == 0)
            {
                var list = await _shopService.GetMarketsAsync("agent@example.com");
                foreach (var item in list) Markets.Add(item);
            }
        }

        private async Task LoadShopsAsync(int mktId)
        {
            IsBusy = true;
            Shops.Clear();
            try
            {
                var list = await _shopService.GetMarketShopsAsync(mktId);
                foreach (var item in list) Shops.Add(item);
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

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}