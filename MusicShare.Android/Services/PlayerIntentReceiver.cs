using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicShare.Droid.Services
{
    //[BroadcastReceiver]
    //[IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    //class PlayerIntentReceiver : BroadcastReceiver
    //{
    //    public override void OnReceive(Context context, Intent intent)
    //    {
    //        if (intent.Action != AudioManager.ActionAudioBecomingNoisy)
    //            return;

    //        //signal the service to stop!
    //        var stopIntent = new Intent(PlayerService.ActionStop);
    //        context.StartService(stopIntent);
    //    }
    //}
}