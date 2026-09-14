using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class KekeServicesPage : ContentPage
    {
        private readonly IKekeService _kekeService;
        private bool _isBusy;

        public ObservableCollection<ServiceModel> Services { get; set; } = new ObservableCollection<ServiceModel>();
        public string CurrentDay => DateTime.Now.ToString("dd");
        public string CurrentMonth => DateTime.Now.ToString("MMMM");
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand SelectServiceCommand { get; }

        public KekeServicesPage()
        {
            InitializeComponent();
            _kekeService = new KekeService();
            BindingContext = this;

            SelectServiceCommand = new Command<ServiceModel>(async (service) =>
            {
                await Navigation.PushAsync(new KekeTaxCollectionPage(service));
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (Services.Count == 0)
            {
                await LoadServicesAsync();
            }
        }

        private async Task LoadServicesAsync()
        {
            IsBusy = true;
            try
            {
                string email = MainPage.ValidUserMail ?? SessionService.SavedEmail ?? "agent@example.com";
                var list = await _kekeService.GetServicesAsync(email);
                foreach (var item in list)
                {
                    Services.Add(item);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not load services: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}