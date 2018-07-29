using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace ImuiQS.Class.Views.PhotoView
{
    public class PhotoView : AppCompatImageView, IPhotoView
    {
        private PhotoViewAttacher mAttacher;

        private ScaleType mPendingScaleType;

        public PhotoView(bool fromChatActivity, Context context) : this(fromChatActivity, context, null)
        {
        }

        public PhotoView(bool fromChatActivity, Context context, IAttributeSet attr) : this(fromChatActivity, context, attr, 0)
        {
        }

        public PhotoView(bool fromChatActivity, Context context, IAttributeSet attr, int defStyle) : base(context, attr, defStyle)
        {
            base.SetScaleType(ScaleType.Matrix);
            mAttacher = new PhotoViewAttacher(this, fromChatActivity, context);

            if (null != mPendingScaleType)
            {
                SetScaleType(mPendingScaleType);
                mPendingScaleType = null;
            }
        }

        public bool CanZoom { get => mAttacher.CanZoom; set => mAttacher.CanZoom = value; }
        public RectF DisplayRect { get => mAttacher.DisplayRect; set => mAttacher.DisplayRect = value; }
        public float MinScale { get => mAttacher.MinScale; set => mAttacher.MinScale = value; }
        public float MidScale { get => mAttacher.MidScale; set => mAttacher.MidScale = value; }
        public float MaxScale { get => mAttacher.MaxScale; set => mAttacher.MaxScale = value; }
        public float Scale { get => mAttacher.Scale; set => mAttacher.Scale = value; }
        ScaleType IPhotoView.ScaleType
        {
            get => mAttacher.ScaleType; set
            {
                if (mAttacher != null)
                    mAttacher.ScaleType = value;
                else
                    mPendingScaleType = value;
            }
        }

        public void SetAllowParentInterceptOnEdge(bool allow)
        {
            mAttacher.SetAllowParentInterceptOnEdge(allow);
        }

        public void SetOnMatrixChangeListener(PhotoViewAttacher.IOnMatrixChangedListener listener)
        {
            mAttacher.SetOnMatrixChangeListener(listener);
        }

        public void SetOnPhotoTapListener(PhotoViewAttacher.IOnPhotoTapListener listener)
        {
            mAttacher.SetOnPhotoTapListener(listener);
        }

        public void SetOnViewTapListener(PhotoViewAttacher.IOnViewTapListener listener)
        {
            mAttacher.SetOnViewTapListener(listener);
        }

        public void ZoomTo(float scale, float focalX, float focalY)
        {
            mAttacher.ZoomTo(scale, focalX, focalY);
        }

        public override void SetImageDrawable(Drawable drawable)
        {
            base.SetImageDrawable(drawable);
            if (null != mAttacher)
            {
                mAttacher.Update();
            }
        }
        public override void SetImageResource(int resId)
        {
            base.SetImageResource(resId);
            if (null != mAttacher)
            {
                mAttacher.Update();
            }
        }
        public override void SetImageURI(Android.Net.Uri uri)
        {
            base.SetImageURI(uri);
            if (null != mAttacher)
            {
                mAttacher.Update();
            }
        }
        public override void SetOnLongClickListener(IOnLongClickListener l)
        {
            mAttacher.SetOnLongClickListener(l);
        }
        protected override void OnDetachedFromWindow()
        {
            mAttacher.Cleanup();
            base.OnDetachedFromWindow();
        }

    }
}