using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using ZamfaraIRS.Services;
using ZamfaraIRS.Models; // Assume standard transaction models are here

namespace ZamfaraIRS.Views.Keke
{
    public partial class KekeHistoryPage : ContentPage, INotifyPropertyChanged
    {
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        private DateTime _endDate = DateTime.Now;
        private bool _isBusy;
        private bool _hasResults;
        private bool _showEmptyState;

        // Observable collection for the UI list
        public ObservableCollection<KekeTransactionHistoryModel> Transactions { get; set; } = new ObservableCollection<KekeTransactionHistoryModel>();

        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool HasResults
        {
            get => _hasResults;
            set { _hasResults = value; OnPropertyChanged(); }
        }

        public bool ShowEmptyState
        {
            get => _showEmptyState;
            set { _showEmptyState = value; OnPropertyChanged(); }
        }

        public ICommand SearchTransactionsCommand { get; }

        public KekeHistoryPage()
        {
            InitializeComponent();
            BindingContext = this;

            SearchTransactionsCommand = new Command(async () => await LoadHistoryAsync());
        }

        private async Task LoadHistoryAsync()
        {
            IsBusy = true;
            HasResults = false;
            ShowEmptyState = false;
            Transactions.Clear();

            try
            {
                // Format strings to avoid culture-related parsing issues server-side
                string formattedStart = StartDate.ToString("yyyy-MM-dd");
                string formattedEnd = EndDate.ToString("yyyy-MM-dd");
                string agentEmail = MainPage.ValidUserMail ?? "agent@example.com";

                // NOTE: Await actual API call here once the History endpoint is confirmed for Keke
                // Example: var results = await _kekeService.GetAgentHistoryAsync(agentEmail, formattedStart, formattedEnd);

                await Task.Delay(1000); // Simulated network delay

                // Placeholder logic to toggle empty state vs results
                if (Transactions.Count == 0)
                {
                    ShowEmptyState = true;
                }
                else
                {
                    HasResults = true;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}