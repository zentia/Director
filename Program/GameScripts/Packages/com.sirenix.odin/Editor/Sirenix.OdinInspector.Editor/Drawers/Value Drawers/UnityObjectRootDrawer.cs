#if UNITY_EDITOR
//-----------------------------------------------------------------------
// <copyright file="UnityObjectRootDrawer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Sirenix.OdinInspector.Editor.Drawers
{
#pragma warning disable

    using UnityEngine;

    [DrawerPriority(100, 0, 0)] // This should override most things, including the property context menu
    public sealed class UnityObjectRootDrawer<T> : OdinValueDrawer<T>
        where T : UnityEngine.Object
    {
        protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            return property.IsTreeRoot;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var count = this.Property.Children.Count;

            for (int i = 0; i < count; i++)
            {
                this.Property.Children[i].Draw();
            }
        }
    }
}
#endif