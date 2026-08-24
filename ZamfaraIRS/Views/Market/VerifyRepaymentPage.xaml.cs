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
    public partial class VerifyRepaymentPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private ObservableCollection<MarketModel> _markets = new ObservableCollection<MarketModel>();
        private MarketModel _selectedMarket;
        private string _shopNo;
        private string _occupantName;
        private bool _isBusy;
        private bool _hasResult;
        private ShopRepaymentVerificationModel _repayResult;

        public ObservableCollection<MarketModel> Markets { get => _markets; set { _markets = value; OnPropertyChanged(); } }
        public MarketModel SelectedMarket { get => _selectedMarket; set { _selectedMarket = value; OnPropertyChanged(); } }
        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public string OccupantName { get => _occupantName; set { _occupantName = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }

        public ShopRepaymentVerificationModel RepayResult
        {
            get => _repayResult;
            set { _repayResult = value; OnPropertyChanged(); }
        }

        public ICommand VerifyRepayCommand { get; }

        public VerifyRepaymentPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            BindingContext = this;
            VerifyRepayCommand = new Command(async () => await ExecuteVerifyRepay());
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

        private async Task ExecuteVerifyRepay()
        {
            if (SelectedMarket == null || string.IsNullOrWhiteSpace(ShopNo) || string.IsNullOrWhiteSpace(OccupantName))
            {
                await DisplayAlert("Validation", "Please select a market, enter the shop number, and occupant name.", "OK");
                return;
            }

            IsBusy = true;
            HasResult = false;
            try
            {
                var result = await _shopService.VerifyShopRepayAsync(ShopNo.Trim(), SelectedMarket.Id, OccupantName.Trim());
                if (result != null)
                {
                    RepayResult = result;
                    HasResult = true;
                }
                else
                {
                    await DisplayAlert("Verification Failed", "Combination does not match an existing shop record.", "OK");
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