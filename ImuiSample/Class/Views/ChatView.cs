using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using CN.Jiguang.Imui.Chatinput;
using CN.Jiguang.Imui.Chatinput.Listener;
using CN.Jiguang.Imui.Chatinput.Menu.View;
using CN.Jiguang.Imui.Chatinput.Record;
using CN.Jiguang.Imui.Messages;
using CN.Jiguang.Imui.Messages.Ptr;
using CN.Jiguang.Imui.Utils;

namespace ImuiSample.Class.Views
{
    public class ChatView : RelativeLayout
    {
        private TextView mTitle;
        private LinearLayout mTitleContainer;
        private MessageList mMsgList;
        private ChatInputView mChatInput;
        private RecordVoiceButton mRecordVoiceBtn;
        private PullToRefreshLayout mPtrLayout;
        private ImageButton mSelectAlbumIb;

        public ChatView(Context context) : base(context)
        {
        }

        public ChatView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ChatView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public void InitModule()
        {
            mTitleContainer = FindViewById<LinearLayout>(Resource.Id.title_container);
            mTitle = FindViewById<TextView>(Resource.Id.title_tv);
            mMsgList = FindViewById<MessageList>(Resource.Id.msg_list);
            mChatInput = FindViewById<ChatInputView>(Resource.Id.chat_input);
            mPtrLayout = FindViewById<PullToRefreshLayout>(Resource.Id.pull_to_refresh_layout);

            mChatInput.SetMenuContainerHeight(819);
            mRecordVoiceBtn = mChatInput.RecordVoiceButton;
            mSelectAlbumIb = mChatInput.SelectAlbumBtn;
            PtrDefaultHeader header = new PtrDefaultHeader(Context);
            int[] colors = Resources.GetIntArray(Resource.Array.google_colors);
            header.SetColorSchemeColors(colors);
            header.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);
            header.SetPadding(0, DisplayUtil.Dp2px(Context, 15), 0, DisplayUtil.Dp2px(Context, 10));
            header.SetPtrFrameLayout(mPtrLayout);

            //        mMsgList.setDateBgColor(Color.parseColor("#FF4081"));
            //        mMsgList.setDatePadding(5, 10, 10, 5);
            //        mMsgList.setEventTextPadding(5);
            //        mMsgList.setEventBgColor(Color.parseColor("#34A350"));
            //        mMsgList.setDateBgCornerRadius(15);

            mMsgList.HasFixedSize = true;
            mPtrLayout.SetLoadingMinTime(1000);
            mPtrLayout.SetDurationToCloseHeader(1500);
            mPtrLayout.HeaderView = header;
            mPtrLayout.AddPtrUIHandler(header);

            mPtrLayout.PinContent = true;
            // set show display name or not
            //        mMsgList.setShowReceiverDisplayName(true);
            //        mMsgList.setShowSenderDisplayName(false);

            CN.Jiguang.Imui.Chatinput.Menu.MenuManager menuManager = mChatInput.MenuManager;
            menuManager.AddCustomMenu("MY_CUSTOM", Resource.Layout.menu_text_item, Resource.Layout.menu_text_feature);

            // Custom menu order
            menuManager.SetMenu(CN.Jiguang.Imui.Chatinput.Menu.Menu.NewBuilder()
                .Customize(true)
                .SetRight(CN.Jiguang.Imui.Chatinput.Menu.Menu.TagSend)
                .SetBottom(CN.Jiguang.Imui.Chatinput.Menu.Menu.TagVoice,
                CN.Jiguang.Imui.Chatinput.Menu.Menu.TagEmoji,
                CN.Jiguang.Imui.Chatinput.Menu.Menu.TagGallery,
                CN.Jiguang.Imui.Chatinput.Menu.Menu.TagCamera, "MY_CUSTOM")
                .Build());

            menuManager.SetCustomMenuClickListener(new CustomMenuEventListener());
        }

        public PullToRefreshLayout GetPtrLayout()
        {
            return mPtrLayout;
        }

        public void SetTitle(string title)
        {
            mTitle.Text = title;
        }

        public void SetMenuClickListener(IOnMenuClickListener listener)
        {
            mChatInput.SetMenuClickListener(listener);
        }

        public void SetAdapter(MsgListAdapter adapter)
        {
            mMsgList.SetAdapter(adapter);
        }

        public void SetLayoutManager(RecyclerView.LayoutManager layoutManager)
        {
            mMsgList.SetLayoutManager(layoutManager);
        }

        public void SetRecordVoiceFile(string path, string fileName)
        {
            mRecordVoiceBtn.SetVoiceFilePath(path, fileName);
        }

        public void SetCameraCaptureFile(string path, string fileName)
        {
            mChatInput.SetCameraCaptureFile(path, fileName);
        }

        public void SetRecordVoiceListener(IRecordVoiceListener listener)
        {
            mChatInput.SetRecordVoiceListener(listener);
        }

        public void SetOnCameraCallbackListener(IOnCameraCallbackListener listener)
        {
            mChatInput.SetOnCameraCallbackListener(listener);
        }

        new public void SetOnTouchListener(IOnTouchListener listener)
        {
            mMsgList.SetOnTouchListener(listener);
        }

        public void SetOnTouchEditTextListener(IOnClickEditTextListener listener)
        {
            mChatInput.SetOnClickEditTextListener(listener);
        }

        public override bool PerformClick()
        {
            base.PerformClick();
            return true;
        }
        public ChatInputView GetChatInputView()
        {
            return mChatInput;
        }

        public MessageList GetMessageListView()
        {
            return mMsgList;
        }

        public ImageButton GetSelectAlbumBtn()
        {
            return this.mSelectAlbumIb;
        }

        public class CustomMenuEventListener : Java.Lang.Object, ICustomMenuEventListener
        {
            public void OnMenuFeatureVisibilityChanged(int visibility, string tag, MenuFeature menuFeature)
            {
                if (visibility == (int)SystemUiFlags.Visible)
                {
                    // Menu feature is visible.
                }
                else
                {
                    // Menu feature is gone.
                }
            }

            public bool OnMenuItemClick(string p0, MenuItem p1)
            {
                return true;
            }
        }
    }
}