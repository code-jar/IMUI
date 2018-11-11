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
using CN.Jiguang.Imui.Commons.Models;

namespace ImuiQS.Class.Models
{
    public class MyMessage : Java.Lang.Object, IMessage
    {
<<<<<<< HEAD

=======
>>>>>>> 4889fb595da70fb3da3b688d8b9c37f389330acc
        public MyMessage(int type)
        {
            this.Type = type;
            this.MessageStatus = MessageMessageStatus.Created;
        }
        public MyMessage(string text, int type) : this(type)
        {
            this.Text = text;
            this.MsgId = Guid.NewGuid().ToString();
        }
        public long Duration { get; set; }
        public IDictionary<string, string> Extras { get; set; }
        public IUser FromUser { get; set; }
        public string MediaFilePath { get; set; }
        public MessageMessageStatus MessageStatus { get; set; }
        public string MsgId { get; set; }
        public string Progress { get; set; }
        public string Text { get; set; }
        public string TimeString { get; set; }
        public int Type { get; set; }
    }
}