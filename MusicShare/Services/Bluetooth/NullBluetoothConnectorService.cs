using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.Streams;

namespace MusicShare.Services.Bluetooth
{
    public interface IBluetoothConnectorImpl : IBluetoothConnector
    {
        void Start();
    }

    public class NullBluetoothConnectorService : DisposableObject, IBluetoothConnectorImpl
    {
        public event Action<IBtDeviceChannel> OnConnection;
        public event Action<BtDeviceEntryInfo> OnDiscover;
        public event Action<bool> OnStateChanged;
        public event Action<Exception> OnError;
        
        public bool IsEnabled { get; private set; } = false;

        public NullBluetoothConnectorService()
        {
        }

        public void Connect(string addr, Action<IBtDeviceChannel> onSuccess, Action<Exception> onError)
        {
        }

        public void Enable()
        {
        }

        public void Disable()
        {
        }

        public void Refresh()
        {
        }

        protected override void DisposeImpl()
        {
        }

        public void Start()
        {
        }
    }
}
