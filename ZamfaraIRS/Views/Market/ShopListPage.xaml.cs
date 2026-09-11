using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopListPage : ContentPage
    {
        private List<ShopItemModel> _allShops = new List<ShopItemModel>();
        private List<ShopItemModel> _filteredShops = new List<ShopItemModel>();
        private const int BatchSize = 50;

        public ObservableCollection<ShopItemModel> DisplayedShops { get; set; } = new ObservableCollection<ShopItemModel>();
        public MarketModel SelectedMarket { get; set; }
        public int TotalFound { get; set; }

        public ICommand LoadMoreShopsCommand { get; }
        public ICommand ToggleExpandCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        public ShopListPage(MarketModel market)
        {
            InitializeComponent();
            SelectedMarket = market;
            BindingContext = this;

            LoadMoreShopsCommand = new Command(LoadNextBatch);

            ToggleExpandCommand = new Command<ShopItemModel>((shop) => {
                shop.IsExpanded = !shop.IsExpanded; // Requires adding IsExpanded to your Model wrapper
                shop.ExpandIcon = shop.IsExpanded ? "plus.png" : "plus.png";
            });

            ViewDetailsCommand = new Command<ShopItemModel>(async (shop) => {
                await Navigation.PushAsync(new ShopDetailsPage(shop));
            });

            SimulateDataLoad();
        }

        private void SimulateDataLoad()
        {
            // Simulate 3000 API records
            for (int i = 1; i <= 3000; i++)
            {
                _allShops.Add(new ShopItemModel
                {
                    ShopNo = i.ToString("D3"),
                    CurrentOccupant = "ALH. SULE NABO",
                    ShopCat = "4IN1",
                    Amount = "1823744.00",
                    TotalAmtPaid = "300000.00",
                    Balance = "1523744.00",
                    LastPaymentDate = "08-11-24 12:00 AM",
                    IsExpanded = false,
                    ExpandIcon = "plus.png"
                });
            }
            _filteredShops = _allShops.ToList();
            TotalFound = _filteredShops.Count;
            OnPropertyChanged(nameof(TotalFound));
            LoadNextBatch();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = e.NewTextValue?.ToLower() ?? "";
            _filteredShops = string.IsNullOrWhiteSpace(keyword)
                ? _allShops.ToList()
                : _allShops.Where(s => s.ShopNo.ToLower().Contains(keyword)).ToList();

            TotalFound = _filteredShops.Count;
            OnPropertyChanged(nameof(TotalFound));

            DisplayedShops.Clear();
            LoadNextBatch();
        }

        private void LoadNextBatch()
        {
            var currentCount = DisplayedShops.Count;
            var nextItems = _filteredShops.Skip(currentCount).Take(BatchSize).ToList();

            foreach (var item in nextItems)
            {
                DisplayedShops.Add(item);
            }
        }
    }
}