using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicShare
{
    static class Extensions
    {
        public static void StartForegroundServiceCompat<T>(this Context context, Bundle args = null) where T : Service
        {
            var intent = new Intent(context, typeof(T));
            if (args != null)
            {
                intent.PutExtras(args);
            }

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        
        public static IEnumerable<T> Of<T>(this IEnumeration seq)
             where T : Java.Lang.Object
        {
            while(seq.HasMoreElements)
            {
                var obj = seq.NextElement();
                if (obj is T t)
                    yield return t;
            }
        }

    }
}