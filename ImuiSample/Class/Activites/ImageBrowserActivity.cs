using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using CN.Jiguang.Imui.Commons;
using ImuiQS.Class.Views;
using ImuiQS.Class.Views.PhotoView;

namespace ImuiSample.Class.Activites
{
    [Activity(Label = "ImageBrowserActivity")]
    public class ImageBrowserActivity : Activity
    {
        private static ImgBrowserViewPager mViewPager;
        private static List<string> mPathList = new List<string>();
        private static List<string> mMsgIdList = new List<string>();
        private static Android.Util.LruCache mCache; // LruCache<String, Bitmap> mCache;
        private static int mWidth;
        private static int mHeight;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_image_browser);

            mPathList = Intent.GetStringArrayListExtra("pathList").ToList();
            mMsgIdList = Intent.GetStringArrayListExtra("idList").ToList();
            mViewPager = FindViewById<ImgBrowserViewPager>(Resource.Id.img_browser_viewpager);
            DisplayMetrics dm = Resources.DisplayMetrics;
            mWidth = dm.WidthPixels;
            mHeight = dm.HeightPixels;

            int maxMemory = (int)(Java.Lang.Runtime.GetRuntime().MaxMemory());
            int cacheSize = maxMemory / 4;
            mCache = new LruCache(cacheSize);
            mViewPager.Adapter = new CustomPagerAdapter(this);

            InitCurrentItem();
        }

        private void InitCurrentItem()
        {
            PhotoView photoView = new PhotoView(true, this);
            string msgId = Intent.GetStringExtra("msgId");
            int position = mMsgIdList.IndexOf(msgId);
            string path = mPathList[position];
            if (path != null)
            {
                if (mCache.Get(path) is Bitmap bitmap)
                {
                    photoView.SetImageBitmap(bitmap);
                }
                else
                {
                    if (System.IO.File.Exists(path))
                    {
                        bitmap = BitmapLoader.GetBitmapFromFile(path, mWidth, mHeight);
                        if (bitmap != null)
                        {
                            photoView.SetImageBitmap(bitmap);
                            mCache.Put(path, bitmap);
                        }
                        else
                        {
                            photoView.SetImageResource(Resource.Drawable.aurora_picture_not_found);
                        }
                    }
                    else
                    {
                        photoView.SetImageResource(Resource.Drawable.aurora_picture_not_found);
                    }
                }
            }
            else
            {
                photoView.SetImageResource(Resource.Drawable.aurora_picture_not_found);
            }
            mViewPager.CurrentItem = position;
        }

        class CustomPagerAdapter : PagerAdapter
        {
            private Context curentContext;

            public CustomPagerAdapter(Context ctx)
            {
                curentContext = ctx;
            }

            public override int Count => mPathList.Count;

            public override bool IsViewFromObject(View view, Java.Lang.Object @object)
            {
                return view == @object;
            }
            public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
            {
                PhotoView photoView = new PhotoView(true, curentContext);
                photoView.SetScaleType(ImageView.ScaleType.CenterCrop);
                photoView.Tag = position;
                string path = mPathList[position];
                if (path != null)
                {
                    if (mCache.Get(path) is Bitmap bitmap)
                    {
                        photoView.SetImageBitmap(bitmap);
                    }
                    else
                    {
                        if (System.IO.File.Exists(path))
                        {
                            bitmap = BitmapLoader.GetBitmapFromFile(path, mWidth, mHeight);
                            if (bitmap != null)
                            {
                                photoView.SetImageBitmap(bitmap);
                                mCache.Put(path, bitmap);
                            }
                            else
                            {
                                photoView.SetImageResource(Resource.Drawable.aurora_picture_not_found);
                            }
                        }
                        else
                        {
                            photoView.SetImageResource(Resource.Drawable.aurora_picture_not_found);
                        }
                    }
                }
                else
                {
                    photoView.SetImageResource(Resource.Drawable.aurora_picture_not_found);
                }
                container.AddView(photoView, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                return photoView;
            }

            public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object @object)
            {
                container.RemoveView((View)@object);
            }
            public override int GetItemPosition(Java.Lang.Object @object)
            {
                View view = @object as View;
                int currentPage = mViewPager.CurrentItem;
                if (currentPage == (int)view.Tag)
                {
                    return PositionNone;
                }
                else
                {
                    return PositionUnchanged;
                }
            }
        }
    }
}