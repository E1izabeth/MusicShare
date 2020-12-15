using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MusicShare.ViewModels
{
    public class AboutViewModel : MenuPageViewModel
    {
        public AboutViewModel()
            : base("About")
        {
            this.OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://xamarin.com"));
        }

        public ICommand OpenWebCommand { get; }
    }
}