using TimelineRuntime;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    [TimelineTrackGroupControl(typeof(DirectorGroup))]
    public class DirectorGroupControl : TimelineTrackGroupControl
    {
        public override bool IsEditing => false;
        public DirectorGroupControl(TimelineTrackGroupWrapper wrapper) : base(wrapper) { }

        protected override void UpdateHeaderControl6(Rect position)
        {
        }

    }
}
