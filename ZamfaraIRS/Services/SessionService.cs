using System;
using Newtonsoft.Json;
using Xamarin.Essentials;
using ZamfaraIRS;

namespace ZamfaraIRS.Services
{
    public static class SessionService
    {
        private const string KeyRememberMe = "RememberMe";
        private const string KeyRememberPassword = "RememberPassword";
        private const string KeyEmail = "SavedEmail";
        private const string KeyPassword = "SavedPassword";
        private const string KeyToken = "AuthToken";
        private const string KeyUserData = "UserDataJson";

        public static bool IsRememberMe
        {
            get => Preferences.Get(KeyRememberMe, false);
            set => Preferences.Set(KeyRememberMe, value);
        }

        public static bool IsRememberPassword
        {
            get => Preferences.Get(KeyRememberPassword, false);
            set => Preferences.Set(KeyRememberPassword, value);
        }

        public static string SavedEmail
        {
            get => Preferences.Get(KeyEmail, string.Empty);
            set => Preferences.Set(KeyEmail, value);
        }

        public static string SavedPassword
        {
            get => Preferences.Get(KeyPassword, string.Empty);
            set => Preferences.Set(KeyPassword, value);
        }

        public static void SaveSession(string email, string jsonResponse)
        {
            SavedEmail = email;
            Preferences.Set("UserDataJson", jsonResponse ?? string.Empty);
        }
        /// <summary>
        /// Restores static session properties on MainPage from stored UserDataJson.
        /// </summary>
        public static bool RestoreSession()
        {
            try
            {
                string json = Preferences.Get(KeyUserData, string.Empty);
                string savedEmail = SavedEmail;

                if (!string.IsNullOrEmpty(json))
                {
                    var response = JsonConvert.DeserializeObject<LoginResponse>(json);
                    if (response?.agent != null)
                    {
                        var a = response.agent;
                        MainPage.ValidUserMail = string.IsNullOrEmpty(savedEmail) ? a.email : savedEmail;
                        MainPage.Passwords = a.password;
                        MainPage.Name = a.name;
                        MainPage.Pin = a.pin;
                        MainPage.Super_Agent = a.SuperAgent;
                        MainPage.Category = a.category; // Works for "keke", "shop", "market", etc.[cite: 12]
                        MainPage.CollectionPoint = a.collectionPoint;
                        MainPage.Message = response.message;
                        App.IsUserLoggedIn = true;
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(savedEmail))
                {
                    MainPage.ValidUserMail = savedEmail;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionService] RestoreSession error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Ensures static properties on MainPage are hydrated when moving across views.
        /// </summary>
        public static void EnsureSessionRestored()
        {
            if (string.IsNullOrEmpty(MainPage.ValidUserMail) || string.IsNullOrEmpty(MainPage.Name))
            {
                RestoreSession();
            }
        }

        public static void ClearSession()
        {
            Preferences.Remove(KeyToken);
            Preferences.Remove(KeyUserData);
            if (!IsRememberPassword)
            {
                Preferences.Remove(KeyPassword);
                Preferences.Remove(KeyEmail);
            }
        }
    }
}