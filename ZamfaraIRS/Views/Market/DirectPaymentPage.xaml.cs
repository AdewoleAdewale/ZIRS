using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views
{
    public partial class DirectPaymentPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopNumber;
        private string _portalMessage;
        private string _monthsInput = "1";
        private string _amount;
        private string _pinInput;
        private bool _isBusy;

        public string ShopNumber { get => _shopNumber; set { _shopNumber = value; OnPropertyChanged(); } }
        public string PortalMessage { get => _portalMessage; set { _portalMessage = value; OnPropertyChanged(); } }
        public string MonthsInput { get => _monthsInput; set { _monthsInput = value; OnPropertyChanged(); } }
        public string Amount { get => _amount; set { _amount = value; OnPropertyChanged(); } }
        public string PinInput { get => _pinInput; set { _pinInput = value; OnPropertyChanged(); } }

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand ProcessPaymentCommand { get; }

        public DirectPaymentPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;

            ProcessPaymentCommand = new Command(async () => await ExecutePayment());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SessionManager.Instance.UpdateActivity();
            if (string.IsNullOrEmpty(Amount) && !string.IsNullOrEmpty(ShopNumber))
            {
                await CalculateTotalAsync();
            }
        }

        private async void OnMonthsChanged(object sender, TextChangedEventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            await CalculateTotalAsync();
        }

        private async Task CalculateTotalAsync()
        {
            if (string.IsNullOrWhiteSpace(ShopNumber)) return;

            if (!int.TryParse(MonthsInput, out int months) || months <= 0)
                months = 1;

            IsBusy = true;
            try
            {
                Amount = await _shopService.CalculateAmountOwedAsync(ShopNumber.Trim(), months);
            }
            catch
            {
                Amount = "0.00";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecutePayment()
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(Amount) || Amount == "0.00")
            {
                await DisplayAlert("Validation", "Amount cannot be zero.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length != 4)
            {
                await DisplayAlert("Validation", "A valid 4-digit PIN is required.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                await Task.Delay(1000);

                decimal.TryParse(Amount.Replace(",", ""), out decimal amountPaid);
                string refNo = $"TX-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                await ShopReceiptPrinter.PrintPaymentReceiptAsync(
                    ShopNumber,
                    "ZIRS Direct Payment",
                    "Verified Occupant",
                    amountPaid,
                    0.00m,
                    refNo,
                    DateTime.Now.ToString("dd-MMM-yyyy HH:mm"),
                    isReprint: false
                ); 

                await DisplayAlert("Success", "Payment processed and receipt printed.", "Done");
                await Navigation.PopToRootAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Payment Failed", $"Error: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}