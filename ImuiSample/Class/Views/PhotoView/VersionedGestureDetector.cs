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
using static Android.Views.GestureDetector;

namespace ImuiQS.Class.Views.PhotoView
{
    public abstract class VersionedGestureDetector
    {
        static readonly String LOG_TAG = "VersionedGestureDetector";
        protected IOnGestureListener mListener;

        public static VersionedGestureDetector NewInstance(Context context, IOnGestureListener listener)
        {
            int sdkVersion = (int)Build.VERSION.SdkInt;
            VersionedGestureDetector detector = null;

            if (sdkVersion < (int)BuildVersionCodes.Eclair)
            {
                detector = new CupcakeDetector(context);
            }
            else if (sdkVersion < (int)BuildVersionCodes.Froyo)
            {
                detector = new EclairDetector(context);
            }
            else
            {
                detector = new FroyoDetector(context);
            }

            detector.mListener = listener;

            return detector;
        }

        public abstract bool OnTouchEvent(MotionEvent ev);

        public abstract bool IsScaling();

        public interface IOnGestureListener
        {
            void OnDrag(float dx, float dy);

            void OnFling(float startX, float startY, float velocityX, float velocityY);

            void OnScale(float scaleFactor, float focusX, float focusY);
        }

        private class CupcakeDetector : VersionedGestureDetector
        {
            protected float mLastTouchX;
            protected float mLastTouchY;
            float mTouchSlop;
            float mMinimumVelocity;

            public CupcakeDetector(Context context)
            {
                ViewConfiguration configuration = ViewConfiguration.Get(context);
                mMinimumVelocity = configuration.ScaledMinimumFlingVelocity;
                mTouchSlop = configuration.ScaledTouchSlop;
            }

            private VelocityTracker mVelocityTracker;
            private bool mIsDragging;

            public virtual float GetActiveX(MotionEvent ev)
            {
                return ev.GetX();
            }

            public virtual float GetActiveY(MotionEvent ev)
            {
                return ev.GetY();
            }

            public override bool IsScaling()
            {
                return false;
            }

            public override bool OnTouchEvent(MotionEvent ev)
            {
                switch (ev.Action)
                {
                    case MotionEventActions.Cancel:
                        #region Cancel
                        if (null != mVelocityTracker)
                        {
                            mVelocityTracker.Recycle();
                            mVelocityTracker = null;
                        }
                        #endregion
                        break;
                    case MotionEventActions.Down:
                        #region Down
                        mVelocityTracker = VelocityTracker.Obtain();
                        mVelocityTracker.AddMovement(ev);

                        mLastTouchX = GetActiveX(ev);
                        mLastTouchY = GetActiveY(ev);
                        mIsDragging = false;
                        #endregion
                        break;
                    case MotionEventActions.Move:
                        #region Move
                        float x = GetActiveX(ev);
                        float y = GetActiveY(ev);
                        float dx = x - mLastTouchX, dy = y - mLastTouchY;

                        if (!mIsDragging)
                        {
                            // Use Pythagoras to see if drag length is larger than
                            // touch slop
                            mIsDragging = Math.Sqrt((dx * dx) + (dy * dy)) >= mTouchSlop;
                        }

                        if (mIsDragging)
                        {
                            mListener.OnDrag(dx, dy);
                            mLastTouchX = x;
                            mLastTouchY = y;

                            if (null != mVelocityTracker)
                            {
                                mVelocityTracker.AddMovement(ev);
                            }
                        }
                        #endregion
                        break;
                }

                return true;
            }
        }

        [Android.Annotation.TargetApi(Value = 5)]
        private class EclairDetector : CupcakeDetector
        {
            private static int INVALID_POINTER_ID = -1;
            private int mActivePointerId = INVALID_POINTER_ID;
            private int mActivePointerIndex = 0;


            public EclairDetector(Context context) : base(context)
            {
            }

            public override float GetActiveX(MotionEvent ev)
            {
                try
                {
                    return ev.GetX(mActivePointerIndex);
                }
                catch (Exception e)
                {
                    Android.Util.Log.Debug(LOG_TAG, e.Message);
                }
                return ev.GetX();
            }
            public override float GetActiveY(MotionEvent ev)
            {
                try
                {
                    return ev.GetY(mActivePointerIndex);
                }
                catch (Exception e)
                {
                    Android.Util.Log.Debug(LOG_TAG, e.Message);
                }
                return ev.GetY();
            }

            public override bool OnTouchEvent(MotionEvent ev)
            {

                switch (ev.Action & MotionEventActions.Mask)
                {
                    case MotionEventActions.Down:
                        mActivePointerId = ev.GetPointerId(0);
                        break;
                    case MotionEventActions.Cancel:
                    case MotionEventActions.Up:
                        mActivePointerId = INVALID_POINTER_ID;
                        break;
                    case MotionEventActions.PointerUp:
                        int pointerIndex = (int)(ev.Action & MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
                        int pointerId = ev.GetPointerId(pointerIndex);
                        if (pointerId == mActivePointerId)
                        {
                            // This was our active pointer going up. Choose a new
                            // active pointer and adjust accordingly.
                            int newPointerIndex = pointerIndex == 0 ? 1 : 0;
                            mActivePointerId = ev.GetPointerId(newPointerIndex);
                            mLastTouchX = ev.GetX(newPointerIndex);
                            mLastTouchY = ev.GetY(newPointerIndex);
                        }
                        break;
                }
                mActivePointerIndex = ev.FindPointerIndex(mActivePointerId != INVALID_POINTER_ID ? mActivePointerId : 0);
                return base.OnTouchEvent(ev);
            }
        }

        [Android.Annotation.TargetApi(Value = 8)]
        private class FroyoDetector : EclairDetector
        {
            private ScaleGestureDetector mDetector;

            private ScaleGestureDetector.IOnScaleGestureListener mScaleListener;

            public FroyoDetector(Context ctx) : base(ctx)
            {
                mDetector = new ScaleGestureDetector(ctx, mScaleListener);
                mScaleListener = new ScaleGestureListener(base.mListener);
            }

            public override bool IsScaling()
            {
                return mDetector.IsInProgress;
            }
            public override bool OnTouchEvent(MotionEvent ev)
            {
                mDetector.OnTouchEvent(ev);
                return base.OnTouchEvent(ev);
            }



            class ScaleGestureListener : Java.Lang.Object, ScaleGestureDetector.IOnScaleGestureListener
            {
                private IOnGestureListener mListener;
                public ScaleGestureListener(IOnGestureListener listener)
                {
                    mListener = listener;
                }
                public bool OnScale(ScaleGestureDetector detector)
                {
                    mListener.OnScale(detector.ScaleFactor, detector.FocusX, detector.FocusY);
                    return true;
                }

                public bool OnScaleBegin(ScaleGestureDetector detector)
                {
                    return true;
                }

                public void OnScaleEnd(ScaleGestureDetector detector)
                {

                }
            }

        }

    }
}