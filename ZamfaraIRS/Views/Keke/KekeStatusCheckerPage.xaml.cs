using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeStatusCheckerPage : ContentPage
    {
        private readonly IKekeService _kekeService;
        private string _kekeNumber;
        private bool _isBusy;
        private bool _hasResult;
        private KekeStatusResponse _result;
        private Color _statusTextColor;

        public string KekeNumber
        {
            get => _kekeNumber;
            set
            {
                _kekeNumber = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValidInput));
            }
        }

        public bool IsValidInput => !string.IsNullOrWhiteSpace(KekeNumber) && KekeNumber.Length >= 3;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        // Properties driving the new sophisticated UI
        public bool HasResult { get => _hasResult; set { _hasResult = value; OnPropertyChanged(); } }
        public KekeStatusResponse Result { get => _result; set { _result = value; OnPropertyChanged(); } }
        public Color StatusTextColor { get => _statusTextColor; set { _statusTextColor = value; OnPropertyChanged(); } }

        public ICommand CheckStatusCommand { get; }

        public KekeStatusCheckerPage()
        {
            InitializeComponent();
            _kekeService = new KekeService();
            BindingContext = this;
            CheckStatusCommand = new Command(async () => await ExecuteCheckStatusAsync());
        }

        private async Task ExecuteCheckStatusAsync()
        {
            if (!IsValidInput) return;

            // Keep global session alive
            SessionManager.Instance.UpdateActivity();

            IsBusy = true;
            HasResult = false;

            try
            {

                // Requires the Concode (SuperAgent.NewMerchantNo)[cite: 2]
                string concode = "9LF299r0afwIXMN";

                // Server-side uppercases and strips spaces, we do it here for good measure[cite: 2]
                var response = await _kekeService.GetKekeStatusAsync(KekeNumber.Trim().ToUpper(), concode);

                if (response != null && (response.Status == "00" || response.Status == "01"))
                {
                    Result = response;

                    // Status 00 = Not Owing (Green), Status 01 = Owing (Red)[cite: 2]
                    StatusTextColor = response.Status == "00" ? Color.FromHex("#059669") : Color.FromHex("#DC2626");

                    HasResult = true;
                }
                else
                {
                    await DisplayAlert("Not Found", response?.Message ?? "No record found for this Keke number.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not fetch status: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
           await ExecuteCheckStatusAsync();
        }
    }
}