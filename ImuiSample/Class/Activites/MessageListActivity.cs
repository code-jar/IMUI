using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using CN.Jiguang.Imui.Chatinput.Listener;
using CN.Jiguang.Imui.Chatinput.Model;
using CN.Jiguang.Imui.Commons.Models;
using CN.Jiguang.Imui.Messages;
using CN.Jiguang.Imui.Messages.Ptr;
using Com.Bumptech.Glide;
using Com.Bumptech.Glide.Request;
using Com.Bumptech.Glide.Request.Transition;
using ImuiQS.Class.Models;
using ImuiQS.Class.Views;
using ImuiSample.Class.Views;
using Java.IO;
using Java.Lang;
using Pub.Devrel.Easypermissions;

namespace ImuiSample.Class.Activites
{
    [Activity(Label = "MessageListActivity", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MessageListActivity : Activity
    {
        private static string TAG = "MessageListActivity";
        private static ChatView mChatView;
        private static MsgListAdapter mAdapter;
        private List<MyMessage> mData;

        private HeadsetDetectReceiver currentHeadsetDetectReceiver;
        private SensorManager currentSensorManager;
        private Sensor currentSensor;
        private static PowerManager currentPowerManager;
        private static PowerManager.WakeLock currentWakeLock;
        private ISensorEventListener currentSensorEventListener;

        private static List<string> mPathList = new List<string>();
        private static List<string> mMsgIdList = new List<string>();


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_chat);

            var inputManager = GetSystemService(Context.InputMethodService) as InputMethodManager;
            RegisterProximitySensorListener();
            mChatView = FindViewById<ChatView>(Resource.Id.chat_view);
            mChatView.InitModule();
            mChatView.SetTitle("Deadpool");
            mData = GetMessages();
            InitMsgAdapter();

            currentHeadsetDetectReceiver = new HeadsetDetectReceiver();
            IntentFilter intentFilter = new IntentFilter();
            intentFilter.AddAction(Intent.ActionHeadsetPlug);
            RegisterReceiver(currentHeadsetDetectReceiver, intentFilter);
            mChatView.SetOnTouchListener(new ChatViewOnTouchListener(this, inputManager, this.Window));

            mChatView.SetMenuClickListener(new MenuClickListener(this));

            mChatView.SetRecordVoiceListener(new RecordVoiceListener(this));

            mChatView.SetOnCameraCallbackListener(new CameraCallbackListener(this));

            mChatView.GetChatInputView().InputView.SetOnTouchListener(new InputViewOnTouchListener());

            mChatView.GetSelectAlbumBtn().SetOnClickListener(new AlbumBtnOnClickListener(this));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterReceiver(currentHeadsetDetectReceiver);
            currentSensorManager.UnregisterListener(currentSensorEventListener);
        }


        #region EventListener
        class ChatViewOnTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private Activity currentActivity;
            private InputMethodManager currentInputManager;
            private Window currentWindow;
            public ChatViewOnTouchListener(Activity act, InputMethodManager inputManager, Window wd)
            {
                currentActivity = act;
                currentInputManager = inputManager;
                currentWindow = wd;
            }
            public bool OnTouch(View view, MotionEvent e)
            {
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        {
                            var chatInputView = mChatView.GetChatInputView();
                            if (chatInputView.MenuState == (int)ViewStates.Visible)
                            {
                                chatInputView.DismissMenuLayout();
                            }
                            try
                            {
                                View v = currentActivity.CurrentFocus;
                                if (currentInputManager != null && v != null)
                                {
                                    currentInputManager.HideSoftInputFromWindow(v.WindowToken, 0);
                                    currentWindow.SetSoftInputMode(SoftInput.AdjustResize);
                                    view.ClearFocus();
                                }
                            }
                            catch (System.Exception ex)
                            {
                                throw ex;
                            }
                        }
                        break;
                    case MotionEventActions.Up:
                        view.PerformClick();
                        break;
                    default:
                        break;
                }
                return false;
            }
        }
        class MenuClickListener : Java.Lang.Object, IOnMenuClickListener
        {
            private readonly int RC_RECORD_VOICE = 0x0001;
            private readonly int RC_CAMERA = 0x0002;
            private readonly int RC_PHOTO = 0x0003;

            private Context currentContext;
            private Activity currentActivity;

            public MenuClickListener(Activity act)
            {
                currentActivity = act;
                currentContext = act.ApplicationContext;
            }

            public void OnSendFiles(IList<FileItem> list)
            {
                if (list?.Count == 0)
                {
                    return;
                }

                MyMessage message;
                list.ToList().ForEach(item =>
                {
                    if (item.GetType() == FileItem.Type.Image)
                    {
                        message = new MyMessage(null, MessageMessageType.SendImage.Ordinal());
                        mPathList.Add(item.FilePath);
                        mMsgIdList.Add(message.MsgId);
                    }
                    else if (item.GetType() == FileItem.Type.Video)
                    {
                        message = new MyMessage(MessageMessageType.SendVideo.Ordinal())
                        {
                            Duration = ((VideoItem)item).Duration
                        };
                    }
                    else
                    {
                        throw new RuntimeException("Invalid FileItem type. Must be Type.Image or Type.Video");
                    }

                    message.TimeString = DateTime.Now.ToString("HH:mm");
                    message.MediaFilePath = item.FilePath;
                    message.FromUser = new DefaultUser("1", "Ironman", "R.drawable.ironman");

                    MyMessage fMsg = message;

                    currentActivity.RunOnUiThread(new Runnable(() =>
                    {
                        mAdapter.AddToStart(fMsg, true);
                    }));
                });
            }

            public bool OnSendTextMessage(ICharSequence input)
            {
                if (input.Length() == 0)
                {
                    return false;
                }

                MyMessage message = new MyMessage(input.ToString(), MessageMessageType.SendText.Ordinal())
                {
                    FromUser = new DefaultUser("1", "Ironman", "R.drawable.ironman")
                };

                message.TimeString = DateTime.Now.ToString("HH:mm");
                message.MessageStatus = CN.Jiguang.Imui.Commons.Models.MessageMessageStatus.SendGoing;
                mAdapter.AddToStart(message, true);
                return true;
            }

            public bool SwitchToCameraMode()
            {
                ScrollToBottom();
                string[] perms = { Android.Manifest.Permission.WriteExternalStorage,
                    Android.Manifest.Permission.Camera,
                    Android.Manifest.Permission.RecordAudio };

                if (!EasyPermissions.HasPermissions(currentContext, perms))
                {
                    EasyPermissions.RequestPermissions(currentActivity, currentContext.Resources.GetString(Resource.String.rationale_camera), RC_CAMERA, perms);

                    return false;
                }
                else
                {
                    string fileDir = currentContext.FilesDir.AbsolutePath + "/photo";

                    mChatView.SetCameraCaptureFile(fileDir, DateTime.Now.ToString("yyyyMMddHHmmss"));
                }

                return true;
            }

            public bool SwitchToEmojiMode()
            {
                ScrollToBottom();
                return true;
            }

            public bool SwitchToGalleryMode()
            {
                ScrollToBottom();
                string[] perms = { Android.Manifest.Permission.ReadExternalStorage };

                if (!EasyPermissions.HasPermissions(currentContext, perms))
                {
                    EasyPermissions.RequestPermissions(currentActivity, currentContext.Resources.GetString(Resource.String.rationale_photo), RC_PHOTO, perms);
                }
                mChatView.GetChatInputView().SelectPhotoView.UpdateData();
                return true;
            }

            public bool SwitchToMicrophoneMode()
            {
                ScrollToBottom();
                string[] perms = { Android.Manifest.Permission.RecordAudio, Android.Manifest.Permission.WriteExternalStorage };

                if (!EasyPermissions.HasPermissions(currentContext, perms))
                {
                    EasyPermissions.RequestPermissions(currentActivity, currentContext.Resources.GetString(Resource.String.rationale_record_voice), RC_RECORD_VOICE, perms);
                }
                return true;
            }
        }
        class EditTextListener : Java.Lang.Object, IOnClickEditTextListener
        {
            public void OnTouchEditText()
            {
                mAdapter.LayoutManager.ScrollToPosition(0);
            }
        }
        class RecordVoiceListener : Java.Lang.Object, IRecordVoiceListener
        {
            private Context currentContext;
            public RecordVoiceListener(Context ctx)
            {
                currentContext = ctx;
            }
            public void OnStartRecord()
            {
                string path = Android.OS.Environment.ExternalStorageDirectory.Path + "/voice";

                var file = new File(path);
                if (!file.Exists())
                {
                    file.Mkdir();
                }
                mChatView.SetRecordVoiceFile(file.Path, DateTime.Now.ToString("yyyy-MM-dd-hhmmss"));
            }
            public void OnFinishRecord(File p0, int p1)
            {
                MyMessage message = new MyMessage(null, MessageMessageType.SendVoice.Ordinal())
                {
                    FromUser = new DefaultUser("1", "Ironman", "R.drawable.ironman"),
                    MediaFilePath = p0.Path,
                    Duration = p1,
                    TimeString = DateTime.Now.ToString("HH:mm")
                };
                mAdapter.AddToStart(message, true);
            }
            public void OnCancelRecord()
            {
            }

            public void OnPreviewCancel()
            {
            }

            public void OnPreviewSend()
            {
            }
        }
        class CameraCallbackListener : Java.Lang.Object, IOnCameraCallbackListener
        {
            private readonly Activity currentActivity;
            public CameraCallbackListener(Activity act)
            {
                currentActivity = act;
            }
            public void OnCancelVideoRecord()
            {
            }

            public void OnFinishVideoRecord(string p0)
            {
                // Fires when finished recording video.
                // Pay attention here, when you finished recording video and click send
                // button in screen, will fire onSendFiles() method.
            }

            public void OnStartVideoRecord()
            {
            }

            public void OnTakePictureCompleted(string path)
            {
                MyMessage message = new MyMessage(null, MessageMessageType.SendImage.Ordinal())
                {
                    TimeString = DateTime.Now.ToString("HH:mm"),
                    MediaFilePath = path
                };
                mPathList.Add(path);
                mMsgIdList.Add(message.MsgId);
                message.FromUser = new DefaultUser("1", "Ironman", "R.drawable.ironman");

                currentActivity.RunOnUiThread(() =>
                {
                    new Runnable(() =>
                    {
                        mAdapter.AddToStart(message, true);
                    });
                });
            }
        }
        class InputViewOnTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            public bool OnTouch(View v, MotionEvent e)
            {
                ScrollToBottom();
                return false;
            }
        }
        class AlbumBtnOnClickListener : Java.Lang.Object, View.IOnClickListener
        {
            private readonly Context currentContext;
            public AlbumBtnOnClickListener(Context ctx)
            {
                currentContext = ctx;
            }
            public void OnClick(View v)
            {
                Toast.MakeText(currentContext, "OnClick select album button", ToastLength.Long).Show();
            }
        }
        class HeadsetDetectReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent.Action.Equals(Android.Content.Intent.ActionHeadsetPlug))
                {
                    if (intent.HasExtra("state"))
                    {
                        int state = intent.GetIntExtra("state", 0);
                        mAdapter.SetAudioPlayByEarPhone(state);
                    }
                }
            }
        }
        class PermissionCallbacks : Java.Lang.Object, EasyPermissions.IPermissionCallbacks
        {
            private readonly Activity currentActivity;
            public PermissionCallbacks(Activity act) => currentActivity = act;

            public void OnPermissionsDenied(int p0, IList<string> p1)
            {
                if (EasyPermissions.SomePermissionPermanentlyDenied(currentActivity, p1))
                {
                    new AppSettingsDialog.Builder(currentActivity).Build().Show();
                }
            }

            public void OnPermissionsGranted(int p0, IList<string> p1)
            {
            }

            public void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
            {
                EasyPermissions.OnRequestPermissionsResult(requestCode, permissions, grantResults.Select(item => (int)item).ToArray());
            }
        }
        class SensorEventListener : Java.Lang.Object, ISensorEventListener
        {
            private Context currentContext;
            private Sensor currentSensor;
            public SensorEventListener(Context ctx, Sensor sensor)
            {
                currentContext = ctx;
                currentSensor = sensor;
            }

            public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
            {
            }

            public void OnSensorChanged(SensorEvent e)
            {
                Android.Media.AudioManager audioManager = currentContext.GetSystemService(AudioService) as Android.Media.AudioManager;

                try
                {

                    if (audioManager.BluetoothA2dpOn || audioManager.WiredHeadsetOn)
                    {
                        return;
                    }

                    if (mAdapter.MediaPlayer.IsPlaying)
                    {
                        float distance = e.Values[0];
                        if (distance >= currentSensor.MaximumRange)
                        {
                            mAdapter.SetAudioPlayByEarPhone(0);
                            SetScreenOn();
                        }
                        else
                        {
                            mAdapter.SetAudioPlayByEarPhone(2);
                            ViewHolderController.Instance.ReplayVoice();
                            SetScreenOff();
                        }
                    }
                    else
                    {
                        if (currentWakeLock != null && currentWakeLock.IsHeld)
                        {
                            currentWakeLock.Release();
                            currentWakeLock = null;
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    Toast.MakeText(currentContext, "Error:" + ex.Message, ToastLength.Short).Show();
                }
            }
        }
        class MyImageLoader : Java.Lang.Object, CN.Jiguang.Imui.Commons.IImageLoader
        {
            private Context currentContext;

            public MyImageLoader(Context ctx)
            {
                this.currentContext = ctx;
            }

            public void LoadAvatarImage(ImageView avatarImageView, string p1)
            {
                if (p1.Contains("R.drawable"))
                {
                    int resId = currentContext.Resources.GetIdentifier(p1.Replace("R.drawable.", ""), "drawable", currentContext.PackageName);

                    avatarImageView.SetImageResource(resId);
                }
                else
                {
                    Glide.With(currentContext).Load(p1).Apply(new RequestOptions().Placeholder(Resource.Drawable.aurora_headicon_default)).Into(avatarImageView);
                }
            }

            public void LoadImage(ImageView imageView, string p1)
            {
                Glide.With(currentContext)
                       .AsBitmap()
                       .Load(p1)
                       .Apply(new RequestOptions().FitCenter().Placeholder(Resource.Drawable.aurora_picture_not_found))
                       .Into(new MySimpleTarget(currentContext, imageView));
            }

            public void LoadVideo(ImageView imageCover, string uri)
            {
                long interval = 5000 * 1000;
                Glide.With(currentContext).AsBitmap().Load(uri).Apply(new RequestOptions().Frame(interval).Override(200, 400)).Into(imageCover);
            }
        }
        class MySimpleTarget : Com.Bumptech.Glide.Request.Target.SimpleTarget
        {
            private Context currentContext;
            private ImageView imageView;

            public MySimpleTarget(Context ctx, ImageView view)
            {
                this.currentContext = ctx;
                this.imageView = view;
            }
            public override void OnResourceReady(Java.Lang.Object p0, ITransition p1)
            {
                float density = currentContext.Resources.DisplayMetrics.Density;
                float MIN_WIDTH = 60 * density;
                float MAX_WIDTH = 200 * density;
                float MIN_HEIGHT = 60 * density;
                float MAX_HEIGHT = 200 * density;

                var resource = p0 as Android.Graphics.Bitmap;
                int imageWidth = resource.Width;
                int imageHeight = resource.Height;

                // 裁剪 bitmap
                float width, height;
                if (imageWidth > imageHeight)
                {
                    if (imageWidth > MAX_WIDTH)
                    {
                        float temp = MAX_WIDTH / imageWidth * imageHeight;
                        height = temp > MIN_HEIGHT ? temp : MIN_HEIGHT;
                        width = MAX_WIDTH;
                    }
                    else if (imageWidth < MIN_WIDTH)
                    {
                        float temp = MIN_WIDTH / imageWidth * imageHeight;
                        height = temp < MAX_HEIGHT ? temp : MAX_HEIGHT;
                        width = MIN_WIDTH;
                    }
                    else
                    {
                        float ratio = imageWidth / imageHeight;
                        if (ratio > 3)
                        {
                            ratio = 3;
                        }
                        height = imageHeight * ratio;
                        width = imageWidth;
                    }
                }
                else
                {
                    if (imageHeight > MAX_HEIGHT)
                    {
                        float temp = MAX_HEIGHT / imageHeight * imageWidth;
                        width = temp > MIN_WIDTH ? temp : MIN_WIDTH;
                        height = MAX_HEIGHT;
                    }
                    else if (imageHeight < MIN_HEIGHT)
                    {
                        float temp = MIN_HEIGHT / imageHeight * imageWidth;
                        width = temp < MAX_WIDTH ? temp : MAX_WIDTH;
                        height = MIN_HEIGHT;
                    }
                    else
                    {
                        float ratio = imageHeight / imageWidth;
                        if (ratio > 3)
                        {
                            ratio = 3;
                        }
                        width = imageWidth * ratio;
                        height = imageHeight;
                    }
                }

                var layoutParms = imageView.LayoutParameters;
                layoutParms.Width = (int)width;
                layoutParms.Height = (int)height;
                imageView.LayoutParameters = layoutParms;
                var matrix = new Android.Graphics.Matrix();
                float scaleWidth = width / imageWidth;
                float scaleHeight = height / imageHeight;
                matrix.PostScale(scaleWidth, scaleHeight);
                imageView.SetImageBitmap(Android.Graphics.Bitmap.CreateBitmap(resource, 0, 0, imageWidth, imageHeight, matrix, true));
            }
        }
        class MsgClickListener : Java.Lang.Object, MsgListAdapter.IOnMsgClickListener
        {
            private Context currentContext;
            public MsgClickListener(Context ctx)
            {
                this.currentContext = ctx;
            }
            public void OnMessageClick(Java.Lang.Object p0)
            {
                var message = p0 as MyMessage;

                if (message.Type == MessageMessageType.ReceiveVideo.Ordinal() || message.Type == MessageMessageType.SendVideo.Ordinal())
                {
                    if (!string.IsNullOrEmpty(message.MediaFilePath))
                    {
                        Intent intent = new Intent(currentContext, typeof(VideoActivity));
                        intent.PutExtra(VideoActivity.VIDEO_PATH, message.MediaFilePath);
                        currentContext.StartActivity(intent);
                    }
                }
                else if (message.Type == MessageMessageType.ReceiveImage.Ordinal() || message.Type == MessageMessageType.SendImage.Ordinal())
                {
                    Intent intent = new Intent(currentContext, typeof(ImageBrowserActivity));
                    intent.PutExtra("msgId", message.MsgId);
                    intent.PutStringArrayListExtra("pathList", mPathList);
                    intent.PutStringArrayListExtra("idList", mMsgIdList);
                    currentContext.StartActivity(intent);
                }
                else
                {
                    Toast.MakeText(currentContext, currentContext.GetString(Resource.String.message_click_hint), ToastLength.Short).Show();
                }
            }
        }
        class MsgLongClickListener : Java.Lang.Object, MsgListAdapter.IOnMsgLongClickListener
        {
            private Context currentContext;
            public MsgLongClickListener(Context ctx)
            {
                this.currentContext = ctx;
            }
            public void OnMessageLongClick(View p0, Java.Lang.Object p1)
            {
                Toast.MakeText(currentContext, currentContext.GetString(Resource.String.message_long_click_hint), ToastLength.Short).Show();
            }
        }
        class AvatarClickListener : Java.Lang.Object, MsgListAdapter.IOnAvatarClickListener
        {
            private Context currentContext;
            public AvatarClickListener(Context ctx)
            {
                this.currentContext = ctx;
            }
            public void OnAvatarClick(Java.Lang.Object p0)
            {
                var msg = p0 as MyMessage;
                var userInfo = (DefaultUser)msg.FromUser;

                Toast.MakeText(currentContext, currentContext.GetString(Resource.String.avatar_click_hint), ToastLength.Short).Show();
            }
        }
        class MsgStatusViewClickListener : Java.Lang.Object, MsgListAdapter.IOnMsgStatusViewClickListener
        {
            private Context currentContext;
            public MsgStatusViewClickListener(Context ctx)
            {
                this.currentContext = ctx;
            }
            public void OnStatusViewClick(Java.Lang.Object p0)
            {
            }
        }
        class PtrHandler : Java.Lang.Object, IPtrHandler
        {
            private Context currentContext;
            public PtrHandler(Context ctx)
            {
                this.currentContext = ctx;
            }
            public void OnRefreshBegin(PullToRefreshLayout p0)
            {
                LoadNextPage();
            }

            private void LoadNextPage()
            {
                new Handler().PostDelayed(new Runnable(() =>
                {
                    List<MyMessage> list = new List<MyMessage>();

                    var messages = currentContext.Resources.GetStringArray(Resource.Array.conversation);

                    for (int i = 0; i < messages.Length; i++)
                    {
                        MyMessage message;
                        if (i % 2 == 0)
                        {
                            message = new MyMessage(messages[i], MessageMessageType.ReceiveText.Ordinal())
                            {
                                FromUser = new DefaultUser("0", "DeadPool", "R.drawable.deadpool")
                            };
                        }
                        else
                        {
                            message = new MyMessage(messages[i], MessageMessageType.SendText.Ordinal())
                            {
                                FromUser = new DefaultUser("1", "IronMan", "R.drawable.ironman")
                            };
                        }
                        message.TimeString = DateTime.Now.ToString("HH:mm");
                        list.Add(message);
                    }

                    mAdapter.AddToEndChronologically(list);
                    mChatView.GetPtrLayout().RefreshComplete();

                }), 1500);

            }
        }
        class OnLoadMoreListener : Java.Lang.Object, MsgListAdapter.IOnLoadMoreListener
        {
            public void OnLoadMore(int p0, int p1)
            {

            }
        }

        #endregion

        private static void ScrollToBottom()
        {
            new Handler().PostDelayed(new Runnable(() =>
            {
                mChatView.GetMessageListView().SmoothScrollToPosition(0);
            }), 200);
        }

        private static void SetScreenOn()
        {
            if (currentWakeLock != null)
            {
                currentWakeLock.SetReferenceCounted(false);
                currentWakeLock.Release();
                currentWakeLock = null;
            }
        }
        private static void SetScreenOff()
        {
            if (currentWakeLock == null)
            {
                currentWakeLock = currentPowerManager.NewWakeLock(WakeLockFlags.ProximityScreenOff, TAG);
            }
            currentWakeLock.Acquire();
        }

        private List<MyMessage> GetMessages()
        {
            List<MyMessage> list = new List<MyMessage>();
            string[] messages = Resources.GetStringArray(Resource.Array.messages_array);

            for (int i = 0; i < messages.Length; i++)
            {
                MyMessage message;
                if (i % 2 == 0)
                {
                    message = new MyMessage(messages[i], MessageMessageType.ReceiveText.Ordinal())
                    {
                        FromUser = new DefaultUser("0", "DeadPool", "R.drawable.deadpool")
                    };
                }
                else
                {
                    message = new MyMessage(messages[i], MessageMessageType.SendText.Ordinal())
                    {
                        FromUser = new DefaultUser("1", "IronMan", "R.drawable.ironman")
                    };
                }
                message.TimeString = DateTime.Now.ToString("HH:mm");
                list.Add(message);
            }

            return list;
        }

        private void InitMsgAdapter()
        {
            var imageLoader = new MyImageLoader(this);
            var holdersConfig = new MsgListAdapter.HoldersConfig();
            mAdapter = new MsgListAdapter("0", holdersConfig, imageLoader);

            mAdapter.SetOnMsgClickListener(new MsgClickListener(this));
            mAdapter.SetMsgLongClickListener(new MsgLongClickListener(this));
            mAdapter.SetOnAvatarClickListener(new AvatarClickListener(this));
            mAdapter.SetMsgStatusViewClickListener(new MsgStatusViewClickListener(this));


            MyMessage message = new MyMessage("Hello World", MessageMessageType.ReceiveText.Ordinal())
            {
                FromUser = new DefaultUser("0", "Deadpool", "R.drawable.deadpool")
            };
            mAdapter.AddToStart(message, true);

            //MyMessage voiceMessage = new MyMessage("", MessageMessageType.ReceiveVoice.Ordinal())
            //{
            //    FromUser = new DefaultUser("0", "Deadpool", "R.drawable.deadpool"),
            //    MediaFilePath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/voice/2018-02-28-105103.m4a",
            //    Duration = 4
            //};
            //mAdapter.AddToStart(voiceMessage, true);

            //MyMessage sendVoiceMsg = new MyMessage("", MessageMessageType.SendVoice.Ordinal())
            //{
            //    FromUser = new DefaultUser("1", "Ironman", "R.drawable.ironman"),
            //    MediaFilePath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/voice/2018-02-28-105103.m4a",
            //    Duration = 4
            //};
            //mAdapter.AddToStart(sendVoiceMsg, true);
            //MyMessage eventMsg = new MyMessage("haha", MessageMessageType.Event.Ordinal());
            //mAdapter.AddToStart(eventMsg, true);

            //MyMessage receiveVideo = new MyMessage("", MessageMessageType.ReceiveVideo.Ordinal());
            //receiveVideo.MediaFilePath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Pictures/Hangouts/video-20170407_135638.3gp";
            //receiveVideo.Duration = 4;
            //receiveVideo.FromUser = new DefaultUser("0", "Deadpool", "R.drawable.deadpool");
            //mAdapter.AddToStart(receiveVideo, true);
            //mAdapter.AddToEndChronologically(mData);
            var layout = mChatView.GetPtrLayout();
            layout.SetPtrHandler(new PtrHandler(this));
            mAdapter.SetOnLoadMoreListener(new OnLoadMoreListener());

            mChatView.SetAdapter(mAdapter);
            mAdapter.LayoutManager.ScrollToPosition(0);
        }

        private void RegisterProximitySensorListener()
        {
            try
            {
                currentPowerManager = GetSystemService(PowerService) as PowerManager;
                currentWakeLock = currentPowerManager.NewWakeLock(WakeLockFlags.ProximityScreenOff, TAG);
                currentSensorManager = GetSystemService(SensorService) as SensorManager;
                currentSensor = currentSensorManager.GetDefaultSensor(SensorType.Proximity);
                currentSensorEventListener = new SensorEventListener(this, currentSensor);
                currentSensorManager.RegisterListener(currentSensorEventListener, currentSensor, SensorDelay.Normal);
            }
            catch (System.Exception e)
            {
                ShowMsg("Error:" + e.Message);
            }
        }

        private void ShowMsg(string msg)
        {
            Toast.MakeText(this, msg, ToastLength.Short).Show();
        }
    }
}