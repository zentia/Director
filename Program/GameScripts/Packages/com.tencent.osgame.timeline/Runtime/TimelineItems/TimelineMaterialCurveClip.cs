using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Material Curve Clip", "Material Curve Clip", TimelineItemGenre.CurveClipItem)]
    public class TimelineMaterialCurveClip : TimelineCurveClip
    {
        public void AddMaterialCurveData(MemberCurveClipData memberCurveClipData)
        {
            if (CurveData.Find(item=>item.PropertyName == memberCurveClipData.PropertyName) != null)
                return;

            var actor = Actor;
            if (actor == null)
            {
                Debug.LogError( "Actor is null");
                return;
            }

            var renderer = actor.GetComponentInChildren<Renderer>();
            InitializeClipCurves(memberCurveClipData, renderer);
            CurveData.Add(memberCurveClipData);
            timeline?.Recache();
        }

        public override void SampleTime(Transform actor, float time)
        {
            if (Firetime <= time && time <= Firetime + Duration)
            {
                var children = actor.GetComponentsInChildren<Renderer>();
                foreach (var memberCurveClipData in CurveData)
                {
                    var value = Evaluate(memberCurveClipData, time, null);
                    foreach (var r in children)
                    {
                        switch (memberCurveClipData.PropertyType)
                        {
                            case PropertyTypeInfo.Float:
                                foreach (var sharedMaterial in r.sharedMaterials)
                                {
                                    if (sharedMaterial == null)
                                    {
                                        continue;
                                    }
                                    sharedMaterial.SetFloat(memberCurveClipData.PropertyName, (float)value);
                                }
                                break;
                            case PropertyTypeInfo.Color:
                                foreach (var sharedMaterial in r.sharedMaterials)
                                {
                                    if (sharedMaterial == null)
                                    {
                                        continue;
                                    }
                                    sharedMaterial.SetColor(memberCurveClipData.PropertyName, (Color)value);
                                }
                                break;
                        }
                    }
                }
            }
        }

        public override object GetCurrentValue(Component component, MemberCurveClipData property, bool isProperty)
        {
            if (component == null)
            {
                return null;
            }

            if (component is Renderer renderer)
            {
                switch (property.PropertyType)
                {
                    case PropertyTypeInfo.Float:
                        return renderer.sharedMaterial.GetFloat(property.PropertyName);
                    case PropertyTypeInfo.Color:
                        return renderer.sharedMaterial.GetColor(property.PropertyName);
                }
            }

            return null;
        }
    }
}
