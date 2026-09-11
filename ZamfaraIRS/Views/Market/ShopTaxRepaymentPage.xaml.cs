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
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopTaxRepaymentPage : ContentPage, INotifyPropertyChanged
    {
        private ShopRepaymentVerificationModel _shopData;
        private string _selectedPaymentChannel = "Select Payment Channel";
        private bool _isPopupVisible = false;
        private string _paymentReference;
        private string _amountToPay;
        private string _transactionPin;

        public ShopRepaymentVerificationModel ShopData
        {
            get => _shopData;
            set { _shopData = value; OnPropertyChanged(); }
        }

        // Bound UI Properties
        public string SelectedPaymentChannel
        {
            get => _selectedPaymentChannel;
            set { _selectedPaymentChannel = value; OnPropertyChanged(); }
        }

        public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set { _isPopupVisible = value; OnPropertyChanged(); }
        }

        public string PaymentReference
        {
            get => _paymentReference;
            set { _paymentReference = value; OnPropertyChanged(); }
        }

        public string AmountToPay
        {
            get => _amountToPay;
            set { _amountToPay = value; OnPropertyChanged(); }
        }

        public string TransactionPin
        {
            get => _transactionPin;
            set { _transactionPin = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand OpenChannelPopupCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand SelectChannelCommand { get; }
        public ICommand ProcessPaymentCommand { get; }

        public ShopTaxRepaymentPage(ShopRepaymentVerificationModel verificationData)
        {
            InitializeComponent();
            ShopData = verificationData;

            // Auto-fill the amount to pay with the current owed balance[cite: 1, 4]
            AmountToPay = verificationData.AmountOwed;
            BindingContext = this;

            OpenChannelPopupCommand = new Command(() => IsPopupVisible = true);
            ClosePopupCommand = new Command(() => IsPopupVisible = false);

            SelectChannelCommand = new Command<string>((channel) =>
            {
                SelectedPaymentChannel = channel;
                IsPopupVisible = false;
            });

            ProcessPaymentCommand = new Command(ExecutePayment);
        }

        private async void ExecutePayment()
        {
            // Validation
            if (SelectedPaymentChannel == "Select Payment Channel")
            {
                await DisplayAlert("Error", "Please select a valid payment channel.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountToPay) || string.IsNullOrWhiteSpace(TransactionPin))
            {
                await DisplayAlert("Error", "Amount and PIN are required.", "OK");
                return;
            }

            // Provide visual feedback for processing
            await DisplayAlert("Processing", $"Processing {SelectedPaymentChannel} payment for {ShopData.ShopNo}...", "OK");

            // TODO: In production, hook this into your ShopService API post and ShopReceiptPrinter[cite: 4]
            // await ShopReceiptPrinter.PrintShopPaymentReceiptAsync(...)[cite: 4]
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}