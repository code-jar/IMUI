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

namespace CN.Jiguang.Imui.Messages
{
    public partial class MsgListAdapter : global::Android.Support.V7.Widget.RecyclerView.Adapter
    {
        public override void OnBindViewHolder(Android.Support.V7.Widget.RecyclerView.ViewHolder holder, int position)
        {
            this.OnBindViewHolder(holder as Android.Support.V7.Widget.RecyclerView.ViewHolder, position);
        }

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder(Android.Views.ViewGroup parent, int viewType)
        {
            return this.OnCreatingViewHolder(parent, viewType);
        }
    }
}