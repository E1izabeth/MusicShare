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
    }
}
