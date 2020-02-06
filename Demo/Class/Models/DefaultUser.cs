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

namespace imuisample.Class
{
    public class DefaultUser : Java.Lang.Object, IUser
    {
        public DefaultUser(string id, string displayName, string avatar)
        {
            this.Id = id;
            this.DisplayName = displayName;
            this.AvatarFilePath = avatar;
        }

        public string AvatarFilePath { get; set; }
        public string DisplayName { get; set; }
        public string Id { get; set; }
    }
}