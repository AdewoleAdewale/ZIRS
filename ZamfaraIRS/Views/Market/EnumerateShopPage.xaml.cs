using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class EnumerateShopPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private ObservableCollection<MarketModel> _markets = new ObservableCollection<MarketModel>();
        private ObservableCollection<ShopCategoryModel> _categories = new ObservableCollection<ShopCategoryModel>();
        private MarketModel _selectedMarket;
        private ShopCategoryModel _selectedCategory;
        private string _shopNo;
        private bool _isBusy;
        private readonly string _currentUserEmail = "agent@example.com";

        public ObservableCollection<MarketModel> Markets { get => _markets; set { _markets = value; OnPropertyChanged(); } }
        public ObservableCollection<ShopCategoryModel> Categories { get => _categories; set { _categories = value; OnPropertyChanged(); } }
        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public MarketModel SelectedMarket
        {
            get => _selectedMarket;
            set
            {
                _selectedMarket = value;
                OnPropertyChanged();
                if (_selectedMarket != null) _ = LoadCategoriesAsync(_selectedMarket.Id);
            }
        }

        public ShopCategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public ICommand EnumerateCommand { get; }

        public EnumerateShopPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            BindingContext = this;
            EnumerateCommand = new Command(async () => await ExecuteEnumerate());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadMarketsAsync();
        }

        private async Task LoadMarketsAsync()
        {
            IsBusy = true;
            Markets.Clear();
            var list = await _shopService.GetMarketsAsync(_currentUserEmail);
            foreach (var item in list) Markets.Add(item);
            IsBusy = false;
        }

        private async Task LoadCategoriesAsync(int marketId)
        {
            IsBusy = true;
            Categories.Clear();
            var list = await _shopService.GetShopCategoriesAsync(marketId, _currentUserEmail);
            foreach (var item in list) Categories.Add(item);
            IsBusy = false;
        }

        private async Task ExecuteEnumerate()
        {
            if (SelectedMarket == null || SelectedCategory == null || string.IsNullOrWhiteSpace(ShopNo))
            {
                await DisplayAlert("Validation", "Select market, category, and enter shop number", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _shopService.EnumerateShopAsync(
                    _currentUserEmail,
                    SelectedMarket.Id,
                    SelectedCategory.ShopCategoryName,
                    ShopNo.Trim()
                );

                if (result.Status == "00")
                {
                    // Trigger thermal registration receipt via SDK
                    await ShopReceiptPrinter.PrintShopRegistrationReceiptAsync(
                        result.BusinessName,
                        result.PayerId,
                        SelectedMarket.Market_Plaza,
                        _currentUserEmail,
                        isReprint: false
                    );

                    bool reprint = await DisplayAlert("Registration Successful",
                        $"{result.Message}\nAssigned ID: {result.PayerId}\n\nDo you want to reprint the registration receipt?",
                        "Reprint", "Done");

                    if (reprint)
                    {
                        await ShopReceiptPrinter.PrintShopRegistrationReceiptAsync(
                            result.BusinessName,
                            result.PayerId,
                            SelectedMarket.Market_Plaza,
                            _currentUserEmail,
                            isReprint: true
                        );
                    }

                    ShopNo = string.Empty;
                }
                else if (result.Status == "O1") // Handling the letter 'O' status response from API
                {
                    await DisplayAlert("Duplicate", "Failed! ShopName and Code already Exist", "OK");
                }
                else if (result.Status == "02")
                {
                    await DisplayAlert("Auth Error", "Agent Not Found", "OK");
                }
                else
                {
                    await DisplayAlert("Failed", result.Message, "OK");
                }
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