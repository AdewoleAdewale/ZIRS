using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZamfaraIRS.Services;
using ZamfaraIRS.Views;
using ZamfaraIRS.Views.Market;

namespace ZamfaraIRS
{
    public partial class App : Application
    {
        public static bool IsUserLoggedIn { get; set; }
        public static string PrinterFooter { get; set; }
        public static string RevenueServiceName { get; set; }
        public static string CentralPortalURL { get; set; }
        public static string ThankYouMessage { get; set; }

        public static IPrinterService Printer { get; private set; }
        public static PrintJobManager PrintJobManager { get; private set; }
        public object SessionService { get; private set; }

        public App()
        {
            InitializeComponent();

            CentralPortalURL = "https://zamfara.osoftpay.net/api/SingleCollections/PostCollect/NewCollect";
            RevenueServiceName = "ZAMFARA STATE INTERNAL REVENUE SERVICE (ZIRS)";
            PrinterFooter = "POWERED BY OSOFTPAY";
            ThankYouMessage = "THANK YOU FOR MAKING YOUR PAYMENT!";

            // 1. SSL Handling
            ZamfaraIRS.Services.SslHandler.ConfigureSSL();

            // 2. Printing Subsystem & Job Queue Init
            Printer = new BluetoothPrinterService(use80mm: false);
            PrintJobManager = new PrintJobManager(Printer);

            // 3. Check Session / Auto-login
            if (SessionManager.Instance.TryAutoLoginAsync().GetAwaiter().GetResult())
            {
                IsUserLoggedIn = true;
                SessionManager.Instance.StartSession();
                MainPage = new NavigationPage(new Dashboard())
                {
                    BarBackgroundColor = Color.FromHex("#064E3B"),
                    BarTextColor = Color.White
                };
            }
            else
            {
                MainPage = new NavigationPage(new MainPage())
                {
                    BarBackgroundColor = Color.FromHex("#064E3B"),
                    BarTextColor = Color.White
                };
            }
        }

        protected override async void OnStart()
        {
            // 1. Attempt to auto-login and restore session data (including Category & Super_Agent)
            bool isLoggedIn = await SessionManager.Instance.TryAutoLoginAsync();

            if (isLoggedIn)
            {
                Page targetDashboard;

                // 2. Route the user based on their stored category profile
                // string userCategory = MainPage.Category?.ToLower() ?? "";
                // Replace this line in OnStart():
                // string userCategory = MainPage.Category?.ToLower() ?? "";

                // With the following code to safely retrieve the user category from the BindingContext, if available:
                string userCategory = "";

                if (MainPage is NavigationPage navPage && navPage.CurrentPage?.BindingContext != null)
                {
                    var categoryProperty = navPage.CurrentPage.BindingContext.GetType().GetProperty("Category");
                    if (categoryProperty != null)
                    {
                        var categoryValue = categoryProperty.GetValue(navPage.CurrentPage.BindingContext) as string;
                        userCategory = categoryValue?.ToLower() ?? "";
                    }
                }
                if (userCategory.Contains("keke") || userCategory.Contains("tricycle"))
                {
                    // Route to Keke Module
                    targetDashboard = new ZamfaraIRS.Views.Keke.Dashboard();
                }
                else
                {
                    // Default to Market/Shop Module
                    targetDashboard = new ZamfaraIRS.Views.Market.Dashboard(); // or ShopDashboardPage depending on your exact naming
                }

                // 3. Set the MainPage
                MainPage = new NavigationPage(targetDashboard)
                {
                    BarBackgroundColor = Color.FromHex("#064E3B"),
                    BarTextColor = Color.White
                };

                // 4. (Optional) Start your printer retry queue if applicable
                // _ = ReceiptPrinter.RetryPendingAsync(); 
            }
            else
            {
                // Session invalid or expired, route to Login Page
                MainPage = new NavigationPage(new MainPage())
                {
                    BarBackgroundColor = Color.FromHex("#064E3B"),
                    BarTextColor = Color.White
                };
            }
        }

        protected override void OnResume()
        {
            if (IsUserLoggedIn)
            {
                SessionManager.Instance.StartSession();
                SessionManager.Instance.UpdateActivity();
                // REMOVED: _ = ReceiptPrinter.RetryPendingAsync();
            }
        }

        protected override void OnSleep()
        {
            SessionManager.Instance.StopSession();
        }

      
    }
}