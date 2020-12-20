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

namespace MusicShare.Droid.Services.Impl
{
    public class PlayerException : Exception
    {
        public PlayerException(MediaCodec.CodecException e)
        {
            this.E = e;
        }

        public MediaCodec.CodecException E { get; }
    }
}