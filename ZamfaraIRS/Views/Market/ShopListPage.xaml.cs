using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views.Market;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopListPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private List<ShopItemDisplayModel> _allShops = new List<ShopItemDisplayModel>();
        private List<ShopItemDisplayModel> _filteredShops = new List<ShopItemDisplayModel>();
        private const int BatchSize = 50;
        private bool _isBusy;
        private int _totalFound;

        public ObservableCollection<ShopItemDisplayModel> DisplayedShops { get; set; } = new ObservableCollection<ShopItemDisplayModel>();
        public MarketModel SelectedMarket { get; set; }

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public int TotalFound
        {
            get => _totalFound;
            set { _totalFound = value; OnPropertyChanged(); }
        }

        public ShopListPage(MarketModel market)
        {
            InitializeComponent();
            SelectedMarket = market;
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SessionManager.Instance.UpdateActivity();
            if (_allShops.Count == 0)
            {
                await FetchShopsFromApiAsync();
            }
        }

        private async Task FetchShopsFromApiAsync()
        {
            IsBusy = true;
            try
            {
                // API Endpoint 4: GET /api/Shops/{MktId}/GetMktShops
                var shops = await _shopService.GetMarketShopsAsync(SelectedMarket.Id);
                _allShops = shops.Select(s => new ShopItemDisplayModel
                {
                    ShopNo = s.ShopNo,
                    ShopCat = s.ShopCat,
                    MarketName = s.MarketName ?? SelectedMarket.Market_Plaza,
                    MarketId = s.MarketId == 0 ? SelectedMarket.Id : s.MarketId,
                    DateRecorded = s.DateRecorded,
                    LastPaymentDate = s.LastPaymentDate,
                    CurrentOccupant = s.CurrentOccupant,
                    Amount = s.Amount,
                    TotalAmtPaid = s.TotalAmtPaid,
                    Balance = s.Balance,
                    Lga = s.Lga,
                    IsSelected = false
                }).ToList();

                _filteredShops = _allShops.ToList();
                TotalFound = _filteredShops.Count;

                DisplayedShops.Clear();
                LoadNextBatch();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load market shops: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = e.NewTextValue?.Trim().ToLower() ?? string.Empty;

            _filteredShops = string.IsNullOrWhiteSpace(keyword)
                ? _allShops.ToList()
                : _allShops.Where(s => (s.ShopNo != null && s.ShopNo.ToLower().Contains(keyword)) ||
                                       (s.CurrentOccupant != null && s.CurrentOccupant.ToLower().Contains(keyword))).ToList();

            TotalFound = _filteredShops.Count;
            DisplayedShops.Clear();
            LoadNextBatch();
        }

        private void OnRemainingItemsThresholdReached(object sender, EventArgs e)
        {
            LoadNextBatch();
        }

        private void LoadNextBatch()
        {
            int currentCount = DisplayedShops.Count;
            var nextItems = _filteredShops.Skip(currentCount).Take(BatchSize).ToList();

            foreach (var item in nextItems)
            {
                DisplayedShops.Add(item);
            }
        }

        private void OnShopCardTapped(object sender, EventArgs e)
        {
            var tapGesture = sender as Element;
            if (tapGesture?.BindingContext is ShopItemDisplayModel selected)
            {
                selected.IsSelected = !selected.IsSelected;
            }
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ShopItemDisplayModel shop)
            {
                SessionManager.Instance.UpdateActivity();
                await Navigation.PushAsync(new ShopDetailsPage(shop));
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ShopItemDisplayModel : ShopItemModel, INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}