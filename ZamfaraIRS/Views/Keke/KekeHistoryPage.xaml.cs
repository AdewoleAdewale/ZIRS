using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeHistoryPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IKekeService _kekeService;
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        private DateTime _endDate = DateTime.Now;
        private bool _isBusy, _hasResults, _showEmptyState;
        private string _summaryText;

        public ObservableCollection<KekeHistoryResponseModel> Transactions { get; set; } = new ObservableCollection<KekeHistoryResponseModel>();

        public DateTime StartDate { get => _startDate; set { _startDate = value; OnPropertyChanged(); } }
        public DateTime EndDate { get => _endDate; set { _endDate = value; OnPropertyChanged(); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasResults { get => _hasResults; set { _hasResults = value; OnPropertyChanged(); } }
        public bool ShowEmptyState { get => _showEmptyState; set { _showEmptyState = value; OnPropertyChanged(); } }
        public string SummaryText { get => _summaryText; set { _summaryText = value; OnPropertyChanged(); } }

        public ICommand SearchTransactionsCommand { get; }

        public KekeHistoryPage()
        {
            InitializeComponent();
            _kekeService = new KekeService();
            BindingContext = this;
            SearchTransactionsCommand = new Command(async () => await LoadHistoryAsync());
        }

        private async Task LoadHistoryAsync()
        {
            SessionManager.Instance.UpdateActivity(); // Keep session alive
            IsBusy = true; HasResults = false; ShowEmptyState = false;
            Transactions.Clear();

            try
            {
                // The API requires "dd-MM-yyyy" based on your sample
                string formattedStart = StartDate.ToString("dd-MM-yyyy");
                string formattedEnd = EndDate.ToString("dd-MM-yyyy");
                string agentEmail = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";

                var results = await _kekeService.GetKekeTransactionsAsync(agentEmail, formattedStart, formattedEnd);

                if (results != null && results.Any())
                {
                    decimal totalAmount = 0;
                    foreach (var item in results)
                    {
                        totalAmount += item.Amount;
                        Transactions.Add(item);
                    }

                    SummaryText = $"Found {Transactions.Count} transaction(s) - Total: ₦{totalAmount:N2}";
                    HasResults = true;
                }
                else
                {
                    ShowEmptyState = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not load history: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await LoadHistoryAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}