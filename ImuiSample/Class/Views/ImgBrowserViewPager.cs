using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace ImuiQS.Class.Views
{
    public class ImgBrowserViewPager : ViewPager
    {
        public ImgBrowserViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }
        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            try
            {
                return base.OnInterceptTouchEvent(ev);
            }
            catch (Exception e)
            {
            }
            return false;
        }
    }
}