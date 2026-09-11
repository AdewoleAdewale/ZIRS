using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views.Market;

namespace ZIRS.Views
{
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

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand LoadHistoryCommand { get; }
        public ICommand RouteDirectPaymentCommand { get; }

        public ShopPaymentHistoryPage()
        {
            InitializeComponent();

            // Using the insecure client per your established SSL bypass configuration[cite: 3, 4]
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;

            LoadHistoryCommand = new Command(async () => await ExecuteLoadHistory());

            RouteDirectPaymentCommand = new Command(async () => {
                if (string.IsNullOrWhiteSpace(ShopId))
                {
                    await DisplayAlert("Notice", "Please enter a Shop Number first.", "OK");
                    return;
                }

                // Routes to the direct payment page, bypassing standard verification
                // Auto-fills the shop ID on the payment screen
                var directPaymentPage = new DirectPaymentPage();
                directPaymentPage.ShopNumber = ShopId;
                await Navigation.PushAsync(directPaymentPage);
            });
        }

        private async Task ExecuteLoadHistory()
        {
            if (string.IsNullOrWhiteSpace(ShopId))
            {
                await DisplayAlert("Validation", "Please enter a valid Shop Number.", "OK");
                return;
            }

            IsBusy = true;
            Payments.Clear();

            try
            {
                string fromDate = StartDate.ToString("yyyy-MM-dd");
                string toDate = EndDate.ToString("yyyy-MM-dd");

                var list = await _shopService.GetShopPaymentsAsync(ShopId.Trim(), fromDate, toDate); 
                
                if (list != null && list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        Payments.Add(item);
                    }
                }
                else
                {
                    await DisplayAlert("Notice", "No payment history found for this period.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to fetch history. Please check your network.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}