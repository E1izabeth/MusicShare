using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MusicShare.Services.Streams;

namespace MusicShare.Services.Platform
{
    public interface IPlatformFsItem
    {
        string Name { get; }
        string Path { get; }
        bool IsDir { get; }

        IPlatformFsItem[] GetDirs();
        IPlatformFsItem[] GetFiles();
    }

    public interface IPlatformServices
    {
        string[] PickMusicFiles();
        Bluetooth.IBluetoothConnectorImpl CreateBluetoothConnector(StreamNegotiator negotiator, Guid serviceId, string localName);

        void DemandFsPermission();
        IPlatformFsItem[] GetFsRoots();

        //IPAddress[] GetBroadcastAddresses();
        //IPAddress[] GetLocalAddresses();
    }

    public class PlatformContext
    {
        public static IPlatformServices Instance { get; private set; }

        public static void SetInstance(IPlatformServices instance)
        {
            Instance = instance;
        }
    }
}
