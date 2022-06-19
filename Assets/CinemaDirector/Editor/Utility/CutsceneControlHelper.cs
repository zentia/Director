using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    public class ControlCommond
    {
        public Action<KeyCode> OnKeyDown;
        public Action OnKeyUp;
        public Action OnValidate;
        public Action OnExecute;
    }
}
