using System.Collections;
using EditorExtension;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    public partial class DirectorWindow
    {
        private void Save()
        {
            if (cutscene == null)
            {
                return;
            }
            cutscene.ExportXML(directorControl.Settings.assetsPath);
        }

        private void SaveAs()
        {
            Save();
        }

        [EditorHooker(EditorHookerAttribute.EditorHookerType.Scene, typeof(DirectorWindow))]
        private static bool CheckSwitchScene()
        {
            return CheckModifyCutscene(0);
        }

        private static bool CheckModifyCutscene(int mode)
        {
            if (instance == null || instance.cutscene == null)
                return true;
            if (instance.cutscene.Dirty)
            {
                if (mode == 0)
                {
                    switch (EditorUtility.DisplayDialogComplex("提示?", "当前AGE还没有保存，是否保存？ ", "保存", "取消操作", "放弃修改(慎重！)"))
                    {
                        case 0:
                            instance.Save();
                            return true;
                        case 1:
                            return false;
                        case 2:
                            return true;
                    }
                }
                else
                {
                    if (EditorUtility.DisplayDialog("提示?", "当前AGE还没有保存，是否保存？ ", "保存", "放弃修改(慎重！)"))
                    {
                        instance.Save();
                    }
                    return true;
                }
            }
            return true;
        }
        
        
        private IEnumerator DelaySetFocusedWindow()
        {
            yield return new WaitForSeconds(1);
            ExtensionMethod.SetFocusedWindow(this);
        }

        public void SetFocusedWindow()
        {
            this.StartCoroutine(DelaySetFocusedWindow());
        }
    }
}