﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace imuisample.Class
{
    [Application]
    public class IMUISampleApplication : Application
    {
        public IMUISampleApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public override void OnCreate()
        {
            base.OnCreate();

            if (CN.Jiguang.Imui.BuildConfig.Debug)
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


            AndroidEnvironment.UnhandledExceptionRaiser += AppUnhandledExceptionRaiser;
            CrashExceptionHandler.Instance.Init(this);
        }

        private void AppUnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {

            System.Threading.Tasks.Task.Run(() =>
            {

                Looper.Prepare();

                Toast.MakeText(this, "AppUnhandledException:" + e.Exception.Message, ToastLength.Long).Show();

                Looper.Loop();

            });

            System.Threading.Thread.Sleep(2000);

            e.Handled = true;
        }


        protected override void Dispose(bool disposing)
        {
            AndroidEnvironment.UnhandledExceptionRaiser -= AppUnhandledExceptionRaiser;

            base.Dispose(disposing);
        }

    }

    public class CrashExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        //系统默认的UncaughtException处理类 
        private Java.Lang.Thread.IUncaughtExceptionHandler mDefaultHandler;
        //CrashHandler实例
        public static CrashExceptionHandler Instance = new CrashExceptionHandler();
        //程序的Context对象
        private Context mContext;


        private CrashExceptionHandler()
        {
        }

        public void UncaughtException(Java.Lang.Thread t, Java.Lang.Throwable e)
        {
            if (!HandleException(e) && mDefaultHandler != null)
            {
                mDefaultHandler.UncaughtException(t, e);
            }
            else
            {
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                Java.Lang.JavaSystem.Exit(1);
            }
        }

        private bool HandleException(Java.Lang.Throwable e)
        {
            if (e == null)
            {
                return false;
            }

            System.Threading.Tasks.Task.Run(() =>
            {

                Looper.Prepare();

                Toast.MakeText(mContext, "ThreadUncaughtException:" + e.Message, ToastLength.Long).Show();

                Looper.Loop();

            });

            System.Threading.Thread.Sleep(2000);

            return true;
        }

        public void Init(Context ctx)
        {
            this.mContext = ctx;
            mDefaultHandler = Java.Lang.Thread.DefaultUncaughtExceptionHandler;

            Java.Lang.Thread.DefaultUncaughtExceptionHandler = this;
        }
    }
}