using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace imuisample.Class.Messages
{
    [Activity(Label = "VideoActivity")]
    public class VideoActivity : Activity
    {
        public static readonly String VIDEO_PATH = "VIDEO_PATH";
        private static VideoView VideoView;
        private static int CurrentPosition;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_video);

            String videoPath = Intent.GetStringExtra(VIDEO_PATH);

            VideoView = FindViewById<VideoView>(Resource.Id.videoview_video);

            MediaController mediaController = new MediaController(this);
            mediaController.SetAnchorView(VideoView);

            VideoView.SetMediaController(mediaController);
            VideoView.SetVideoPath(videoPath);
            VideoView.SetOnPreparedListener(new MediaPlayerListener());
            VideoView.SetOnCompletionListener(new MediaPlayerListener());

        }
        class MediaPlayerListener : Java.Lang.Object, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnCompletionListener
        {
            public void OnCompletion(MediaPlayer mp)
            {
                VideoView.KeepScreenOn = false;
            }

            public void OnPrepared(MediaPlayer mp)
            {
                VideoView.RequestLayout();
                if (CurrentPosition != 0)
                {
                    VideoView.SeekTo(CurrentPosition);
                    CurrentPosition = 0;
                }
                else
                {
                    Play();
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            VideoView.Resume();
        }
        protected override void OnPause()
        {
            base.OnPause();
            CurrentPosition = VideoView.CurrentPosition;
            VideoView.Pause();
        }
        protected override void OnStop()
        {
            base.OnStop();
            Pause();
        }


        private static void Play()
        {
            VideoView.Start();
            VideoView.KeepScreenOn = true;
        }

        private static void Pause()
        {
            VideoView.Pause();
            VideoView.KeepScreenOn = false;
        }
    }
}