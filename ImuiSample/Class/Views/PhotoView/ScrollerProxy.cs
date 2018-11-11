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

namespace ImuiQS.Class.Views.PhotoView
{
    public abstract class ScrollerProxy
    {
        public static ScrollerProxy GetScroller(Context context)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt < (int)Android.OS.BuildVersionCodes.Gingerbread)
            {
                return new PreGingerScroller(context);
            }
            else
            {
                return new GingerScroller(context);
            }
        }

        public abstract bool ComputeScrollOffset();

        public abstract void Fling(int startX, int startY, int velocityX, int velocityY, int minX, int maxX, int minY,
                int maxY, int overX, int overY);

        public abstract void ForceFinished(bool finished);

        public abstract int CurrX { get; }

        public abstract int CurrY { get; }

        [Android.Annotation.TargetApi(Value = 9)]
        private class GingerScroller : ScrollerProxy
        {
            private OverScroller mScroller;

            public GingerScroller(Context ctx)
            {
                mScroller = new OverScroller(ctx);
            }

            public override bool ComputeScrollOffset()
            {
                return mScroller.ComputeScrollOffset();
            }

            public override void Fling(int startX, int startY, int velocityX, int velocityY, int minX, int maxX, int minY, int maxY, int overX, int overY)
            {
                mScroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY, overX, overY);
            }

            public override void ForceFinished(bool finished)
            {
                mScroller.ForceFinished(finished);
            }

            public override int CurrX => mScroller.CurrX;

            public override int CurrY => mScroller.CurrY;
        }
        private class PreGingerScroller : ScrollerProxy
        {
            private Scroller mScroller;
            public PreGingerScroller(Context ctx)
            {
                this.mScroller = new Scroller(ctx);
            }

            public override int CurrX => mScroller.CurrX;

            public override int CurrY => mScroller.CurrY;

            public override bool ComputeScrollOffset()
            {
                return mScroller.ComputeScrollOffset();
            }

            public override void Fling(int startX, int startY, int velocityX, int velocityY, int minX, int maxX, int minY, int maxY, int overX, int overY)
            {
                mScroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY);
            }

            public override void ForceFinished(bool finished)
            {
                mScroller.ForceFinished(finished);
            }
        }
    }
}