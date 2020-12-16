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
    public partial class AboutPage : Grid
    {
        #region string QrData 

        public string QrData
        {
            get { return (string)this.GetValue(QrDataProperty); }
            set { this.SetValue(QrDataProperty, value); }
        }

        // Using a BindableProperty as the backing store for QrData. This enables animation, styling, binding, etc...
        public static readonly BindableProperty QrDataProperty =
            BindableProperty.Create("QrData", typeof(string), typeof(AboutPage), default(string));

        #endregion

        #region string QrScanResult 

        public string QrScanResult
        {
            get { return (string)this.GetValue(QrScanResultProperty); }
            set { this.SetValue(QrScanResultProperty, value); }
        }

        // Using a BindableProperty as the backing store for QrScanResult. This enables animation, styling, binding, etc...
        public static readonly BindableProperty QrScanResultProperty =
            BindableProperty.Create("QrScanResult", typeof(string), typeof(AboutPage), default(string));

        #endregion

        
        public AboutPage()
        {
            this.InitializeComponent();
            this.GenerateQR("http://mini.pogoda.yandex.ru");
        }

        void GenerateQR(string codeValue)
        {
            var qrCode = new ZXing.Net.Mobile.Forms.ZXingBarcodeImageView {
                BarcodeFormat = BarcodeFormat.QR_CODE,
                BarcodeOptions = new ZXing.Common.EncodingOptions() {
                    Height = 350,
                    Width = 350,
                },
                BarcodeValue = codeValue,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
            };
            // Workaround for iOS
            qrCode.WidthRequest = 350;
            qrCode.HeightRequest = 350;

            qrView.Children.Clear();
            qrView.Children.Add(qrCode);
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            var f = await Plugin.FilePicker.CrossFilePicker.Current.PickFile(new[] { "*.mp3" });

            System.Diagnostics.Debugger.Break();
            // var fd = this.Resources.OpenRawResourceFd(Resource.Raw.PianoInsideMics);
            //var filepath = f.FilePath;

            LocalUtilityContext.Current.Play(f.FilePath);
        }

        private async void btnScanQr_Clicked(object sender, EventArgs e)
        {
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();

            var result = await scanner.Scan();
            this.QrScanResult = result.BarcodeFormat + ":" + result.Text + Environment.NewLine +
                                string.Join(Environment.NewLine, result.ResultMetadata.Select(kv => kv.Key + ":" + kv.Value));

        }

    }
}