using System;
using System.Collections.Generic;
using System.Text;

namespace MusicShare.Services.Bluetooth
{
    public class BtDeviceEntryInfo
    {
        public string Address { get; }
        public string Name { get; }
        public bool IsServicePresented { get; }

        public BtDeviceEntryInfo(string address, string name, bool isServicePresented)
        {
            this.Address = address;
            this.Name = name;
            this.IsServicePresented = isServicePresented;
        }
    }

    public interface IBluetoothConnector : IDisposable
    {
        event Action<IBtDeviceChannel> OnConnection;
        event Action<BtDeviceEntryInfo> OnDiscover;
        event Action<bool> OnStateChanged;
        event Action<Exception> OnError;

        bool IsEnabled { get; }
        // bool IsDiscovering { get; }

        void Connect(string addr, Action<IBtDeviceChannel> onSuccess, Action<Exception> onError);
        void Enable();
        void Disable();
        void Refresh();
    }

}
