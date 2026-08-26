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
    public partial class ShopPaymentHistoryPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopNo;
        private bool _isBusy;
        private ObservableCollection<ShopPaymentHistoryModel> _payments = new ObservableCollection<ShopPaymentHistoryModel>();

        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public ObservableCollection<ShopPaymentHistoryModel> Payments { get => _payments; set { _payments = value; OnPropertyChanged(); } }

        public ICommand LoadHistoryCommand { get; }

        public ShopPaymentHistoryPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
            LoadHistoryCommand = new Command(async () => await ExecuteLoadHistory());
        }

        private async Task ExecuteLoadHistory()
        {
            if (string.IsNullOrWhiteSpace(ShopNo))
            {
                await DisplayAlert("Validation", "Please enter a shop number", "OK");
                return;
            }

            IsBusy = true;
            Payments.Clear();
            try
            {
                var list = await _shopService.GetShopPaymentsAsync(ShopNo.Trim());
                foreach (var item in list) Payments.Add(item);
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
