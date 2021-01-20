using MusicShare.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MusicShare.ViewModels
{
    public class AboutViewModel : MenuPageViewModel
    {
        public AboutViewModel(AppStateGroupViewModel group)
            : base("About", group)
        {
            this.OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://xamarin.com"));
            
            this.TerminateCommand = new Command(() => {
                ServiceContext.Instance?.Terminate();
                ActivityContext.Instance?.Terminate();
                Application.Current.Quit();
                Environment.Exit(0);
            });
        }

        public ICommand OpenWebCommand { get; }
        
        public ICommand TerminateCommand { get; }
    }
}