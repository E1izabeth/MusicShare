using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MusicShare.Services.Platform
{
    public interface IPlatformServices
    {
        string[] PickMusicFiles();
        Bluetooth.IBluetoothConnectorImpl CreateBluetoothConnector();
        //IPAddress[] GetBroadcastAddresses();
        //IPAddress[] GetLocalAddresses();
    }

    public class PlayformContext
    {
        public static IPlatformServices Instance { get; private set; }

        public static void SetInstance(IPlatformServices instance)
        {
            Instance = instance;
        }
    }
}
