using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace imuisample.Class.Views.PhotoView
{
    public interface IPhotoView
    {
        bool CanZoom { get; set; }
        RectF DisplayRect { get; set; }
        float MinScale { get; set; }
        float MidScale { get; set; }
        float MaxScale { get; set; }
        float Scale { get; set; }
        ImageView.ScaleType ScaleType { get; set; }
        void SetAllowParentInterceptOnEdge(bool allow);
        void SetOnLongClickListener(View.IOnLongClickListener listener);
        void SetOnMatrixChangeListener(PhotoViewAttacher.IOnMatrixChangedListener listener);
        void SetOnPhotoTapListener(PhotoViewAttacher.IOnPhotoTapListener listener);
        void SetOnViewTapListener(PhotoViewAttacher.IOnViewTapListener listener);
        void ZoomTo(float scale, float focalX, float focalY);

    }
}