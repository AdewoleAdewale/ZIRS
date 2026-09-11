using Android.Bluetooth;
using Xamarin.Forms;
using ZamfaraIRS.Droid.Services;
using ZamfaraIRS.Services;

[assembly: Dependency(typeof(BluetoothManagerService))]
namespace ZamfaraIRS.Droid.Services
{
    public class BluetoothManagerService : IBluetoothManager
    {
        public bool IsBluetoothEnabled()
        {
            try
            {
                var adapter = BluetoothAdapter.DefaultAdapter;
                return adapter != null && adapter.IsEnabled;
            }
            catch
            {
                return false; // Safe fallback if permissions are missing
            }
        }
    }
}