using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Services
{
    public interface IReceiptPrintService
    {
        Task PrintTestReceiptAsync();
        Task PrintRegistrationReceiptAsync(string businessName, string payerId, string marketName, string agentEmail);
        Task<bool> PrintShopReceiptAsync(string shopNo, string marketName, string occupant, decimal amountPaid, decimal balanceRemaining, string transactionRef, string date, string agentEmail, bool isReprint = false);
        Task<bool> PrintKekeReceiptAsync(string transactionNo, string vehiclePlateNo, string serviceName, decimal amountPaid, string lga, string agentEmail, bool isReprint = false);
    }

    public class ReceiptPrintService : IReceiptPrintService
    {
        // Centralized fail-safe execution routing with hardware detection.
        //
        // Payments (isPayment: true) get the large bold vertical "ZIRS
        // Services" watermark stamped underneath the receipt; registrations
        // (isPayment: false) print the plain receipt with no watermark.
        //
        // Routing order:
        //   1. Shared Bluetooth printer service (mobile terminal paired to
        //      an external printer) — tried first.
        //   2. Trendit-based POS SDK (built-in printer on a SmartPOS
        //      terminal like the S680) — used only if step 1 is unavailable
        //      or throws.
        // Both steps are wrapped so a hardware/SDK exception on either path
        // can never crash the caller.
        private async Task<bool> SafeExecutePrintAsync(ReceiptData receipt, bool isPayment)
        {
            string watermark = isPayment ? BrandConfig.ReceiptWatermark : null;

            // 1. Shared Bluetooth service (mobile printer path). Instantiated
            // directly (not via the IPrinterService interface) because the
            // watermark-aware overload lives only on the concrete class.
            try
            {
                using (var bluetoothPrinter = new BluetoothPrinterService(use80mm: false))
                {
                    if (await bluetoothPrinter.IsPrinterAvailableAsync())
                    {
                        if (isPayment)
                        {
                            var result = await bluetoothPrinter.PrintReceiptWithWatermarkAsync(
                                receipt, watermarkText: watermark);
                            if (result.Success) return true;
                        }
                        else
                        {
                            await bluetoothPrinter.PrintReceiptAsync(receipt, logoAssetName: null, watermarkText: null);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bluetooth Printer Error: {ex.Message}");
                // Fall back to the POS terminal below.
            }

            // 2. Fall back to the Trendit-based POS SDK (built-in printer).
            try
            {
                var internalPrinter = DependencyService.Get<IInternalPrinterService>();
                if (internalPrinter != null && internalPrinter.IsSmartPOSTerminal())
                {
                    return await internalPrinter.PrintReceiptAsync(
                        receipt, logoAssetName: null, watermarkText: watermark);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"POS Printer Error: {ex.Message}");
            }

            return false;
        }

        // --- 1. SHOP REGISTRATION (No Watermark) ---
        public async Task PrintRegistrationReceiptAsync(string businessName, string payerId, string marketName, string agentEmail)
        {
            // If registrations strictly do not require *any* printout, you can return immediately. 
            // If they require a standard receipt without the watermark, it processes via SafeExecutePrintAsync with isPayment = false.
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
                BarcodeLabel = $"https://zam.osoftpay.net/verify?ref={payerId}",
                FooterLine1 = "Save this ID for reference",
                FooterLine2 = "POWERED BY OSOFTPAY"
            };

            await SafeExecutePrintAsync(receipt, isPayment: false);
        }

        // --- 2. SHOP PAYMENT (With Large Vertical Watermark) ---
        public async Task<bool> PrintShopReceiptAsync(string shopNo, string marketName, string occupant, decimal amountPaid, decimal balanceRemaining, string transactionRef, string date, string agentEmail, bool isReprint = false)
        {
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
                BarcodeLabel = $"https://zam.osoftpay.net/verify?ref={transactionRef}",
                FooterLine1 = isReprint ? "*** REPRINTED RECEIPT ***" : "Status: APPROVED SUCCESSFUL",
                FooterLine2 = isReprint ? $"Reprinted: {DateTime.Now:dd MMM yyyy HH:mm}" : "POWERED BY OSOFTPAY"
            };

            return await SafeExecutePrintAsync(receipt, isPayment: true);
        }

        // --- 3. KEKE PAYMENT (With Large Vertical Watermark) ---
        public async Task<bool> PrintKekeReceiptAsync(string transactionNo, string vehiclePlateNo, string serviceName, decimal amountPaid, string lga, string agentEmail, bool isReprint = false)
        {
            var receipt = new ReceiptData
            {
                StoreName = "ZAMFARA STATE INTERNAL REVENUE SERVICE",
                StoreSubTitle = isReprint ? "KEKE PERMIT (REPRINT)" : "KEKE / TRICYCLE TICKET",
                ReceiptNumber = transactionNo,
                AgentName = agentEmail,
                CollectionPoint = lga,
                PrintDate = DateTime.Now,
                AmountPaid = amountPaid,
                TotalAmount = amountPaid,
                Items = new List<ReceiptItem>
                {
                    new ReceiptItem { Description = serviceName, Amount = amountPaid },
                    new ReceiptItem { Description = "Vehicle Plate No", SubText = vehiclePlateNo, Amount = 0 }
                },
                BarcodeLabel = $"https://zam.osoftpay.net/verify?ref={transactionNo}",
                FooterLine1 = isReprint ? "*** REPRINTED RECEIPT ***" : "Status: APPROVED SUCCESSFUL",
                FooterLine2 = "POWERED BY OSOFTPAY"
            };

            return await SafeExecutePrintAsync(receipt, isPayment: true);
        }

        public async Task PrintTestReceiptAsync()
        {
            try
            {
                var internalPrinter = DependencyService.Get<IInternalPrinterService>();
                if (internalPrinter != null && internalPrinter.IsSmartPOSTerminal())
                {
                    await internalPrinter.PrintTestPageAsync();
                    return;
                }

                var bluetoothPrinter = DependencyService.Get<IPrinterService>() ?? new BluetoothPrinterService(use80mm: false);
                await bluetoothPrinter.PrintTestPageAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Test Print Error: {ex.Message}");
            }
        }
    }
}