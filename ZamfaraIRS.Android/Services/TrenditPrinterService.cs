using System;
using System.Text;
using System.Threading.Tasks;
using Android.OS;
using Xamarin.Forms;
using ZamfaraIRS.Droid.Services;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
// Add your Trendit SDK binding namespace here (e.g., using Com.Trendit.Basesdk.Device.Pos;)

[assembly: Dependency(typeof(TrenditPrinterService))]
namespace ZamfaraIRS.Droid.Services
{
    public class TrenditPrinterService : IInternalPrinterService
    {
        public bool IsSmartPOSTerminal()
        {
            var manufacturer = Build.Manufacturer?.ToLower() ?? "";
            var model = Build.Model?.ToLower() ?? "";

            // 1. Detect the Trendit S680
            return manufacturer.Contains("trendit") ||
                   manufacturer.Contains("pax") ||
                   model.Contains("s680");
        }

        public async Task<bool> PrintTestPageAsync()
        {
            if (!IsSmartPOSTerminal()) return false;

            var tcs = new TaskCompletionSource<bool>();
            try
            {
                var printer = POSDeviceManager.Instance.PrinterDevice;
                printer.Clear();

                // Configure default format
                var format = printer.DefaultTextFormat;
                format.IsBold = true;

                printer.PrintText(format, "ZAMFARA I.R.S.\n");
                format.IsBold = false;
                printer.PrintText(format, "Trendit S680 Printer Test OK!\n\n\n");

                printer.StartPrint(new PrintListener(tcs));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Trendit Print Error: {ex.Message}");
                tcs.TrySetResult(false);
            }
            return await tcs.Task;
        }

        public async Task<bool> PrintReceiptAsync(ReceiptData receipt, string logoAssetName = null)
        {
            if (!IsSmartPOSTerminal()) return false;

            var tcs = new TaskCompletionSource<bool>();
            try
            {
                // 2. Get the printer
                var printer = POSDeviceManager.Instance.PrinterDevice;

                // 3. Call printer.clear()
                printer.Clear();

                var format = printer.DefaultTextFormat;

                // 4. Queue your lines
                format.IsBold = true;
                format.Align = 1; // Center
                printer.PrintText(format, $"{receipt.StoreName}\n");
                printer.PrintText(format, $"{receipt.StoreSubTitle}\n");

                format.IsBold = false;
                format.Align = 0; // Left
                printer.PrintText(format, $"Ref: {receipt.ReceiptNumber}\n");
                printer.PrintText(format, $"Date: {receipt.PrintDate:dd-MMM-yyyy HH:mm}\n");
                printer.PrintText(format, "--------------------------------\n");

                foreach (var item in receipt.Items)
                {
                    printer.PrintMultiText(format, item.Description, item.Amount > 0 ? $"NGN {item.Amount:N2}" : "");
                    if (!string.IsNullOrEmpty(item.SubText))
                    {
                        printer.PrintText(format, $"{item.SubText}\n");
                    }
                }

                printer.PrintText(format, "--------------------------------\n");
                format.IsBold = true;
                printer.PrintMultiText(format, "TOTAL AMOUNT:", $"NGN {receipt.TotalAmount:N2}");
                format.IsBold = false;

                printer.PrintText(format, "--------------------------------\n");

                // Barcode / QR Code
                if (!string.IsNullOrEmpty(receipt.BarcodeLabel))
                {
                    format.Align = 1; // Center
                    printer.PrintQrCode(receipt.BarcodeLabel, 300, 300);
                    printer.PrintText(format, "\n");
                }

                printer.PrintText(format, $"{receipt.FooterLine1}\n");
                printer.PrintText(format, $"{receipt.FooterLine2}\n\n\n\n"); // Clear tear bar

                // 5. Call printer.startPrint(listener)
                printer.StartPrint(new PrintListener(tcs));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Trendit SDK Error: {ex.Message}");
                tcs.TrySetResult(false);
            }
            return await tcs.Task;
        }

        // Native callback listener for Trendit SDK
        private class PrintListener : Java.Lang.Object, IPrintListener // Replace IPrintListener with exact SDK interface
        {
            private readonly TaskCompletionSource<bool> _tcs;
            public PrintListener(TaskCompletionSource<bool> tcs) { _tcs = tcs; }

            public void OnPrintResult(int resultCode)
            {
                // 0 means success; any other code means an error
                _tcs.TrySetResult(resultCode == 0);
            }
        }
    }
}