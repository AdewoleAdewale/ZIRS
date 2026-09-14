using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeTaxCollectionPage : ContentPage
    {
        private readonly IKekeService _kekeService;

        public string ServiceName { get; set; } = "DAILY TRICYCLE TICKET";
        public string Description { get; set; } = "ticketing";
        public decimal Amount { get; set; } = 200.00m;
        public string PayerId { get; set; }
        public string AgentPin { get; set; }
        public ICommand ProcessPaymentCommand { get; }

        public KekeTaxCollectionPage()
        {
            InitializeComponent();
            _kekeService = new KekeService();
            BindingContext = this;
            ProcessPaymentCommand = new Command(async () => await ProcessPaymentAsync());
        }

        private async 
        Task
ProcessPaymentAsync()
        {
            if (string.IsNullOrWhiteSpace(PayerId) || string.IsNullOrWhiteSpace(AgentPin))
            {
                await DisplayAlert("Validation", "Payer ID and PIN are required.", "OK");
                return;
            }

            try
            {
                string agentEmail = MainPage.ValidUserMail ?? "agent@example.com";
                string concode = MainPage.Super_Agent ?? "UNKNOWN_CONCODE";

                // Submitting the multipart/form-data request[cite: 2]
                var response = await _kekeService.SubmitKekeTransactionAsync(
                    ServiceName, agentEmail, Amount, PayerId.Trim().ToUpper(), AgentPin, concode);

                // RespondCode "00" is success, "06" indicates duplicate or failure[cite: 2]
                if (response != null && response.RespondCode == "00")
                {
                    await DisplayAlert("Success", response.Message, "OK");

                    // Route to printing SDK here...
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Transaction Failed", response?.ResponseMessage ?? "Failed to process payment.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("System Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
    }
}