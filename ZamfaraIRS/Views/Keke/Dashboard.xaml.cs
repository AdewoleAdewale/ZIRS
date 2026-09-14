using Android.Content.Res;
using Nancy.ModelBinding;
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
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views.Keke
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Dashboard :ContentPage, INotifyPropertyChanged
    {
        private bool _isNavigating = false;

    public string AgentName { get; set; } = MainPage.Name;
    public string LastLogin { get; set; } = DateTime.Now.ToString();
    public string CurrentDate { get; set; }

    public ICommand NavServicesCommand { get; }
    public ICommand NavCheckStatusCommand { get; }
    public ICommand NavBodyNumberCommand { get; }
    public ICommand NavAgentHistoryCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand TestPrintCommand { get; }
    public ICommand LogoutCommand { get; }

    public Dashboard()
    {
        InitializeComponent();
        BindingContext = this;

        CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy - hh:mm tt");

        NavServicesCommand = new Command(async () => await SafeNavigateAsync(new KekeServicesPage())); // Targets 6.jpeg flow
        NavCheckStatusCommand = new Command(async () => await SafeNavigateAsync(new KekeStatusCheckerPage())); // Targets 3.jpeg flow
        NavBodyNumberCommand = new Command(async () => await SafeNavigateAsync(new VerifyBodyNumberPage())); // Targets 2.jpeg flow
        NavAgentHistoryCommand = new Command(async () => await SafeNavigateAsync(new KekeHistoryPage())); // Targets 1.jpeg flow

        OpenSettingsCommand = new Command(async () => await OpenSettingsModalAsync());
        TestPrintCommand = new Command(async () => await PrintTestReceiptAsync());
        LogoutCommand = new Command(async () => await PerformLogoutAsync());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isNavigating = false;
    }

    private async Task SafeNavigateAsync(Page targetPage)
    {
        if (_isNavigating) return;
        _isNavigating = true;

        try
        {
            await Navigation.PushAsync(targetPage, true);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", ex.Message, "OK");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    // Implementation of 7.jpeg (Settings Modal)
    private async Task OpenSettingsModalAsync()
    {
        string action = await DisplayActionSheet("SETTINGS", "CANCEL", null,
            "Change Password",
            "Change PIN",
            "Test Printer",
            "App Settings",
            "Help & Support");

        switch (action)
        {
            case "Change Password":
                await Navigation.PushModalAsync(new ChangeSecurityPopup("agent@example.com", false));
                break;
            case "Change PIN":
                await Navigation.PushModalAsync(new ChangeSecurityPopup("agent@example.com", true));
                break;
            case "Test Printer":
                await PrintTestReceiptAsync();
                break;
                // Add routing for App Settings and Help as needed
        }
    }

    private async Task PrintTestReceiptAsync()
    {
        var printService = new ReceiptPrintService();
        await printService.PrintTestReceiptAsync();
    }

    private async Task PerformLogoutAsync()
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
        if (confirm)
        {
            await Navigation.PopToRootAsync();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
}