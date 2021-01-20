using System;
using System.Collections.Generic;
using System.Text;
using MusicShare.Services;

namespace MusicShare.Droid.Services.Bluetooth
{
    internal interface IBtSvcCmdHandler
    {
        void HandleConnect(ConnectBluetoothCmd cmd);
        void HandleEnable(EnableBluetoothCmd cmd);
        void HandleDisable(DisableBluetoothCmd cmd);
        void HandleRefresh(RefreshBluetoothCmd cmd);
    }

    internal abstract class BluetoothServieCommand  : ServiceCommand<BluetoothServieCommand , IBtSvcCmdHandler>
    {
    }

    internal class EnableBluetoothCmd : BluetoothServieCommand 
    {
        protected override void HandleWithImpl(IBtSvcCmdHandler svc) { svc.HandleEnable(this); }
    }

    internal class DisableBluetoothCmd : BluetoothServieCommand 
    {
        protected override void HandleWithImpl(IBtSvcCmdHandler svc) { svc.HandleDisable(this); }
    }

    internal class RefreshBluetoothCmd : BluetoothServieCommand 
    {
        protected override void HandleWithImpl(IBtSvcCmdHandler svc) { svc.HandleRefresh(this); }
    }

    internal class ConnectBluetoothCmd : BluetoothServieCommand 
    {     
        public string Address { get; }
        public Action<IBtDeviceChannel> OnSuccessCallback { get; }
        public Action<Exception> OnErrorCallback { get; }

        public ConnectBluetoothCmd(string btAddress, Action<IBtDeviceChannel> onSuccess, Action<Exception> onError)
        {
            this.Address = btAddress;
            this.OnSuccessCallback = onSuccess;
            this.OnErrorCallback = onError;
        }

        protected override void HandleWithImpl(IBtSvcCmdHandler svc) { svc.HandleConnect(this); }
    }
}
