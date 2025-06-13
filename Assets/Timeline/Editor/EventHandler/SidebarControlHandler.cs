using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void SidebarControlHandler(object sender, SidebarControlEventArgs e);
    
    public class SidebarControlEventArgs : EventArgs
    {
        public Behaviour Behaviour;
        public SidebarControl SidebarControl;

        public SidebarControlEventArgs(Behaviour behaviour, SidebarControl control)
        {
            Behaviour = behaviour;
            SidebarControl = control;
        }
    }
}

