using System.Threading.Tasks;

namespace ZamfaraIRS.Services
{
    public interface IBluetoothPrintService
    {
        Task<bool> PrintBytesAsync(byte[] data);
    }
}