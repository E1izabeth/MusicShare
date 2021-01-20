using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MusicShare.Interaction.Standard.Common;
using MusicShare.Services.Streams;

namespace MusicShare.Services.NetworkChannels
{
    public class NetDeviceChannel : DeviceChannel, INetDeviceChannel
    {
        public NetHostInfo Info { get; }

        public NetDeviceChannel(NetHostInfo info, Stream stream, StreamAsyncWriter asyncSender)
            : base(stream, asyncSender, info.Name)
        {
            this.Info = info;
        }
    }
}
