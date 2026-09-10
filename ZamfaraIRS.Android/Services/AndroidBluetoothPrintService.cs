using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Java.Util;
using Xamarin.Forms;
using ZamfaraIRS.Droid.Services;
using ZamfaraIRS.Services;

[assembly: Dependency(typeof(AndroidBluetoothPrintService))]
namespace ZamfaraIRS.Droid.Services
{
    public class AndroidBluetoothPrintService : IBluetoothPrintService
    {
        // Standard SPP (Serial Port Profile) UUID for Bluetooth Printers
        private readonly UUID _sppUuid = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");

        public async Task<bool> PrintBytesAsync(byte[] data)
        {
            BluetoothSocket socket = null;
            try
            {
                var adapter = BluetoothAdapter.DefaultAdapter;
                if (adapter == null || !adapter.IsEnabled)
                    return false;

                // Grab the first paired device (Assume the MP-58T is paired)
                // For production, you may want to filter by device.Name.Contains("58")
                var device = adapter.BondedDevices.FirstOrDefault();
                if (device == null)
                    return false;

                socket = device.CreateRfcommSocketToServiceRecord(_sppUuid);
                await socket.ConnectAsync();

                if (socket.IsConnected)
                {
                    await socket.OutputStream.WriteAsync(data, 0, data.Length);
                    socket.OutputStream.Flush();

                    // Allow time for the printer buffer to process the data
                    await Task.Delay(500);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bluetooth Print Error: {ex.Message}");
                return false;
            }
            finally
            {
                if (socket != null)
                {
                    socket.Close();
                    socket.Dispose();
                }
            }
        }
    }
}