using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZamfaraIRS.Models; // Assuming ReceiptData and ReceiptItem are here

namespace ZamfaraIRS.Services
{
    public interface IReceiptPrintService
    {
        Task PrintTestReceiptAsync();
        Task PrintRegistrationReceiptAsync(string businessName, string payerId, string marketName, string agentEmail);

        // Dedicated Shop Print
        Task<bool> PrintShopReceiptAsync(string shopNo, string marketName, string occupant, decimal amountPaid, decimal balanceRemaining, string transactionRef, string date, string agentEmail, bool isReprint = false);

    
    }

    public class ReceiptPrintService : IReceiptPrintService
    {
        private IPrinterService GetPrinter()
        {
            return DependencyService.Get<IPrinterService>() ?? new BluetoothPrinterService(use80mm: false);
        }

        public async Task PrintTestReceiptAsync()
        {
            var printer = GetPrinter();
            if (printer != null) await printer.PrintTestPageAsync();
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

        // --- SHOP PAYMENT RECEIPT ---
        public async Task<bool> PrintShopReceiptAsync(string shopNo, string marketName, string occupant, decimal amountPaid, decimal balanceRemaining, string transactionRef, string date, string agentEmail, bool isReprint = false)
        {
            var printer = GetPrinter();
            if (printer == null) return false;

            var receipt = new ReceiptData
            {
                StoreName = "ZAMFARA STATE INTERNAL REVENUE SERVICE",
                StoreSubTitle = isReprint ? "SHOP REPAYMENT (REPRINT)" : "OFFICIAL REPAYMENT RECEIPT",
                ReceiptNumber = transactionRef,
                AgentName = agentEmail,
                CollectionPoint = marketName,
                PrintDate = DateTime.TryParse(date, out var parsedDate) ? parsedDate : DateTime.Now,
                AmountPaid = amountPaid,
                TotalAmount = amountPaid + balanceRemaining,
                AmountLeft = balanceRemaining,
                Items = new List<ReceiptItem>
                {
                    new ReceiptItem { Description = "SHOP NO", SubText = shopNo, Amount = 0 },
                    new ReceiptItem { Description = "OCCUPANT NAME", SubText = occupant, Amount = 0 },
                    new ReceiptItem { Description = "AMOUNT PAID", Amount = amountPaid }
                },
                BarcodeLabel = $"https://zamfara.osoftpay.net/verify?ref={transactionRef}",
                FooterLine1 = isReprint ? "*** REPRINTED RECEIPT ***" : "Status: APPROVED SUCCESSFUL",
                FooterLine2 = isReprint ? $"Reprinted: {DateTime.Now:dd MMM yyyy HH:mm}" : "POWERED BY OSOFTPAY"
            };

            await printer.PrintReceiptAsync(receipt, logoAssetName: null);
            return true;
        }

      
    }
}