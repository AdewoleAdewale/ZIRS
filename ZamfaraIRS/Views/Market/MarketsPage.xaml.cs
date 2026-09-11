using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views
{
    public partial class MarketsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private bool _isBusy;

        public ObservableCollection<MarketDisplayModel> Markets { get; set; } = new ObservableCollection<MarketDisplayModel>();

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public MarketsPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SessionManager.Instance.UpdateActivity();
            if (Markets.Count == 0)
            {
                await LoadMarketsAsync();
            }
        }

        private async Task LoadMarketsAsync()
        {
            IsBusy = true;
            try
            {
                string agentEmail = SessionService.SavedEmail ?? "agent@example.com";
                var list = await _shopService.GetMarketsAsync(agentEmail); 
                
                Markets.Clear();
                foreach (var m in list)
                {
                    Markets.Add(new MarketDisplayModel
                    {
                        Id = m.Id,
                        Market_Plaza = m.Market_Plaza,
                        MktCode = m.MktCode,
                        Lga = m.Lga,
                        DateRecorded = m.DateRecorded,
                        IsSelected = false
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not load markets: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnMarketCardTapped(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            if (sender is Element element && element.BindingContext is MarketDisplayModel selected)
            {
                selected.IsSelected = !selected.IsSelected;
            }
        }

        private async void OnViewShopsClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            if (sender is Button btn && btn.CommandParameter is MarketDisplayModel market)
            {
                await Navigation.PushAsync(new ShopListPage(market));
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MarketDisplayModel : MarketModel, INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}