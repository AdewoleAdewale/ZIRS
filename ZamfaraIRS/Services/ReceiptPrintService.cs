using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZamfaraIRS.Services
{
    public interface IReceiptPrintService
    {

        Task PrintTestReceiptAsync();
    }

    public class ReceiptPrintService : IReceiptPrintService
    {
        private byte[] BuildHeader(string title)
        {
            var builder = new StringBuilder();
            builder.AppendLine("================================");
            builder.AppendLine("     ZAMFARA STATE I.R.S.       ");
            builder.AppendLine($"         {title.ToUpper()}        ");
            builder.AppendLine("================================");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

       
      

        public async Task PrintTestReceiptAsync()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Encoding.ASCII.GetString(BuildHeader("Self Test")));
            builder.AppendLine($"Device Status: OK");
            builder.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine("\n\n\n");

            await SendToPrinterDeviceAsync(Encoding.ASCII.GetBytes(builder.ToString()));
        }

        private async Task SendToPrinterDeviceAsync(byte[] data)
        {
            // Forward byte array to your platform-specific Native Bluetooth/USB Print SDK
            await Task.Delay(100);
        }
    }
}