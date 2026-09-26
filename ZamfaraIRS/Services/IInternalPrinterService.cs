using System.Threading.Tasks;
using ZamfaraIRS.Models;

namespace ZamfaraIRS.Services
{
    public interface IInternalPrinterService
    {
        bool IsSmartPOSTerminal();
        Task<bool> PrintReceiptAsync(ReceiptData receipt, string logoAssetName = null);
        Task<bool> PrintTestPageAsync();
    }
}