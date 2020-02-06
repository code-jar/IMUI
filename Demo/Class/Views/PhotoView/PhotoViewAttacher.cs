using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace imuisample.Class.Views.PhotoView
{
    public class PhotoViewAttacher : Java.Lang.Object,
        IPhotoView, View.IOnTouchListener,
        VersionedGestureDetector.IOnGestureListener,
        GestureDetector.IOnDoubleTapListener,
        ViewTreeObserver.IOnGlobalLayoutListener
    {
        #region Fileds
        private static readonly String LOG_TAG = "PhotoViewAttacher";
        private static readonly bool DEBUG = Android.Util.Log.IsLoggable(LOG_TAG, Android.Util.LogPriority.Debug);
        private readonly int EDGE_NONE = -1;
        private readonly int EDGE_LEFT = 0;
        private readonly int EDGE_RIGHT = 1;
        private static readonly int EDGE_BOTH = 2;

        private static readonly float DEFAULT_MAX_SCALE = 3.0f;
        private static readonly float DEFAULT_MID_SCALE = 1.75f;
        private static readonly float DEFAULT_MIN_SCALE = 1.0f;

        private float mMinScale = DEFAULT_MIN_SCALE;
        private float mMidScale = DEFAULT_MID_SCALE;
        private float mMaxScale = DEFAULT_MAX_SCALE;

        private static bool mAllowParentInterceptOnEdge = true;

        private static WeakReference<ImageView> mImageView;
        private static ViewTreeObserver mViewTreeObserver;

        // Gesture Detectors
        private GestureDetector mGestureDetector;
        private VersionedGestureDetector mScaleDragDetector;

        // These are set so we don't keep allocating them on the heap
        private Matrix mBaseMatrix = new Matrix();
        private Matrix mDrawMatrix = new Matrix();
        private Matrix mSuppMatrix = new Matrix();
        private RectF mDisplayRect = new RectF();
        private float[] mMatrixValues = new float[9];

        // Listeners
        private IOnMatrixChangedListener mMatrixChangeListener;
        private IOnPhotoTapListener mPhotoTapListener;
        private IOnViewTapListener mViewTapListener;
        private View.IOnLongClickListener mLongClickListener;

        private int mIvTop, mIvRight, mIvBottom, mIvLeft;
        private FlingRunnable mCurrentFlingRunnable;
        private int mScrollEdge = EDGE_BOTH;

        private bool mZoomEnabled;
        private ImageView.ScaleType mScaleType = ImageView.ScaleType.FitCenter;
        private Context mContext;
        private bool mFromChatActivity;
        private bool mTitleBarVisible = true;

        #endregion

        public PhotoViewAttacher(ImageView imageView, bool fromChatActivity, Context context)
        {
            mImageView = new WeakReference<ImageView>(imageView);
            mFromChatActivity = fromChatActivity;
            mContext = context;
            imageView.SetOnTouchListener(this);

            mViewTreeObserver = imageView.ViewTreeObserver;
            mViewTreeObserver.AddOnGlobalLayoutListener(this);

            // Make sure we using MATRIX Scale Type
            SetImageViewScaleTypeMatrix(imageView);

            if (!imageView.IsInEditMode)
            {
                // Create Gesture Detectors...
                mScaleDragDetector = VersionedGestureDetector.NewInstance(imageView.Context, this);

                mGestureDetector = new GestureDetector(imageView.Context, new MySimpleOnGestureListener(mLongClickListener));

                mGestureDetector.SetOnDoubleTapListener(this);

                // Finally, update the UI so that we're zoomable

                SetZoomable(true);
                Update();
            }
        }

        private void CheckZoomLevels(float minZoom, float midZoom, float maxZoom)
        {
            if (minZoom >= midZoom)
            {
                throw new Java.Lang.IllegalArgumentException("MinZoom should be less than MidZoom");
            }
            else if (midZoom >= maxZoom)
            {
                throw new Java.Lang.IllegalArgumentException("MidZoom should be less than MaxZoom");
            }
        }
        private bool HasDrawable(ImageView imageView)
        {
            return null != imageView && null != imageView.Drawable;
        }
        private bool IsSupportedScaleType(ImageView.ScaleType scaleType)
        {
            if (null == scaleType)
            {
                return false;
            }

            if (scaleType == ImageView.ScaleType.Matrix)
                throw new Java.Lang.IllegalArgumentException(scaleType.Name() + " is not supported in PhotoView");

            return true;
        }
        private void SetImageViewScaleTypeMatrix(ImageView imageView)
        {
            if (null != imageView)
            {
                if (imageView.GetType().IsInstanceOfType(typeof(PhotoView)))
                {
                    /**
                     * PhotoView sets it's own ScaleType to Matrix, then diverts all
                     * calls setScaleType to this.setScaleType. Basically we don't
                     * need to do anything here
                     */
                }
                else
                {
                    imageView.SetScaleType(ImageView.ScaleType.Matrix);
                }
            }
        }
        public void Update()
        {
            ImageView imageView = GetImageView();

            if (null != imageView)
            {
                if (mZoomEnabled)
                {
                    // Make sure we using MATRIX Scale Type
                    SetImageViewScaleTypeMatrix(imageView);

                    // Update the base matrix using the current drawable
                    UpdateBaseMatrix(imageView.Drawable);
                }
                else
                {
                    // Reset the Matrix...
                    ResetMatrix();
                }
            }
        }
        private void SetZoomable(bool v)
        {
            mZoomEnabled = true;
        }
        public void Cleanup()
        {
            if (null != mImageView)
            {
                mImageView.TryGetTarget(out ImageView v);
                v.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
            }
            mViewTreeObserver = null;

            // Clear listeners too
            mMatrixChangeListener = null;
            mPhotoTapListener = null;
            mViewTapListener = null;

            // Finally, clear ImageView
            mImageView = null;
        }
        private void UpdateBaseMatrix(Drawable drawable)
        {
            ImageView imageView = GetImageView();
            if (null == imageView || null == drawable)
            {
                return;
            }

            float viewWidth = imageView.Width;
            float viewHeight = imageView.Height;
            int drawableWidth = drawable.IntrinsicWidth;
            int drawableHeight = drawable.IntrinsicHeight;

            mBaseMatrix.Reset();

            float widthScale = viewWidth / drawableWidth;
            float heightScale = viewHeight / drawableHeight;

            if (mScaleType == ImageView.ScaleType.Center)
            {
                mBaseMatrix.PostTranslate((viewWidth - drawableWidth) / 2F, (viewHeight - drawableHeight) / 2F);

            }
            else if (mScaleType == ImageView.ScaleType.CenterCrop)
            {
                float scale = Math.Max(widthScale, heightScale);
                mBaseMatrix.PostScale(scale, scale);
                mBaseMatrix.PostTranslate((viewWidth - drawableWidth * scale) / 2F,
                        (viewHeight - drawableHeight * scale) / 2F);

            }
            else if (mScaleType == ImageView.ScaleType.CenterInside)
            {
                float scale = Math.Min(1.0f, Math.Min(widthScale, heightScale));
                mBaseMatrix.PostScale(scale, scale);
                mBaseMatrix.PostTranslate((viewWidth - drawableWidth * scale) / 2F,
                        (viewHeight - drawableHeight * scale) / 2F);

            }
            else
            {
                RectF mTempSrc = new RectF(0, 0, drawableWidth, drawableHeight);
                RectF mTempDst = new RectF(0, 0, viewWidth, viewHeight);


                if (mScaleType == ImageView.ScaleType.FitCenter)
                {
                    mBaseMatrix.SetRectToRect(mTempSrc, mTempDst, Matrix.ScaleToFit.Center);
                }
                else if (mScaleType == ImageView.ScaleType.FitStart)
                {
                    mBaseMatrix.SetRectToRect(mTempSrc, mTempDst, Matrix.ScaleToFit.Start);
                }
                else if (mScaleType == ImageView.ScaleType.FitEnd)
                {
                    mBaseMatrix.SetRectToRect(mTempSrc, mTempDst, Matrix.ScaleToFit.End);
                }
                else if (mScaleType == ImageView.ScaleType.FitXy)
                {
                    mBaseMatrix.SetRectToRect(mTempSrc, mTempDst, Matrix.ScaleToFit.Fill);
                }
            }

            ResetMatrix();

        }
        private void SetImageViewMatrix(Matrix matrix)
        {
            ImageView imageView = GetImageView();
            if (imageView == null)
                return;

            CheckImageViewScaleType();
            imageView.ImageMatrix = matrix;

            // Call MatrixChangedListener if needed
            if (null != mMatrixChangeListener)
            {
                RectF displayRect = GetDisplayRect(matrix);
                if (null != displayRect)
                {
                    mMatrixChangeListener.OnMatrixChanged(displayRect);
                }
            }
        }
        private void CheckImageViewScaleType()
        {
            ImageView imageView = GetImageView();

            /**
             * PhotoView's getScaleType() will just divert to this.getScaleType() so
             * only call if we're not attached to a PhotoView.
             */
            if (null != imageView && !imageView.GetType().IsInstanceOfType(typeof(PhotoView)))
            {
                if (imageView.GetScaleType() != ImageView.ScaleType.Matrix)
                {
                    throw new Java.Lang.IllegalStateException(
                            "The ImageView's ScaleType has been changed since attaching a PhotoViewAttacher");
                }
            }
        }
        private Matrix GetDisplayMatrix()
        {
            mDrawMatrix.Set(mBaseMatrix);
            mDrawMatrix.PostConcat(mSuppMatrix);
            return mDrawMatrix;
        }
        private void CancelFling()
        {
            if (null != mCurrentFlingRunnable)
            {
                mCurrentFlingRunnable.CancelFling();
                mCurrentFlingRunnable = null;
            }
        }
        private ImageView GetImageView()
        {
            ImageView imageView = null;

            if (null != mImageView)
            {
                mImageView.TryGetTarget(out imageView);
            }

            // If we don't have an ImageView, call cleanup()
            if (null == imageView)
            {
                Cleanup();
                throw new Java.Lang.IllegalStateException(
                        "ImageView no longer exists. You should not use this PhotoViewAttacher any more.");
            }

            return imageView;
        }
        private float GetScale()
        {
            return GetValue(mSuppMatrix, Matrix.MscaleX);
        }
        private float GetValue(Matrix matrix, int whichValue)
        {
            matrix.GetValues(mMatrixValues);
            return mMatrixValues[whichValue];
        }
        private void CheckAndDisplayMatrix()
        {
            CheckMatrixBounds();
            SetImageViewMatrix(GetDisplayMatrix());
        }
        private void ResetMatrix()
        {
            mSuppMatrix.Reset();
            SetImageViewMatrix(GetDisplayMatrix());
            CheckMatrixBounds();
        }

        private void CheckMatrixBounds()
        {
            ImageView imageView = GetImageView();
            if (null == imageView)
            {
                return;
            }

            RectF rect = GetDisplayRect(GetDisplayMatrix());
            if (null == rect)
            {
                return;
            }

            float height = rect.Height(), width = rect.Width();
            float deltaX = 0, deltaY = 0;

            int viewHeight = imageView.Height;
            if (height <= viewHeight)
            {
                if (mScaleType == ImageView.ScaleType.FitStart)
                {
                    deltaY = -rect.Top;
                }
                else if (mScaleType == ImageView.ScaleType.FitEnd)
                {
                    deltaY = viewHeight - height - rect.Top;
                }
                else
                {
                    deltaY = (viewHeight - height) / 2 - rect.Top;
                }

            }
            else if (rect.Top > 0)
            {
                deltaY = -rect.Top;
            }
            else if (rect.Bottom < viewHeight)
            {
                deltaY = viewHeight - rect.Bottom;
            }

            int viewWidth = imageView.Width;

            if (width <= viewWidth)
            {
                if (mScaleType == ImageView.ScaleType.FitStart)
                {
                    deltaX = -rect.Left;
                }
                else if (mScaleType == ImageView.ScaleType.FitEnd)
                {
                    deltaX = viewWidth - width - rect.Left;
                }
                else
                {
                    deltaX = (viewWidth - width) / 2 - rect.Left;
                }

                mScrollEdge = EDGE_BOTH;
            }
            else if (rect.Left > 0)
            {
                mScrollEdge = EDGE_LEFT;
                deltaX = -rect.Left;
            }
            else if (rect.Right < viewWidth)
            {
                deltaX = viewWidth - rect.Right;
                mScrollEdge = EDGE_RIGHT;
            }
            else
            {
                mScrollEdge = EDGE_NONE;
            }

            // Finally actually translate the matrix
            mSuppMatrix.PostTranslate(deltaX, deltaY);
        }

        private RectF GetDisplayRect(Matrix matrix)
        {
            ImageView imageView = GetImageView();

            if (null != imageView)
            {
                Drawable d = imageView.Drawable;
                if (null != d)
                {
                    mDisplayRect.Set(0, 0, d.IntrinsicWidth, d.IntrinsicHeight);
                    matrix.MapRect(mDisplayRect);
                    return mDisplayRect;
                }
            }
            return null;
        }

        #region ImplementsInterface IPhotoView

        public bool CanZoom { get => mZoomEnabled; set { mZoomEnabled = value; Update(); } }
        public RectF DisplayRect { get { CheckMatrixBounds(); return GetDisplayRect(GetDisplayMatrix()); } set => mDisplayRect = value; }
        public float MinScale { get => mMinScale; set { CheckZoomLevels(value, mMidScale, mMaxScale); mMinScale = value; } }
        public float MidScale { get => mMidScale; set { CheckZoomLevels(mMinScale, value, mMaxScale); mMidScale = value; } }
        public float MaxScale { get => mMaxScale; set { CheckZoomLevels(mMinScale, mMidScale, value); mMaxScale = value; } }
        public float Scale { get => GetScale(); set => throw new NotImplementedException(); }

        public ImageView.ScaleType ScaleType
        {
            get => mScaleType;
            set
            {
                if (IsSupportedScaleType(value) && value != mScaleType)
                {
                    mScaleType = value;

                    Update();
                }
            }
        }
        public void SetAllowParentInterceptOnEdge(bool allow)
        {
            mAllowParentInterceptOnEdge = allow;
        }

        public void SetOnLongClickListener(View.IOnLongClickListener listener)
        {
            mLongClickListener = listener;
        }

        public void SetOnMatrixChangeListener(IOnMatrixChangedListener listener)
        {
            mMatrixChangeListener = listener;
        }

        public void SetOnPhotoTapListener(IOnPhotoTapListener listener)
        {
            mPhotoTapListener = listener;
        }

        public void SetOnViewTapListener(IOnViewTapListener listener)
        {
            mViewTapListener = listener;
        }

        public void ZoomTo(float scale, float focalX, float focalY)
        {
            ImageView imageView = GetImageView();

            if (null != imageView)
            {
                imageView.Post(new AnimatedZoomRunnable(this, GetScale(), scale, focalX, focalY));
            }
        }

        #endregion

        #region Interface
        public interface IOnMatrixChangedListener
        {
            void OnMatrixChanged(RectF rect);
        }
        public interface IOnPhotoTapListener
        {
            void OnPhotoTap(View view, float x, float y);
        }
        public interface IOnViewTapListener
        {
            void OnViewTap(View view, float x, float y);
        }
        #endregion

        private class AnimatedZoomRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            private PhotoViewAttacher photoViewAttacher;
            static float ANIMATION_SCALE_PER_ITERATION_IN = 1.07f;
            static float ANIMATION_SCALE_PER_ITERATION_OUT = 0.93f;

            private float mFocalX, mFocalY;
            private float mTargetZoom;
            private float mDeltaScale;

            public AnimatedZoomRunnable(PhotoViewAttacher obj, float currentZoom, float targetZoom, float focalX, float focalY)
            {
                photoViewAttacher = obj;
                mTargetZoom = targetZoom;
                mFocalX = focalX;
                mFocalY = focalY;

                if (currentZoom < targetZoom)
                {
                    mDeltaScale = ANIMATION_SCALE_PER_ITERATION_IN;
                }
                else
                {
                    mDeltaScale = ANIMATION_SCALE_PER_ITERATION_OUT;
                }
            }


            public void Run()
            {
                ImageView imageView = photoViewAttacher.GetImageView();

                if (null != imageView)
                {
                    photoViewAttacher.mSuppMatrix.PostScale(mDeltaScale, mDeltaScale, mFocalX, mFocalY);
                    photoViewAttacher.CheckAndDisplayMatrix();

                    float currentScale = photoViewAttacher.GetScale();

                    if ((mDeltaScale > 1f && currentScale < mTargetZoom)
                            || (mDeltaScale < 1f && mTargetZoom < currentScale))
                    {
                        Compat.PostOnAnimation(imageView, this);
                    }
                    else
                    {
                        float delta = mTargetZoom / currentScale;
                        photoViewAttacher.mSuppMatrix.PostScale(delta, delta, mFocalX, mFocalY);
                        photoViewAttacher.CheckAndDisplayMatrix();
                    }
                }

            }


        }
        private class FlingRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            private PhotoViewAttacher photoViewAttacher;
            private ScrollerProxy mScroller;
            private int mCurrentX, mCurrentY;
            private Context currentContext;

            public FlingRunnable(Context context, PhotoViewAttacher obj)
            {
                mScroller = ScrollerProxy.GetScroller(context);
                photoViewAttacher = obj;
            }
            public void CancelFling()
            {
                if (DEBUG)
                {
                    Android.Util.Log.Debug(LOG_TAG, "Cancel Fling");
                }
                mScroller.ForceFinished(true);
            }

            public void Fling(int viewWidth, int viewHeight, int velocityX, int velocityY)
            {
                RectF rect = GetDisplayRect();
                if (null == rect)
                {
                    return;
                }

                int startX = (int)Math.Round(-rect.Left);
                int minX, maxX, minY, maxY;

                if (viewWidth < rect.Width())
                {
                    minX = 0;
                    maxX = (int)Math.Round(rect.Width() - viewWidth);
                }
                else
                {
                    minX = maxX = startX;
                }

                int startY = (int)Math.Round(-rect.Top);
                if (viewHeight < rect.Height())
                {
                    minY = 0;
                    maxY = (int)Math.Round(rect.Height() - viewHeight);
                }
                else
                {
                    minY = maxY = startY;
                }

                mCurrentX = startX;
                mCurrentY = startY;

                if (DEBUG)
                {
                    Android.Util.Log.Debug(LOG_TAG, "fling. StartX:" + startX + " StartY:" + startY + " MaxX:" + maxX + " MaxY:" + maxY);
                }

                if (startX != maxX || startY != maxY)
                {
                    mScroller.Fling(startX, startY, velocityX, velocityY, minX, maxX, minY, maxY, 0, 0);
                }
            }

            public RectF GetDisplayRect()
            {
                throw new NotImplementedException();
            }

            public void Run()
            {
                ImageView imageView = photoViewAttacher.GetImageView();
                if (null != imageView && mScroller.ComputeScrollOffset())
                {

                    int newX = mScroller.CurrX;
                    int newY = mScroller.CurrY;

                    if (DEBUG)
                    {
                        Android.Util.Log.Debug(LOG_TAG, "fling run(). CurrentX:" + mCurrentX + " CurrentY:" + mCurrentY + " NewX:" + newX
                                + " NewY:" + newY);
                    }

                    photoViewAttacher.mSuppMatrix.PostTranslate(mCurrentX - newX, mCurrentY - newY);
                    photoViewAttacher.SetImageViewMatrix(photoViewAttacher.GetDisplayMatrix());

                    mCurrentX = newX;
                    mCurrentY = newY;

                    Compat.PostOnAnimation(imageView, this);
                }
            }
        }
        class MySimpleOnGestureListener : GestureDetector.SimpleOnGestureListener
        {
            private View.IOnLongClickListener listener;
            public MySimpleOnGestureListener(View.IOnLongClickListener listner)
            {
                this.listener = listner;
            }

            public override void OnLongPress(MotionEvent e)
            {
                if (null != listener)
                {
                    mImageView.TryGetTarget(out ImageView v);
                    listener.OnLongClick(v);
                }
            }
        }

        bool View.IOnTouchListener.OnTouch(View v, MotionEvent e)
        {
            bool handled = false;

            if (mZoomEnabled)
            {
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        #region Down
                        // First, disable the Parent from intercepting the touch
                        // event
                        v.Parent.RequestDisallowInterceptTouchEvent(true);

                        // If we're flinging, and the user presses down, cancel
                        // fling
                        CancelFling();
                        break;
                    #endregion
                    case MotionEventActions.Cancel:
                    case MotionEventActions.Up:
                        // If the user has zoomed less than min scale, zoom back
                        // to min scale

                        if (GetScale() < mMinScale)
                        {
                            RectF rect = mCurrentFlingRunnable.GetDisplayRect();
                            if (null != rect)
                            {
                                v.Post(new AnimatedZoomRunnable(this, GetScale(), mMinScale, rect.CenterX(), rect.CenterY()));
                                handled = true;
                            }
                        }
                        break;
                }


                // Check to see if the user double tapped
                if (null != mGestureDetector && mGestureDetector.OnTouchEvent(e))
                {
                    handled = true;
                }

                // Finally, try the Scale/Drag detector
                if (null != mScaleDragDetector && mScaleDragDetector.OnTouchEvent(e))
                {
                    handled = true;
                }

            }

            return handled;
        }

        #region IOnGestureListener
        void VersionedGestureDetector.IOnGestureListener.OnDrag(float dx, float dy)
        {
            if (DEBUG)
            {
                Android.Util.Log.Debug(LOG_TAG, $"onDrag: dx:{dx}. dy:{dy}");
            }

            ImageView imageView = GetImageView();

            if (null != imageView && HasDrawable(imageView))
            {
                mSuppMatrix.PostTranslate(dx, dy);
                CheckAndDisplayMatrix();

                /**
                 * Here we decide whether to let the ImageView's parent to start
                 * taking over the touch event.
                 *
                 * First we check whether this function is enabled. We never want the
                 * parent to take over if we're scaling. We then check the edge we're
                 * on, and the direction of the scroll (i.e. if we're pulling against
                 * the edge, aka 'overscrolling', let the parent take over).
                 */
                if (mAllowParentInterceptOnEdge && !mScaleDragDetector.IsScaling())
                {
                    if (mScrollEdge == EDGE_BOTH || (mScrollEdge == EDGE_LEFT && dx >= 1f)
                            || (mScrollEdge == EDGE_RIGHT && dx <= -1f))
                    {
                        imageView.Parent.RequestDisallowInterceptTouchEvent(false);
                    }
                }
            }

        }

        void VersionedGestureDetector.IOnGestureListener.OnFling(float startX, float startY, float velocityX, float velocityY)
        {
            if (DEBUG)
            {
                Android.Util.Log.Debug(LOG_TAG, "onFling. sX: " + startX + " sY: " + startY + " Vx: " + velocityX + " Vy: " + velocityY);
            }

            ImageView imageView = GetImageView();
            if (HasDrawable(imageView))
            {
                mCurrentFlingRunnable = new FlingRunnable(imageView.Context, this);
                mCurrentFlingRunnable.Fling(imageView.Width, imageView.Height, (int)velocityX, (int)velocityY);
                imageView.Post(mCurrentFlingRunnable);
            }
        }

        void VersionedGestureDetector.IOnGestureListener.OnScale(float scaleFactor, float focusX, float focusY)
        {
            if (DEBUG)
            {
                Android.Util.Log.Debug(LOG_TAG, $"onScale: scaleFactor:{scaleFactor},focusX:{focusX},focusY:{focusY}");
            }

            if (HasDrawable(GetImageView()) && (GetScale() < mMaxScale || scaleFactor < 1f))
            {
                mSuppMatrix.PostScale(scaleFactor, scaleFactor, focusX, focusY);
                CheckAndDisplayMatrix();
            }
        }

        #endregion

        #region IOnDoubleTapListener
        bool GestureDetector.IOnDoubleTapListener.OnDoubleTap(MotionEvent e)
        {
            try
            {
                float scale = GetScale();
                float x = e.GetX();
                float y = e.GetY();

                if (scale < mMidScale)
                {
                    ZoomTo(mMidScale, x, y);
                }
                else if (scale >= mMidScale && scale < mMaxScale)
                {
                    ZoomTo(mMaxScale, x, y);
                }
                else
                {
                    ZoomTo(mMinScale, x, y);
                }
            }
            catch (Exception ex)
            {
                // Can sometimes happen when getX() and getY() is called
            }
            return true;
        }

        bool GestureDetector.IOnDoubleTapListener.OnDoubleTapEvent(MotionEvent e)
        {
            return false;
        }

        bool GestureDetector.IOnDoubleTapListener.OnSingleTapConfirmed(MotionEvent e)
        {

            Activity activity = mContext as Activity;
            if (mFromChatActivity)
            {
                activity.Finish();
            }
            else
            {
                //            RelativeLayout titleRl = (RelativeLayout) activity.findViewById(R.id.title_bar_rl);
                //            RelativeLayout checkBoxRl = (RelativeLayout) activity.findViewById(R.id.check_box_rl);
                var attrs = activity.Window.Attributes;
                //如果标题栏，菜单栏可见，单击后隐藏并设置全屏模式
                if (mTitleBarVisible)
                {
                    attrs.Flags |= WindowManagerFlags.Fullscreen;
                    activity.Window.Attributes = attrs;
                    activity.Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                    //                titleRl.setVisibility(View.GONE);
                    //                checkBoxRl.setVisibility(View.GONE);
                    mTitleBarVisible = false;
                    //否则显示标题栏、菜单栏，并取消全屏
                }
                else
                {
                    attrs.Flags &= (~WindowManagerFlags.Fullscreen);
                    activity.Window.Attributes = attrs;
                    activity.Window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
                    //                titleRl.setVisibility(View.VISIBLE);
                    //                checkBoxRl.setVisibility(View.VISIBLE);
                    mTitleBarVisible = true;
                }
            }

            return false;
        }


        #endregion
        void ViewTreeObserver.IOnGlobalLayoutListener.OnGlobalLayout()
        {
            ImageView imageView = GetImageView();

            if (null != imageView && mZoomEnabled)
            {
                int top = imageView.Top;
                int right = imageView.Right;
                int bottom = imageView.Bottom;
                int left = imageView.Left;

                /**
                 * We need to check whether the ImageView's bounds have changed.
                 * This would be easier if we targeted API 11+ as we could just use
                 * View.OnLayoutChangeListener. Instead we have to replicate the
                 * work, keeping track of the ImageView's bounds and then checking
                 * if the values change.
                 */
                if (top != mIvTop || bottom != mIvBottom || left != mIvLeft || right != mIvRight)
                {
                    // Update our base matrix, as the bounds have changed
                    UpdateBaseMatrix(imageView.Drawable);

                    // Update values as something has changed
                    mIvTop = top;
                    mIvRight = right;
                    mIvBottom = bottom;
                    mIvLeft = left;
                }
            }
        }
    }
}