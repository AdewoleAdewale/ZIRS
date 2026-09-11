using Acr.UserDialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZamfaraIRS.Services;

namespace ZamfaraIRS
{
    public partial class MainPage : ContentPage
    {
        #region Static Properties
        public static string Name { get; set; }
        public static string ValidUserMail { get; set; }
        public static string Passwords { get; set; }
        public static string Pin { get; set; }
        public static string Super_Agent { get; set; }
        public static string Message { get; set; }
        public static string Category { get; set; }
        public static string CollectionPoint { get; set; }
        #endregion

        #region Private Fields
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 1;
        private const int REQUEST_TIMEOUT_SECONDS = 30;
        private readonly HttpClient _httpClient;
        private static int _loginAttempts = 0;
        private static DateTime? _lockoutUntil = null;
        private bool _isProcessing = false;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isAnimating = false;
        #endregion

        #region Constructor
        public MainPage()
        {
            try
            {
                InitializeComponent();

                ZamfaraIRS.Services.SslHandler.ConfigureSSL();

                // 2. Initialize HttpClient with SSL bypass handler
                _httpClient = ZamfaraIRS.Services.SslHandler.GetInsecureHttpClient(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));

                CheckLockoutStatus();
            }
            catch (Exception ex)
            {
                LogError("Constructor", ex);
                ShowErrorToast("Failed to initialize. Please restart the app.");
            }
        }
        #endregion

        #region Lifecycle Methods
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                CheckLockoutStatus();
                await AnimatePageAppearance();
                await LoadSavedCredentials();
            }
            catch (Exception ex)
            {
                LogError("OnAppearing", ex);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                LogError("OnDisappearing", ex);
            }
        }
        #endregion

        #region Credential Management
        private async Task LoadSavedCredentials()
        {
            try
            {
                bool hasSaved = await SecureStorageService.HasSavedCredentialsAsync();
                if (hasSaved)
                {
                    var credentials = await SecureStorageService.GetCredentialsAsync();
                    if (credentials != null)
                    {
                        Email.Text = credentials.Email;
                        Password.Text = credentials.Password;
                        RememberMeCheckbox.IsChecked = true;
                        SessionService.IsRememberMe = true;

                        await Task.WhenAll(
                            EmailBorder.FadeTo(0.7, 200),
                            PasswordBorder.FadeTo(0.7, 200)
                        );
                        await Task.WhenAll(
                            EmailBorder.FadeTo(1, 200),
                            PasswordBorder.FadeTo(1, 200)
                        );
                    }
                }
                else if (SessionService.IsRememberMe && !string.IsNullOrEmpty(SessionService.SavedEmail))
                {
                    Email.Text = SessionService.SavedEmail;
                    Password.Text = SessionService.SavedPassword;
                    RememberMeCheckbox.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                LogError("LoadSavedCredentials", ex);
            }
        }

        private void OnRememberMeLabelTapped(object sender, EventArgs e)
        {
            RememberMeCheckbox.IsChecked = !RememberMeCheckbox.IsChecked;
        }
        #endregion

        #region Animation Methods
        private async Task AnimatePageAppearance()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                LogoContainer.Scale = 0;
                LogoContainer.Opacity = 0;
                WelcomeText.TranslationY = -30;
                WelcomeText.Opacity = 0;
                SubtitleText.TranslationY = -20;
                SubtitleText.Opacity = 0;
                LoginCard.TranslationY = 50;
                LoginCard.Opacity = 0;
                FooterSection.Opacity = 0;

                var logoAnimation = LogoContainer.FadeTo(1, 300);
                var logoScale = LogoContainer.ScaleTo(1, 400, Easing.SpringOut);
                await Task.WhenAll(logoAnimation, logoScale);

                await Task.WhenAll(
                    WelcomeText.FadeTo(1, 400),
                    WelcomeText.TranslateTo(0, 0, 400, Easing.CubicOut)
                );

                await Task.Delay(100);

                await Task.WhenAll(
                    SubtitleText.FadeTo(1, 400),
                    SubtitleText.TranslateTo(0, 0, 400, Easing.CubicOut)
                );

                await Task.WhenAll(
                    LoginCard.FadeTo(1, 500),
                    LoginCard.TranslateTo(0, 0, 500, Easing.CubicOut)
                );

                await FooterSection.FadeTo(1, 400);
                _ = AnimateLogoPulse();
            }
            catch (Exception ex)
            {
                LogError("AnimatePageAppearance", ex);
            }
            finally
            {
                _isAnimating = false;
            }
        }

        private async Task AnimateLogoPulse()
        {
            try
            {
                while (true)
                {
                    await LogoContainer.ScaleTo(1.05, 1000, Easing.SinInOut);
                    await LogoContainer.ScaleTo(1.0, 1000, Easing.SinInOut);
                    await Task.Delay(2000);
                }
            }
            catch { }
        }

        private async Task AnimateInputFocus(Xamarin.Forms.View border, bool isFocused)
        {
            try
            {
                var borderView = border as Xamarin.Forms.PancakeView.PancakeView;
                if (borderView == null) return;

                if (isFocused)
                {
                    await Task.WhenAll(
                        borderView.ScaleTo(1.02, 150, Easing.CubicOut),
                        Task.Run(async () =>
                        {
                            await Task.Delay(50);
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                borderView.BorderColor = Color.FromHex("#00D084");
                                borderView.BorderThickness = 2;
                            });
                        })
                    );
                }
                else
                {
                    await borderView.ScaleTo(1.0, 150, Easing.CubicIn);
                    borderView.BorderColor = Color.FromHex("#2C3E50");
                }
            }
            catch (Exception ex)
            {
                LogError("AnimateInputFocus", ex);
            }
        }

        private async Task AnimateButtonPress()
        {
            try
            {
                await SignInButton.ScaleTo(0.95, 100);
                await SignInButton.ScaleTo(1.0, 100, Easing.SpringOut);
            }
            catch (Exception ex)
            {
                LogError("AnimateButtonPress", ex);
            }
        }

        private async Task AnimateLoadingSpinner()
        {
            try
            {
                while (LoadingOverlay.IsVisible)
                {
                    await Task.WhenAll(
                        LoaderRing1.RotateTo(360, 2000, Easing.Linear),
                        LoaderRing2.RotateTo(-360, 1500, Easing.Linear)
                    );

                    LoaderRing1.Rotation = 0;
                    LoaderRing2.Rotation = 0;
                }
            }
            catch { }
        }

        private async Task ShakeAnimation(Xamarin.Forms.View view)
        {
            try
            {
                await view.TranslateTo(-15, 0, 50);
                await view.TranslateTo(15, 0, 50);
                await view.TranslateTo(-10, 0, 50);
                await view.TranslateTo(10, 0, 50);
                await view.TranslateTo(-5, 0, 50);
                await view.TranslateTo(5, 0, 50);
                await view.TranslateTo(0, 0, 50);
            }
            catch (Exception ex)
            {
                LogError("ShakeAnimation", ex);
            }
        }

        private async Task ShowSuccessAnimation()
        {
            try
            {
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    LoginCard.BackgroundColor = Color.FromHex("#1B5E20");
                    await Task.WhenAll(
                        LoginCard.ScaleTo(1.05, 200),
                        LoginCard.FadeTo(0.8, 200)
                    );
                    await Task.WhenAll(
                        LoginCard.ScaleTo(1.0, 200),
                        LoginCard.FadeTo(1.0, 200)
                    );
                });
            }
            catch (Exception ex)
            {
                LogError("ShowSuccessAnimation", ex);
            }
        }
        #endregion

        #region Focus & Validation
        private async void OnEmailFocused(object sender, FocusEventArgs e) => await AnimateInputFocus(EmailBorder, true);
        private async void OnPasswordFocused(object sender, FocusEventArgs e) => await AnimateInputFocus(PasswordBorder, true);
        private async void OnEmailUnfocused(object sender, FocusEventArgs e) { await AnimateInputFocus(EmailBorder, false); ValidateEmail(Email.Text); }
        private async void OnPasswordUnfocused(object sender, FocusEventArgs e) { await AnimateInputFocus(PasswordBorder, false); ValidatePassword(Password.Text); }

        private void OnEmailTextChanged(object sender, TextChangedEventArgs e) { if (!string.IsNullOrWhiteSpace(e.NewTextValue)) ClearEmailError(); }
        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e) { if (!string.IsNullOrWhiteSpace(e.NewTextValue)) ClearPasswordError(); }

        private void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Forgot Password", "Please contact your Zamfara IRS administrator to reset your password.", "OK");
            });
        }

        private async void OnDismissErrorToast(object sender, EventArgs e) => await HideErrorToast();

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) { ShowEmailError("Email address is required"); return false; }
            string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(email.Trim(), emailPattern)) { ShowEmailError("Please enter a valid email address"); return false; }
            ClearEmailError();
            return true;
        }

        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) { ShowPasswordError("Password is required"); return false; }
            if (password.Length < 4) { ShowPasswordError("Password must be at least 4 characters"); return false; }
            ClearPasswordError();
            return true;
        }

        private async void ShowEmailError(string message)
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                EmailError.Text = message;
                EmailError.Opacity = 0;
                EmailError.IsVisible = true;
                EmailBorder.BorderColor = Color.FromHex("#FF6B6B");
                await Task.WhenAll(EmailError.FadeTo(1, 200), EmailBorder.ScaleTo(1.02, 100), EmailBorder.ScaleTo(1.0, 100));
            });
        }

        private async void ShowPasswordError(string message)
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                PasswordError.Text = message;
                PasswordError.Opacity = 0;
                PasswordError.IsVisible = true;
                PasswordBorder.BorderColor = Color.FromHex("#FF6B6B");
                await Task.WhenAll(PasswordError.FadeTo(1, 200), PasswordBorder.ScaleTo(1.02, 100), PasswordBorder.ScaleTo(1.0, 100));
            });
        }

        private void ClearEmailError() => Device.BeginInvokeOnMainThread(() => { EmailError.IsVisible = false; EmailBorder.BorderColor = Color.FromHex("#2C3E50"); });
        private void ClearPasswordError() => Device.BeginInvokeOnMainThread(() => { PasswordError.IsVisible = false; PasswordBorder.BorderColor = Color.FromHex("#2C3E50"); });
        #endregion

        #region Login Methods
        private async void LoginClick(object sender, EventArgs e)
        {
            try
            {
                if (_isProcessing) return;

                await AnimateButtonPress();

                if (!CheckLockoutStatus()) return;

                string myEmail = Email?.Text?.Trim();
                string myPassword = Password?.Text;

                bool isEmailValid = ValidateEmail(myEmail);
                bool isPasswordValid = ValidatePassword(myPassword);

                if (!isEmailValid || !isPasswordValid)
                {
                    ShowErrorToast("Please correct the errors and try again");
                    await ShakeAnimation(LoginCard);
                    return;
                }

                await PerformLoginAsync(myEmail, myPassword);
            }
            catch (Exception ex)
            {
                LogError("LoginClick", ex);
                ShowErrorToast("An unexpected error occurred. Please try again.");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task PerformLoginAsync(string email, string password)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _isProcessing = true;
                await ShowLoading(true, "Signing in...", "Verifying your credentials with Zamfara IRS");
                DisableLoginButton();

                (LoginResponse result, string rawJson) = await LoginRequestAsync(email, password, _cancellationTokenSource.Token);

                if (result == null)
                {
                    throw new Exception("No response received from server");
                }

                await HandleLoginResponse(result, rawJson, email, password);
            }
            catch (OperationCanceledException)
            {
                ShowErrorToast("Login request was cancelled");
            }
            catch (HttpRequestException ex)
            {
                LogError("PerformLoginAsync - HTTP", ex);
                ShowErrorToast("Unable to connect to server. Check your internet connection.");
            }
            catch (JsonException ex)
            {
                LogError("PerformLoginAsync - JSON", ex);
                ShowErrorToast("Received invalid data from server");
            }
            catch (Exception ex)
            {
                LogError("PerformLoginAsync", ex);
                ShowErrorToast($"Login failed: {ex.Message}");
            }
            finally
            {
                await ShowLoading(false);
                EnableLoginButton();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        // Inside MainPage.xaml.cs LoginRequestAsync method:
        private async Task<(LoginResponse, string)> LoginRequestAsync(string email, string password, CancellationToken cancellationToken)
        {
            string sanitizedEmail = Uri.EscapeDataString(email);
            string sanitizedPassword = Uri.EscapeDataString(password);

            // Points to the required v2 Auth endpoint for Zamfara[cite: 9]
            string url = $"https://zamfara.osoftpay.net/api/TaskPayers/v2/AgentLogin?UserName={sanitizedEmail}&Password={sanitizedPassword}";

            using (var response = await _httpClient.GetAsync(url, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginResponse>(json);

                // Starts session timer directly on successful fetch
                if (result != null && result.responseCode == "00")
                {
                    SessionService.SaveSession(email, json);
                    SessionService.RestoreSession();
                    SessionManager.Instance.StartSession();
                }

                return (result, json);
            }
        }

        private async Task HandleLoginResponse(LoginResponse result, string rawJson, string email, string password)
        {
            if (result.responseCode == "00" && result.agent != null)
            {
                _loginAttempts = 0;
                _lockoutUntil = null;

                bool rememberMe = RememberMeCheckbox.IsChecked;
                SessionService.IsRememberMe = rememberMe;
                SessionService.IsRememberPassword = rememberMe;
                SessionService.SaveSession(email, password);
                await SecureStorageService.SaveCredentialsAsync(email, password, rememberMe);

                ValidUserMail = email;
                Passwords = result.agent.password ?? string.Empty;
                Name = result.agent.name ?? "Unknown";
                Category = result.agent.category;
                Pin = result.agent.pin ?? string.Empty;
                Super_Agent = result.agent.SuperAgent ?? string.Empty;
                CollectionPoint = result.agent.collectionPoint ?? string.Empty;
                Message = result.message ?? string.Empty;
                App.IsUserLoggedIn = true;

                SessionManager.Instance.StartSession();

                await ShowSuccessAnimation();
                await NavigateToAppropriatePageAsync(result.agent);
            }
            else
            {
                _loginAttempts++;
                string errorMessage = !string.IsNullOrWhiteSpace(result.message) ? result.message : "Invalid login credentials";
                int attemptsRemaining = MAX_LOGIN_ATTEMPTS - _loginAttempts;

                if (_loginAttempts >= MAX_LOGIN_ATTEMPTS)
                {
                    _lockoutUntil = DateTime.Now.AddMinutes(LOCKOUT_DURATION_MINUTES);
                    ShowErrorToast($"Account locked for {LOCKOUT_DURATION_MINUTES} minutes");
                    await ShakeAnimation(LoginCard);
                }
                else
                {
                    ShowErrorToast($"{errorMessage} ({attemptsRemaining} attempts left)");
                    await ShakeAnimation(LoginCard);
                }
            }
        }

        private async Task NavigateToAppropriatePageAsync(Agent agent)
        {
            Page targetPage = null;
            string categoryName = agent.category?.ToLower() ?? "";

            if (categoryName == "shop" || categoryName == "market")
            {
                targetPage = new Views.Market.Dashboard();
            }
            else if (categoryName == "keke")
            {
                targetPage = new Views.Keke.Dashboard();
            }
            else if (categoryName == "business premise")
            {
                targetPage = new Views.BizPrem.Dashboard();
            }
            else if (categoryName == "haulage")
            {
                targetPage = new Views.Haulage.Dashboard();
            }
            else
            {
                targetPage = new Views.Market.Dashboard();
            }

            UserDialogs.Instance.Toast($"Welcome back, {agent.name}!\nSigned in successfully.", TimeSpan.FromSeconds(3));
            await Task.Delay(400);

            Device.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new NavigationPage(targetPage)
                {
                    BarBackgroundColor = Color.FromHex("#064E3B"),
                    BarTextColor = Color.White
                };
            });
        }
        #endregion

        #region UI Helpers
        private bool CheckLockoutStatus()
        {
            if (_lockoutUntil.HasValue && DateTime.Now < _lockoutUntil.Value)
            {
                TimeSpan remaining = _lockoutUntil.Value - DateTime.Now;
                int minutesRemaining = (int)Math.Ceiling(remaining.TotalMinutes);

                Device.BeginInvokeOnMainThread(() =>
                {
                    SignInButton.IsEnabled = false;
                    SignInButton.Opacity = 0.5;
                });

                ShowErrorToast($"Account locked. Try again in {minutesRemaining} minute(s)");
                return false;
            }
            else
            {
                if (_lockoutUntil.HasValue)
                {
                    _lockoutUntil = null;
                    _loginAttempts = 0;

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        SignInButton.IsEnabled = true;
                        SignInButton.Opacity = 1.0;
                    });
                }
                return true;
            }
        }

        private async Task ShowLoading(bool show, string message = "Loading...", string subtext = "")
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                if (show)
                {
                    LoadingText.Text = message;
                    LoadingSubtext.Text = subtext;
                    LoadingOverlay.Opacity = 0;
                    LoadingOverlay.IsVisible = true;
                    await LoadingOverlay.FadeTo(1, 250);
                    _ = AnimateLoadingSpinner();
                }
                else
                {
                    await LoadingOverlay.FadeTo(0, 250);
                    LoadingOverlay.IsVisible = false;
                }
            });
        }

        private void DisableLoginButton()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                SignInButtonText.Opacity = 0.5;
                SignInButtonLoader.IsVisible = true;
                SignInButtonLoader.IsRunning = true;
                SignInButton.IsEnabled = false;
            });
        }

        private void EnableLoginButton()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                SignInButtonText.Opacity = 1.0;
                SignInButtonLoader.IsVisible = false;
                SignInButtonLoader.IsRunning = false;
                SignInButton.IsEnabled = true;
            });
        }

        private async void ShowErrorToast(string message)
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                ErrorToastMessage.Text = message;
                ErrorToast.TranslationY = -100;
                ErrorToast.Opacity = 0;
                ErrorToast.IsVisible = true;

                await Task.WhenAll(
                    ErrorToast.TranslateTo(0, 0, 300, Easing.SpringOut),
                    ErrorToast.FadeTo(1, 300)
                );

                await Task.Delay(4000);
                await HideErrorToast();
            });
        }

        private async Task HideErrorToast()
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                if (ErrorToast.IsVisible)
                {
                    await Task.WhenAll(
                        ErrorToast.TranslateTo(0, -100, 250, Easing.CubicIn),
                        ErrorToast.FadeTo(0, 250)
                    );
                    ErrorToast.IsVisible = false;
                }
            });
        }

        private void LogError(string method, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {method}: {ex.Message}");
            if (ex.InnerException != null)
                System.Diagnostics.Debug.WriteLine($"[INNER ERROR]: {ex.InnerException.Message}");
        }
        #endregion
    }

    #region Response Models
    public class LoginResponse
    {
        public string responseCode { get; set; }
        public string message { get; set; }
        public Agent agent { get; set; }
    }

    public class Agent
    {
        public string name { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string category { get; set; }
        public string collectionPoint { get; set; }
        public string pin { get; set; }
        public string SuperAgent { get; set; }
    }
    #endregion
}