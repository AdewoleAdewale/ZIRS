using System.Threading.Tasks;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Services
{
    public interface IInternalPrinterService
    {
        bool IsSmartPOSTerminal();

        /// <summary>
        /// Prints a receipt. When <paramref name="watermarkText"/> is set, a large,
        /// bold watermark is stamped underneath the receipt body before the cut.
        /// </summary>
        Task<bool> PrintReceiptAsync(ReceiptData receipt, string logoAssetName = null, string watermarkText = null);
        Task<bool> PrintTestPageAsync();
    }
}