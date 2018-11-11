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
using Java.Interop;
using Java.Lang;

namespace CN.Jiguang.Imui.Chatinput.Camera
{
    public partial class CameraNew : global::Java.Lang.Object, global::CN.Jiguang.Imui.Chatinput.Camera.ICameraSupport
    {
        public partial class CompareSizesByArea : global::Java.Lang.Object, global::Java.Util.IComparator
        {
            public int Compare(Java.Lang.Object o1, Java.Lang.Object o2)
            {
                return this.Compare((Android.Util.Size)o1, (Android.Util.Size)o2);
            }
        }


    }

}