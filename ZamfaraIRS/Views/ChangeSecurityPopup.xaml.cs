using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Services;

namespace ZamfaraIRS.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChangeSecurityPopup : ContentPage, INotifyPropertyChanged
    {
        private readonly IAccountService _accountService;
        private readonly bool _isPinMode;
        private readonly string _email;

        public string TitleText => _isPinMode ? "Change Agent PIN" : "Change Password";
        public Keyboard InputKeyboard => _isPinMode ? Keyboard.Numeric : Keyboard.Default;

        public string CurrentValue { get; set; }
        public string NewValue { get; set; }

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public ChangeSecurityPopup(string email, bool isPinMode)
        {
            InitializeComponent();
            _accountService = new AccountService(new System.Net.Http.HttpClient());
            _email = email;
            _isPinMode = isPinMode;
            BindingContext = this;

            SubmitCommand = new Command(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewValue))
                {
                    await DisplayAlert("Required", "Please enter new value", "OK");
                    return;
                }

                var res = _isPinMode
                    ? await _accountService.ChangePinAsync(_email, NewValue)
                    : await _accountService.ChangePasswordAsync(_email, NewValue);

                await DisplayAlert("Result", res?.Message ?? "Updated successfully", "OK");
                await Navigation.PopModalAsync();
            });

            CancelCommand = new Command(async () => await Navigation.PopModalAsync());
        }
    }
}