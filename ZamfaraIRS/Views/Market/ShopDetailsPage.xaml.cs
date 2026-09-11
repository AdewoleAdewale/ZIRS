using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views.Market;
using ZIRS.Views;

namespace ZamfaraIRS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopDetailsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private ShopItemModel _shopData;
        private bool _isBusy;

        public ShopItemModel ShopData
        {
            get => _shopData;
            set { _shopData = value; OnPropertyChanged(); }
        }

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ShopDetailsPage(ShopItemModel selectedShop)
        {
            InitializeComponent();
            ShopData = selectedShop;
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
        }

        private async void OnPaymentHistoryClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            var historyPage = new ShopPaymentHistoryPage();
            historyPage.ShopId = ShopData.ShopNo;
            await Navigation.PushAsync(historyPage);
        }

        private async void OnMakeRepaymentClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();
            IsBusy = true;

            try
            {
                // API Endpoint 8: GET /api/Shops/VerifyShopRePay
                var verifiedRepay = await _shopService.VerifyShopRepayAsync(
                    ShopData.ShopNo.Trim(),
                    ShopData.MarketId,
                    ShopData.CurrentOccupant.Trim()
                );

                if (verifiedRepay != null)
                {
                    await Navigation.PushAsync(new ShopTaxRepaymentPage(verifiedRepay));
                }
                else
                {
                    // Fallback to locally passed data if verifiedRepay endpoint fails to match exact string
                    var fallbackModel = new ShopRepaymentVerificationModel
                    {
                        ShopNo = ShopData.ShopNo,
                        Market = ShopData.MarketName,
                        MktId = ShopData.MarketId.ToString(),
                        Owner = ShopData.CurrentOccupant,
                        ShopCategory = ShopData.ShopCat,
                        ShopAmount = ShopData.Amount,
                        AmountPaid = ShopData.TotalAmtPaid,
                        AmountOwed = ShopData.Balance
                    };
                    await Navigation.PushAsync(new ShopTaxRepaymentPage(fallbackModel));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not verify repayment info: {ex.Message}", "OK");
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