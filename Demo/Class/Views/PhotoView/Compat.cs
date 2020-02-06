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

namespace imuisample.Class.Views.PhotoView
{
    public class Compat
    {
        private static readonly int SIXTY_FPS_INTERVAL = 1000 / 60;

        public static void PostOnAnimation(View view, Java.Lang.IRunnable runnable)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 16)
            {
                SDK16.PostOnAnimation(view, runnable);
            }
            else
            {
                view.PostDelayed(runnable, SIXTY_FPS_INTERVAL);
            }
        }

    }
    [Android.Annotation.TargetApi(Value = 16)]
    public class SDK16
    {
        public static void PostOnAnimation(View view, Java.Lang.IRunnable r)
        {
            view.PostOnAnimation(r);
        }

    }
}