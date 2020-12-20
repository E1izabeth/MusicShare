using BruTile.Predefined;
using BruTile.Web;
using MusicShare.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing;
using ZXing.QrCode;

namespace MusicShare.Views
{
   

    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ConnectivityPage : Grid
    {
        public ConnectivityPage()
        {
            this.InitializeComponent();
        }
    }
}