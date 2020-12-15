using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using MusicShare.Models;
using MusicShare.ViewModels;

namespace MusicShare.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        public AppViewModel AppModel { get; }

        public MainPage()
        {
            this.BindingContext = this.AppModel = new AppViewModel();
            this.InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {

            ////// retrieve the current xamarin forms page instance
            //////var currentpage = (CoolContentPage)
            //////Xamarin.Forms.Application.Current.MainPage.Navigation.NavigationStack.LastOrDefault();

            ////// check if the page has subscribed to 
            ////// the custom back button event
            ////if (currentpage?.CustomBackButtonAction != null)
            ////{
            ////	currentpage?.CustomBackButtonAction.Invoke();
            ////}
            ////else
            ////{
            ////	base.OnBackButtonPressed();
            ////}
            ///
            return false;
        }
    }
}