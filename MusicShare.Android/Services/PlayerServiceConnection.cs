using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicShare.Interaction.Standard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicShare.Droid.Services
{
    public class PlayerServiceBinder : Binder
    {
        public override bool IsBinderAlive { get { return true; } }

        public PlayerService Service { get; }

        public PlayerServiceBinder(PlayerService service)
        {
            this.Service = service;
        }
    }

    public class PlayerServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(PlayerServiceConnection).FullName;

        MainActivity _mainActivity;

        public PlayerServiceConnection(MainActivity activity)
        {
            this.IsConnected = false;
            this.Binder = null;
            
            _mainActivity = activity;
        }

        public bool IsConnected { get; private set; }
        public PlayerServiceBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            this.Binder = service as PlayerServiceBinder;
            this.IsConnected = this.Binder != null;

            string message = "onServiceConnected - ";
            Log.Message(TAG, $"OnServiceConnected {name.ClassName}");

            if (this.IsConnected)
            {
                message = message + " bound to service " + name.ClassName;
                // _mainActivity.UpdateUiForBoundService();
            }
            else
            {
                message = message + " not bound to service " + name.ClassName;
                // _mainActivity.UpdateUiForUnboundService();
            }

            Log.Message(TAG, message);
            // _mainActivity.timestampMessageTextView.Text = message;
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Message(TAG, $"OnServiceDisconnected {name.ClassName}");
            this.IsConnected = false;
            this.Binder = null;
            // _mainActivity.UpdateUiForUnboundService();
        }
    }

}