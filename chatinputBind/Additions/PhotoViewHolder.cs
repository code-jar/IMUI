using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace CN.Jiguang.Imui.Chatinput.Photo
{
    public partial class PhotoAdapter : global::Android.Support.V7.Widget.RecyclerView.Adapter
    {
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            this.OnBindViewHolder(holder, position, null);
        }

    }

}