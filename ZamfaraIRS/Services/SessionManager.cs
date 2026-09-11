using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ZamfaraIRS.Services
{
    public class SessionManager
    {
        private static SessionManager _instance;
        private static readonly object _lock = new object();
        private static readonly object _timerLock = new object();

        private DateTime _lastActivityTime;
        private Timer _sessionTimer;
        private const int SESSION_TIMEOUT_MINUTES = 30; // 30 minutes of inactivity allowed[cite: 12]
        private const int CHECK_INTERVAL_SECONDS = 60;
        private bool _isSessionActive = false;

        public static SessionManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new SessionManager();
                    return _instance;
                }
            }
        }

        private SessionManager()
        {
            _lastActivityTime = DateTime.Now;
        }

        /// <summary>
        /// Attempts to validate existing credentials and restore active session state[cite: 12].
        /// </summary>
        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                if (!SessionService.IsRememberMe)
                    return false;

                // Validate and hydrate user session from stored JSON
                bool restored = SessionService.RestoreSession();
                if (!restored)
                    return false;

                App.IsUserLoggedIn = true;
                StartSession();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionManager] TryAutoLoginAsync failed: {ex.Message}");
                return false;
            }
        }

        public void StartSession()
        {
            lock (_timerLock)
            {
                if (_isSessionActive) return;

                _isSessionActive = true;
                _lastActivityTime = DateTime.Now;

                _sessionTimer = new Timer(CheckSessionTimeout, null,
                    TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS),
                    TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS));

                System.Diagnostics.Debug.WriteLine("Session started");
            }
        }

        public void StopSession()
        {
            lock (_timerLock)
            {
                _isSessionActive = false;
                _sessionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _sessionTimer?.Dispose();
                _sessionTimer = null;
                System.Diagnostics.Debug.WriteLine("Session stopped");
            }
        }

        /// <summary>
        /// Call this method from buttons and entry fields in BOTH Market and Keke modules[cite: 12].
        /// </summary>
        public void UpdateActivity()
        {
            if (!_isSessionActive) return;
            _lastActivityTime = DateTime.Now;
        }

        private void CheckSessionTimeout(object state)
        {
            if (!_isSessionActive) return;

            TimeSpan inactiveTime = DateTime.Now - _lastActivityTime;

            if (inactiveTime.TotalMinutes >= SESSION_TIMEOUT_MINUTES)
            {
                System.Diagnostics.Debug.WriteLine("Session timeout - logging out user");
                StopSession();
                Device.BeginInvokeOnMainThread(async () => await LogoutAsync(isTimeout: true));
            }
        }

        public async Task LogoutAsync(bool isTimeout = false)
        {
            try
            {
                StopSession();

                App.IsUserLoggedIn = false;
                SessionService.ClearSession();

                if (isTimeout)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Session Expired",
                        "Your session has expired due to inactivity. Please login again.",
                        "OK");
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage = new NavigationPage(new MainPage())
                    {
                        BarBackgroundColor = Color.FromHex("#064E3B"),
                        BarTextColor = Color.White
                    };
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
            }
        }
    }
}