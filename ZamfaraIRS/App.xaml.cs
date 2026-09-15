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

        protected override void OnStart()
        {
            if (IsUserLoggedIn)
            {
                SessionManager.Instance.StartSession();
                // REMOVED: _ = ReceiptPrinter.RetryPendingAsync();
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