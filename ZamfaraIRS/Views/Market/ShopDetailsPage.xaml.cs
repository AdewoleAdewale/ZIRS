using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZIRS.Views;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopDetailsPage : ContentPage
    {
        public ShopItemModel ShopData { get; set; }

        public ICommand HistoryCommand { get; }
        public ICommand RepaymentCommand { get; }

        public ShopDetailsPage(ShopItemModel selectedShop)
        {
            InitializeComponent();
            ShopData = selectedShop;
            BindingContext = this;

            // Route to Payment History, pre-filling the Shop No
            HistoryCommand = new Command(async () => {
                var historyPage = new ShopPaymentHistoryPage();
                historyPage.ShopId = ShopData.ShopNo; // Auto-fill search field
                await Navigation.PushAsync(historyPage);
            });

            // Route to Tax Repayment, mapping data to auto-fill the form
            RepaymentCommand = new Command(async () => {

                // Convert ShopItemModel to ShopRepaymentVerificationModel for the existing layout
                var verificationModel = new ShopRepaymentVerificationModel
                {
                    ShopNo = ShopData.ShopNo,
                    Market = ShopData.MarketName,
                    Owner = ShopData.CurrentOccupant,
                    ShopCategory = ShopData.ShopCat,
                    ShopAmount = ShopData.Amount,
                    AmountPaid = ShopData.TotalAmtPaid,
                    AmountOwed = ShopData.Balance
                };

                await Navigation.PushAsync(new ShopTaxRepaymentPage(verificationModel));
            });
        }
    }
}