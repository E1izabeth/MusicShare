using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MusicShare
{
    static class Extensions
    {
        public static void InvokeAction(this BindableObject obj, Action act)
        {
            if(obj.Dispatcher.IsInvokeRequired)
            {
                obj.Dispatcher.BeginInvokeOnMainThread(act);
            }
            else
            {
                act();
            }
        }

        public static string FormatPlaybackTime(this TimeSpan time)
        {
            var result = time.Minutes.ToString().PadLeft(2, '0') + ":" + time.Seconds.ToString().PadLeft(2, '0');

            if (time > TimeSpan.FromHours(1))
            {
                result = time.Truncate(TimeSpan.FromHours(1)).TotalHours.ToString() + ":" + result;
            }

            return result;
        }
    }
}
