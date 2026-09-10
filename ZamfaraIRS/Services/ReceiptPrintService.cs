using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ZamfaraIRS.Services
{
    public interface IReceiptPrintService
    {
        Task PrintTestReceiptAsync();
        Task PrintPaymentReceiptAsync(string shopNo, string marketName, string occupant, decimal amount, string transactionRef, string date);
        Task PrintRegistrationReceiptAsync(string businessName, string payerId, string marketName, string agentEmail);
    }

    public class ReceiptPrintService : IReceiptPrintService
    {
        // Helper to resolve the main Printer Service
        private IPrinterService GetPrinter()
        {
            // Resolves the IPrinterService (implemented by BluetoothPrinterService)
            // If not registered in DI, you can instantiate it directly: new BluetoothPrinterService(use80mm: false);
            return DependencyService.Get<IPrinterService>() ?? new BluetoothPrinterService(use80mm: false);
        }

        public async Task PrintPaymentReceiptAsync(string shopNo, string marketName, string occupant, decimal amount, string transactionRef, string date)
        {
            var printer = GetPrinter();
            if (printer == null) return;

            // Map your parameters to the existing ReceiptData model
            var receipt = new ReceiptData
            {
                StoreName = "ZAMFARA STATE INTERNAL REVENUE SERVICE",
                StoreSubTitle = "OFFICIAL PAYMENT RECEIPT",
                ReceiptNumber = transactionRef,
                CollectionPoint = marketName,
                PrintDate = DateTime.TryParse(date, out var parsedDate) ? parsedDate : DateTime.Now,
                AmountPaid = amount,
                TotalAmount = amount,

                Items = new List<ReceiptItem>
                {
                    new ReceiptItem { Description = "SHOP NO", SubText = shopNo, Amount = 0 },
                    new ReceiptItem { Description = "OCCUPANT", SubText = occupant, Amount = 0 },
                    new ReceiptItem { Description = "AMOUNT PAID", Amount = amount }
                },

                BarcodeLabel = $"https://zamfara.osoftpay.net/api/SingleCollections/v1/VerifyPayment?TransactId={transactionRef}",
                FooterLine1 = "APPROVED SUCCESSFUL",
                FooterLine2 = "POWERED BY OSOFTPAY"
            };

            // Send to BluetoothPrinterService
            // Note: pass null for logoAssetName if you don't have a logo embedded in the project
            await printer.PrintReceiptAsync(receipt, logoAssetName: null);
        }

        public async Task PrintTestReceiptAsync()
        {
            var printer = GetPrinter();
            if (printer == null) return;

            // Utilizes the built-in test page functionality of your BluetoothPrinterService
            await printer.PrintTestPageAsync();
        }

        public async Task PrintRegistrationReceiptAsync(string businessName, string payerId, string marketName, string agentEmail)
        {
            var printer = GetPrinter();
            if (printer == null) return;

            var receipt = new ReceiptData
            {
                StoreName = "ZAMFARA STATE INTERNAL REVENUE SERVICE",
                StoreSubTitle = "SHOP REGISTRATION",
                ReceiptNumber = payerId,
                CollectionPoint = marketName,
                AgentName = agentEmail,
                PrintDate = DateTime.Now,

                Items = new List<ReceiptItem>
                {
                    new ReceiptItem { Description = "BUSINESS NAME", SubText = businessName, Amount = 0 },
                    new ReceiptItem { Description = "SHOP ID", SubText = payerId, Amount = 0 }
                },
                FooterLine1 = "Save this ID for reference",
                FooterLine2 = "POWERED BY OSOFTPAY"
            };

            await printer.PrintReceiptAsync(receipt, logoAssetName: null);
        }
    }
}