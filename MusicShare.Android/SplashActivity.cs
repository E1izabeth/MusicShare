using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicShare.Droid.Services;
using MusicShare.Droid.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicShare.Droid
{
    
    [Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AppCompatActivity
    {
        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
        }

        // Launches the startup task
        protected override void OnResume()
        {
            base.OnResume();
            //Task startupWork = new Task(() => { this.StartActivity(new Intent(Application.Context, typeof(MainActivity))); }); 
            //startupWork.Start();

            if (PlayerService.Instance == null)
                this.StartForegroundServiceCompat<PlayerService>();

            PlayerService.GetLazyService(s => this.StartActivity(new Intent(Application.Context, typeof(MainActivity))));
        }

        // Prevent the back button from canceling the startup process
        public override void OnBackPressed() { }

    }
    
}