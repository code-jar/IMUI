using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace imuisample.Class.Views
{
    public class WaveView : View
    {
        public WaveView(Context context) : base(context)
        {
        }

        public WaveView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public WaveView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }
    }
}