using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CinemaDirector;

namespace AGE
{

    [EventCategory("Utility")]
    public class SetVisibility : TickEvent
    {
        public bool enabled = true;
        public bool includeInactive = false;
        public string[] excludeMeshes = new string[0];

        Dictionary<string, bool> excludeMeshNames = new Dictionary<string, bool>();

        [Template]
        public int targetId = 0;

        public override bool SupportEditMode()
        {
            return true;
        }

        public override void Process(Action _action, Track _track)
        {
            if (excludeMeshNames.Count != excludeMeshes.Length)
            {
                for (int i = 0; i < excludeMeshes.Length; i++)
                {
                    string meshName = excludeMeshes[i];
                    excludeMeshNames.Add(meshName, true);
                }
            }

            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null) return;

            //SetChild(targetObject); 此方法太耗
            SetVisible(targetObject, enabled);
        }

        void SetVisible(GameObject go, bool visible)
        {
            if (go == null)
                return;

            MeshRenderer[] meshRenderers = go.GetComponentsInChildren<MeshRenderer>(includeInactive);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                meshRenderer.enabled = visible;
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            for (int j = 0; j < skinnedMeshRenderers.Length; j++)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[j];
                skinnedMeshRenderer.enabled = visible;
            }
        }

        void SetChild(GameObject _obj)
        {
            string objectName = _obj.name;
            if (excludeMeshNames.ContainsKey(objectName))
                return;

            Renderer renderer = _obj.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = enabled;

            for (int i = 0; i < _obj.transform.childCount; i++)
            {
                Transform child = _obj.transform.GetChild(i);
                SetChild(child.gameObject);

            }
        }


        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as SetVisibility;
            enabled = copySrc.enabled;
            excludeMeshes = copySrc.excludeMeshes;
            var enu = copySrc.excludeMeshNames.GetEnumerator();
            while(enu.MoveNext())
            {
                excludeMeshNames.Add(enu.Current.Key,enu.Current.Value);
            }
            targetId = copySrc.targetId;
            includeInactive = copySrc.includeInactive;
        }

        protected override void ClearData()
        {
            enabled = true;
            excludeMeshes = new string[0];
            excludeMeshNames.Clear();
            targetId = 0;
            includeInactive = false;
        }

        protected override uint GetPoolInitCount()
        {
            return 20;
        }
    }
}
