using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Market
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShopVerifyPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IShopService _shopService;
        private string _shopNo;
        private string _occupantName;
        private bool _isBusy;
        private bool _hasResult;
        private ShopNoVerificationModel _result;
        private Color _statusTextColor = Color.Black;

        public string ShopNo { get => _shopNo; set { _shopNo = value; OnPropertyChanged(); } }
        public string OccupantName { get => _occupantName; set { _occupantName = value; OnPropertyChanged(); } }
        public new bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public Color StatusTextColor { get => _statusTextColor; set { _statusTextColor = value; OnPropertyChanged(); } }

        public ShopNoVerificationModel Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }

        public ShopVerifyPage()
        {
            InitializeComponent();
            _shopService = new ShopService(new System.Net.Http.HttpClient());
            BindingContext = this;
            VerifyCommand = new Command(async () => await ExecuteVerify());
        }

        private async Task ExecuteVerify()
        {
            SessionManager.Instance.UpdateActivity(); 

            if (string.IsNullOrWhiteSpace(ShopNo) || string.IsNullOrWhiteSpace(OccupantName))
            {
                await DisplayAlert("Validation", "Please enter both the shop number and occupant name.", "OK");
                return;
            }

            IsBusy = true;
            HasResult = false;
            try
            {
                var res = await _shopService.VerifyShopNoAsync(ShopNo.Trim(), OccupantName.Trim());
                if (res != null)
                {
                    Result = res;
                    StatusTextColor = res.StatusCode == "00" ? Color.FromHex("#059669") : Color.FromHex("#DC2626");
                    HasResult = true;
                }
                else
                {
                    await DisplayAlert("Not Found", "No record matched the provided shop number and occupant.", "OK");
                }
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