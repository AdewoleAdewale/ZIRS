using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views.Market;

namespace ZamfaraIRS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopPaymentHistoryPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopId;
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        private DateTime _endDate = DateTime.Now;
        private bool _isBusy;

        public ObservableCollection<ShopPaymentHistoryModel> Payments { get; set; } = new ObservableCollection<ShopPaymentHistoryModel>();

        public string ShopId
        {
            get => _shopId;
            set { _shopId = value; OnPropertyChanged(); }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ShopPaymentHistoryPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
        }

        private async void OnCheckHistoryClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(ShopId))
            {
                await DisplayAlert("Validation", "Please enter a Shop Number.", "OK");
                return;
            }

            IsBusy = true;
            Payments.Clear();

            try
            {
                // API Endpoint 10: GET /api/Shops/GetShopPayments[cite: 1]
                string fromDate = StartDate.ToString("yyyy-MM-dd");
                string toDate = EndDate.ToString("yyyy-MM-dd");

                var list = await _shopService.GetShopPaymentsAsync(ShopId.Trim(), fromDate, toDate);

                if (list != null && list.Count > 0)
                {
                    foreach (var item in list) Payments.Add(item);
                }
                else
                {
                    await DisplayAlert("Notice", "No approved payment history found for this range.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not load payments: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnDirectPaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(ShopId))
            {
                await DisplayAlert("Notice", "Please enter a Shop Number to proceed to direct payment.", "OK");
                return;
            }

            // Route directly to DirectPaymentPage passing shop number
            var directPage = new DirectPaymentPage();
            directPage.ShopNumber = ShopId.Trim();
            await Navigation.PushAsync(directPage);
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}