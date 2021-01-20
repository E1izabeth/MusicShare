using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;
using MusicShare.Services.Streams;
using Windows.Networking.Sockets;

namespace MusicShare.Uwp.Services.Bluetooth
{

    internal class BluetoothDeviceChannel : DeviceChannel, IBtDeviceChannel
    {
        public StreamSocket Socket { get; }

        public BtDeviceEntryInfo Info { get; private set; }

        public BluetoothDeviceChannel(StreamSocket socket, Stream stream, StreamAsyncWriter writer, BtDeviceEntryInfo info)
            : base(stream, writer, info.Name)
        {
            this.Socket = _disposables.Add(socket);
            this.Info = info;
        }
    }
    
}