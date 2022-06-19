using Assets.Plugins.Common;
using CinemaDirector;
using UnityEngine;

namespace AGE
{
    [EventCategory("ModifyMaterialColor")]
    public class ModifyMaterialColor : TickEvent
    {
        [Template]
        public int modifyTargetId = 0;//要修改的目标

        public Color albedoColor = Color.white;
        private Color oriColor = Color.white;

        private string propertyName = "_AlbedoColor";
        public override void Process(Action action, Track track)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);

            if (modifyGo == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy No modifyTargetId GameObject]</color> action:" + action.actionName);
                return;
            }

            var renderer = modifyGo.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    mats[i] = new Material(renderer.sharedMaterials[i]);
                    bool isFind = false;
                    var property = mats[i].GetPropertyInfos();
                    for (int j = 0; j < property.Length; j++)
                    {
                        if (property[j].name == propertyName && property[j].type == Material.PropertyType.Vector)
                        {
                            isFind = true;
                        }
                    }

                    if (isFind)
                    {
                        oriColor = mats[i].GetColor(propertyName);
                        mats[i].SetColor(propertyName, albedoColor);
                    }
                }

                renderer.sharedMaterials = mats;
            }
        }

        public override void ProcessBlend(Action action, Track track, TickEvent prevEvent, float blendWeight)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
            var _prevEvent = prevEvent as ModifyMaterialColor;
            if (modifyGo == null || _prevEvent == null)
                return;
            
            var renderer = modifyGo.GetComponent<Renderer>();
            if (renderer == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }
            
            if (renderer != null)
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    bool isFind = false;
                    var property = mats[i].GetPropertyInfos();
                    for (int j = 0; j < property.Length; j++)
                    {
                        if (property[j].name == propertyName && property[j].type == Material.PropertyType.Vector)
                        {
                            isFind = true;
                        }
                    }

                    if (isFind)
                    {
                        var curColor = Color.Lerp(_prevEvent.albedoColor, albedoColor, blendWeight);
                        mats[i].SetColor(propertyName, curColor);
                    }
                }
            }
        }
        
        protected override void CopyData(BaseEvent src)
        {
            ModifyMaterialColor r = src as ModifyMaterialColor;

            if (r != null)
            {
                modifyTargetId = r.modifyTargetId;
                albedoColor = r.albedoColor;
            }
        }

        protected override void ClearData()
        {
            modifyTargetId = -1;
            albedoColor = Color.white;
        }
    }
}