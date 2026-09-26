using System;
using System.Threading.Tasks;
using Android.Graphics;
using Android.OS;
using Com.Trendit.Basesdk;
using Com.Trendit.Basesdk.Device.Printer;
using Com.Trendit.Basesdk.Device.Printer.Format;
using Xamarin.Forms;
using ZamfaraIRS.Droid.Services;
using ZamfaraIRS.Models;
using ZamfaraIRS.Services;
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
                format.Bold = true;

                printer.PrintText(format, "ZAMFARA I.R.S.\n");
                format.Bold = false;
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

        public async Task<bool> PrintReceiptAsync(ReceiptData receipt, string logoAssetName = null, string watermarkText = null)
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
                format.Bold = true;
                format.Align = PrintAlign.FormatAlignCenter;
                printer.PrintText(format, $"{receipt.StoreName}\n");
                printer.PrintText(format, $"{receipt.StoreSubTitle}\n");

                format.Bold = false;
                format.Align = PrintAlign.FormatAlignLeft;
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
                format.Bold = true;
                printer.PrintMultiText(format, "TOTAL AMOUNT:", $"NGN {receipt.TotalAmount:N2}");
                format.Bold = false;

                printer.PrintText(format, "--------------------------------\n");

                // Barcode / QR Code
                if (!string.IsNullOrEmpty(receipt.BarcodeLabel))
                {
                    QrCodeFormat qrFormat = printer.DefaultQrCodeFormat;
                    qrFormat.Align = PrintAlign.FormatAlignCenter;
                    qrFormat.Width = 300;
                    qrFormat.Height = 300;
                    printer.PrintQrCode(qrFormat, receipt.BarcodeLabel);
                    printer.PrintText(format, "\n");
                }

                printer.PrintText(format, $"{receipt.FooterLine1}\n");
                printer.PrintText(format, $"{receipt.FooterLine2}\n");

                // Large bold vertical watermark, stamped underneath the receipt body.
                // Only set for payment receipts - registrations and the printer test
                // page pass watermarkText: null and skip this.
                if (!string.IsNullOrWhiteSpace(watermarkText))
                {
                    PrintVerticalWatermark(printer, watermarkText);
                }

                printer.PrintText(format, "\n\n\n"); // Clear tear bar

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

        // Renders "watermarkText" as one large, bold word rotated 90° (reads
        // bottom-to-top) and sends it to the printer as a single raster image,
        // underneath the receipt body and above the final tear-bar feed.
        private static void PrintVerticalWatermark(PrinterDevice printer, string watermarkText)
        {
            const float textSize = 60f;
            const int margin = 40;

            int paperWidth = printer.DefaultBitmapFormat?.Width ?? 0;
            if (paperWidth <= 0) paperWidth = 384; // 58mm fallback

            using (var paint = new Paint(PaintFlags.AntiAlias)
            {
                TextSize = textSize,
                Color = Android.Graphics.Color.Black,
                FakeBoldText = true,
            })
            {
                paint.SetTypeface(Typeface.DefaultBold);
                paint.TextAlign = Paint.Align.Center;

                int bandHeight = (int)Math.Ceiling(paint.MeasureText(watermarkText)) + margin * 2;

                using (var bmp = Bitmap.CreateBitmap(paperWidth, bandHeight, Bitmap.Config.Argb8888))
                {
                    var canvas = new Canvas(bmp);
                    canvas.DrawColor(Android.Graphics.Color.White);
                    canvas.Save();
                    canvas.Translate(paperWidth / 2f, bandHeight / 2f);
                    canvas.Rotate(-90);
                    canvas.DrawText(watermarkText, 0, 0, paint);
                    canvas.Restore();

                    var bitmapFormat = printer.DefaultBitmapFormat;
                    bitmapFormat.Align = PrintAlign.FormatAlignCenter;
                    bitmapFormat.Width = paperWidth;
                    bitmapFormat.Height = bandHeight;
                    printer.PrintBitmap(bitmapFormat, bmp);
                }
            }
        }

        private sealed class PrintListener : OnPrintTaskListener
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public PrintListener(TaskCompletionSource<bool> tcs) { _tcs = tcs; }

            public override void OnPrintResult(int resultCode)
            {
                _tcs.TrySetResult(resultCode == 0);
            }
        }
    }
}