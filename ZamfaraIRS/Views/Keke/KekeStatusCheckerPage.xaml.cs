using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeStatusCheckerPage : ContentPage
    {
        private readonly IKekeService _kekeService;
        private string _kekeNumber;
        private bool _isBusy;

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

            IsBusy = true;
            try
            {
                // Requires the Concode (SuperAgent.NewMerchantNo)
                string concode = MainPage.Super_Agent ?? "UNKNOWN_CONCODE";

                var result = await _kekeService.GetKekeStatusAsync(KekeNumber.Trim().ToUpper(), concode);

                if (result != null)
                {
                    // Status 00 = Not Owing, Status 01 = Owing[cite: 2]
                    string alertTitle = result.Status == "00" ? "Verified - Not Owing" : "Attention - Owing";
                    await DisplayAlert(alertTitle, result.Message, "OK");
                }
                else
                {
                    await DisplayAlert("Not Found", "No record found for this Keke number.", "OK");
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
    }
}