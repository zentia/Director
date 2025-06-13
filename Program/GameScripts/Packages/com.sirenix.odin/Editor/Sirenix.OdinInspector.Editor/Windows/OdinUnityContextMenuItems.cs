#if UNITY_EDITOR
//-----------------------------------------------------------------------
// <copyright file="OdinUnityContextMenuItems.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Editor.Windows
{
#pragma warning disable

    using Sirenix.Utilities;
    using Sirenix.Utilities.Editor;
    using UnityEditor;
    using UnityEngine;

    internal class OdinUnityContextMenuItems
    {
        const int Group1 = 10000;
        const int Group2 = 100000;
        const int Group3 = 1000000;

        // ---------- GROUP 1 -------------

        [MenuItem("Tools/Odin Inspector/Static Inspector", priority = Group1 + 1)]
        private static void OpenStaticInspector()
        {
            StaticInspectorWindow.ShowWindow();
        }

        [MenuItem("Tools/Odin Inspector/Serialization Debugger", priority = Group1 + 2)]
        public static void ShowSerializationDebugger()
        {
            SerializationDebuggerWindow.ShowWindow();
        }

        // ---------- GROUP 2 -------------

        [MenuItem("Tools/Odin Inspector/Preferences", priority = Group2 + 1)]
        public static void OpenSirenixPreferences()
        {
            SirenixPreferencesWindow.OpenSirenixPreferences();
        }

        // ---------- CONTEXT -------------

        [MenuItem("CONTEXT/MonoBehaviour/Debug Serialization")]
        private static void ComponentContextMenuItem(MenuCommand menuCommand)
        {
            SerializationDebuggerWindow.ShowWindow(menuCommand.context.GetType());
        }
    }
}
#endif
