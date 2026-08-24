using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopCalculatorPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopNo;
        private string _numberOfMonths = "1";
        private string _totalAmount;
        private bool _isBusy;
        private bool _hasResult;

        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public string NumberOfMonths { get => _numberOfMonths; set { _numberOfMonths = value; OnPropertyChanged(); } }
        public string TotalAmount { get => _totalAmount; set { _totalAmount = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }

        public ICommand CalculateCommand { get; }

        public ShopCalculatorPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            BindingContext = this;
            CalculateCommand = new Command(async () => await ExecuteCalculate());
        }

        private async Task ExecuteCalculate()
        {
            if (string.IsNullOrWhiteSpace(ShopNo) || !int.TryParse(NumberOfMonths, out int months) || months <= 0)
            {
                await DisplayAlert("Validation", "Please enter a valid shop number and positive number of months.", "OK");
                return;
            }

            IsBusy = true;
            HasResult = false;
            try
            {
                var result = await _shopService.CalculateAmountOwedAsync(ShopNo.Trim(), months);
                TotalAmount = result;
                HasResult = true;
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