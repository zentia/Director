using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EditorExtension;

namespace CinemaDirector
{
	public partial class DirectorWindow
	{
        private void UpdateToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(CREATE, EditorStyles.toolbarDropDown, GUILayout.Width(40)))
            {
                ShowCreateMenu();
            }

            if (GUILayout.Button("打开", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                Open(selected =>
                {
                    this.StartCoroutine(CreateAge(selected));
                });
            }

            if (GUILayout.Button(SAVE, EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                Save();
            }

            if (GUILayout.Button(SAVEAS, EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                SaveAs();
            }
            if (GUILayout.Button(pickerImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                if (cutscene != null)
                    selection.activeObject = cutscene;
            }
            var timelineName = cutscene != null ? cutscene.name : "";
            if (GUILayout.Button(timelineName, EditorStyles.toolbarButton))
            {
                Application.OpenURL("file:///" + Path.GetDirectoryName(Path.GetFullPath(directorControl.Settings.assetsPath + timelineName)));
            }
            if (cutscene && cutscene.Dirty)
            {
                GUILayout.Label("*");
            }
            else
            {
                GUILayout.Label("");
            }
            if (GetSelection().activeObject == null)
            {
                selection.activeObject = cutscene;
            }
            GUILayout.Label("T/F", EditorStyles.toolbarButton);
            GUILayout.Label(string.Format("{0:N2}/{1:N0}", m_touchTime, m_touchTime*30), EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("选择", EditorStyles.toolbarButton))
            {
                RuntimePreview();
            }
            GUI.enabled = true;
            Texture resizeTexture = cropImage;
            if (directorControl.ResizeOption == ResizeOption.Scale)
            {
                resizeTexture = scaleImage;
            }
            Rect resizeRect = GUILayoutUtility.GetRect(new GUIContent(resizeTexture), EditorStyles.toolbarDropDown, GUILayout.Width(32));
            if (GUI.Button(resizeRect, new GUIContent(resizeTexture, "Resize Option"), EditorStyles.toolbarDropDown))
            {
                GenericMenu resizeMenu = new GenericMenu();

                string[] names = Enum.GetNames(typeof(ResizeOption));

                for (int i = 0; i < names.Length; i++)
                {
                    resizeMenu.AddItem(new GUIContent(names[i]), directorControl.ResizeOption == (ResizeOption)i, chooseResizeOption, i);
                }

                resizeMenu.DropDown(new Rect(resizeRect.x, TOOLBAR_HEIGHT, 0, 0));
            }

            bool tempSnapping = GUILayout.Toggle(isSnappingEnabled, snapImage, EditorStyles.toolbarButton, GUILayout.Width(24));
            if (tempSnapping != isSnappingEnabled)
            {
                isSnappingEnabled = tempSnapping;
                directorControl.IsSnappingEnabled = isSnappingEnabled;
            }
            
            if (GUILayout.Button(rescaleImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                directorControl.Rescale();
            }
            if (GUILayout.Button(new GUIContent(zoomInImage, "Zoom In"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                directorControl.ZoomIn();
            }
            if (GUILayout.Button(zoomOutImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                directorControl.ZoomOut();
            }
            var temp = GUI.color;
            GUI.color = directorControl.InPreviewMode ? Color.red : temp;
            directorControl.InPreviewMode = GUILayout.Toggle(directorControl.InPreviewMode, PREVIEW_MODE, EditorStyles.toolbarButton, GUILayout.Width(60));
            GUI.color = directorControl.InCurveMode ? Color.yellow : temp;
            directorControl.InCurveMode = GUILayout.Toggle(directorControl.InCurveMode, "曲线模式",
                EditorStyles.toolbarButton, GUILayout.Width(60));
            GUI.color = directorControl.InMultiMode ? Color.green : temp;
            directorControl.InMultiMode = GUILayout.Toggle(directorControl.InMultiMode, "多显", EditorStyles.toolbarButton, GUILayout.Width(30));
            GUI.color = temp;
            
            if (GUILayout.Button(settingsImage, EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                GetSelection().activeObject = directorControl.Settings;
            }

            EditorGUILayout.EndHorizontal();
        }

        public void Open(Action<string> onSelect)
        {
            new BetterSelector<string>()
            {
                candiates = GetAllAgePathes().Select(p => new BetterSelectorItem<string>(Path.ChangeExtension(p, null).Replace(@"\", "/"), p)),
                onSelect = onSelect
            }.ShowInPopup();
        }

        private static IEnumerable<BetterSelectorItem<string>> GetAllAction()
        {
            var list = new List<BetterSelectorItem<string>>();
            var runningActions = AGE.ActionService.instance.HistoryRunningActions;
            runningActions.ForEach(o=>list.Add(o.name));
            return list;
        }

        private void RuntimePreview()
        {
            new BetterSelector<string>()
            {
                candiates = GetAllAction(),
                onSelect = OnSelectTimeline
            }.ShowInPopup();
        }
    }
}