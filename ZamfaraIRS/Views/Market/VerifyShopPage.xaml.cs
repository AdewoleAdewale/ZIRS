using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views.Market;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VerifyShopPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopNo;
        private string _occupantName;
        private bool _isBusy;
        private bool _hasResult;
        private ShopNoVerificationModel _verificationResult;
        private Color _statusBannerColor = Color.FromHex("#DC2626");

        public string ShopNo
        {
            get => _shopNo;
            set { _shopNo = value; OnPropertyChanged(); }
        }

        public string OccupantName
        {
            get => _occupantName;
            set { _occupantName = value; OnPropertyChanged(); }
        }

        public new bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool HasResult
        {
            get => _hasResult;
            set { _hasResult = value; OnPropertyChanged(); }
        }

        public ShopNoVerificationModel VerificationResult
        {
            get => _verificationResult;
            set { _verificationResult = value; OnPropertyChanged(); }
        }

        public Color StatusBannerColor
        {
            get => _statusBannerColor;
            set { _statusBannerColor = value; OnPropertyChanged(); }
        }

        public VerifyShopPage()
        {
            InitializeComponent();
            _shopService = new ShopService(SslHandler.GetInsecureHttpClient());
            BindingContext = this;
        }

        private async void OnVerifyShopClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (string.IsNullOrWhiteSpace(ShopNo) || string.IsNullOrWhiteSpace(OccupantName))
            {
                await DisplayAlert("Validation", "Please input both Shop Number and Occupant Name.", "OK");
                return;
            }

            IsBusy = true;
            HasResult = false;

            try
            {
                // API Endpoint 9: GET /api/Shops/VerifyShopNo[cite: 1]
                var result = await _shopService.VerifyShopNoAsync(ShopNo.Trim(), OccupantName.Trim());

                if (result != null)
                {
                    VerificationResult = result;
                    // statusCode "00" = not owing / paid ahead, "01" = owing[cite: 1]
                    StatusBannerColor = result.StatusCode == "00" ? Color.FromHex("#059669") : Color.FromHex("#DC2626");
                    HasResult = true;
                }
                else
                {
                    await DisplayAlert("Not Found", "No registered shop matched this number and occupant name.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Verification request failed: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnDirectPaymentBypassClicked(object sender, EventArgs e)
        {
            SessionManager.Instance.UpdateActivity();

            if (VerificationResult == null) return;

            // Route directly to Direct Payment screen, pre-populating fields
            var directPaymentPage = new DirectPaymentPage();
            directPaymentPage.ShopNumber = VerificationResult.ShopNo;
            directPaymentPage.PortalMessage = VerificationResult.Message;
            directPaymentPage.Amount = VerificationResult.Amount;

            await Navigation.PushAsync(directPaymentPage);
        }

        public new event PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}