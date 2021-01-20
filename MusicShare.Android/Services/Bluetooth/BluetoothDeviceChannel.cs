using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Droid.Services.Bluetooth;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services;
using MusicShare.Services.Bluetooth;
using MusicShare.Services.NetworkChannels;
using MusicShare.Services.Streams;

namespace MusicShare.Droid.Services.Bluetooth
{

    internal class BluetoothDeviceChannel : DeviceChannel, IBtDeviceChannel
    {
        public BluetoothSocket Socket { get; }

        public BtDeviceEntryInfo Info { get; }

        public BluetoothDeviceChannel(BluetoothSocket socket, BidirectionalStream stream, StreamAsyncWriter asyncWriter, string peerName)
            : base(stream, asyncWriter, peerName)
        {
            this.Socket = _disposables.Add(socket);
            this.Info = new BtDeviceEntryInfo(socket.RemoteDevice.Address, socket.RemoteDevice.Name, true);
        }
    }

}