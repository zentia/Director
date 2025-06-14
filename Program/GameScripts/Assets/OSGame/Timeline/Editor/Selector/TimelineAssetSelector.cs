using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineAssetSelector:OdinSelector<string>
    {
        private const string Root = "Assets/CustomResources/";
        private string[] m_PathList;

        public static TimelineAssetSelector Create(Action<IEnumerable<string>> onSelectionConfirmed, string[] pathList, float windowWidth)
        {
            var selector = new TimelineAssetSelector();
            selector.SelectionConfirmed += onSelectionConfirmed;
            selector.m_PathList = pathList;
            selector.EnableSingleClickToSelect();
            selector.ShowInPopup(windowWidth);
            return selector;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            foreach (var p in m_PathList)
            {
                var path = Path.Combine(Root, p);
                var assets = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                foreach (var guid in assets)
                {
                    var file = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                    if (asset.GetComponent<Timeline>())
                    {
                        tree.Add(file.Substring(path.Length + 1, file.Length - 7 - path.Length - 1).Replace("\\", "/"), file);
                    }
                }
            }

        }
    }
}
