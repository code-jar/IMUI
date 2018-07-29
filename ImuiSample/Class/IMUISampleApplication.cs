using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ImuiQS.Class
{
    public class IMUISampleApplication : Application
    {
        public override void OnCreate()
        {
            base.OnCreate();

            if (BuildConfig.DEBUG)
            {
                StrictMode.SetThreadPolicy(new StrictMode.ThreadPolicy.Builder()
                        .DetectAll()
                        .PenaltyLog()
                        .Build());

                StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder()
                        .DetectAll()
                        .PenaltyLog()
                        .Build());
            }

        }
    }
}