using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace MusicShare
{
    internal static class Extensions
    {
        public static IPAddress Clone(this IPAddress address)
        {
            return new IPAddress(address.GetAddressBytes());
        }

        public static void InvokeAction(this BindableObject obj, Action act)
        {
            if (obj.Dispatcher.IsInvokeRequired)
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

        public static void CleanupAllSubscriptions(this object eventSource)
        {
            foreach (var ev in eventSource.GetType().GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var body = ev.AddMethod.ReadInstructions().ToArray();
                var fieldRef = body.OfType<IlInstruction<FieldInfo>>().FirstOrDefault();
                if (fieldRef != null)
                {
                    var calllist = ((Delegate)fieldRef.Operand.GetValue(eventSource)).GetInvocationList();
                    calllist.ForEach(d => ev.RemoveEventHandler(eventSource, d));
                }
            }
        }

        public static void CleanupSubscriptions(this object eventSource, object subscriber)
        {
            foreach (var ev in eventSource.GetType().GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var body = ev.AddMethod.ReadInstructions().ToArray();
                var fieldRef = body.OfType<IlInstruction<FieldInfo>>().FirstOrDefault();
                if (fieldRef != null)
                {
                    var calllist = ((Delegate)fieldRef.Operand.GetValue(eventSource)).GetInvocationList();
                    calllist.Where(d => d.Target == subscriber).ForEach(d => ev.RemoveEventHandler(eventSource, d));
                }
            }
        }
    }
}
