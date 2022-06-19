using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AlphaRecord : MonoBehaviour {

    private int count = 0;

    private Dictionary<Material, Color> alphaDic = new Dictionary<Material, Color>();

    public void BeginAlphaModify(SkinnedMeshRenderer[] source)
    {
        if(count == 0)
        {
            if (null != source)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    SkinnedMeshRenderer renderer = source[i];
                    if (null != renderer)
                    {
                        Material[] materials = renderer.materials;
                        if (null != materials)
                        {
                            for (int j = 0; j < materials.Length; j++)
                            {
                                Material material = materials[j];
                                if (null != material)
                                {
                                    if (material.HasProperty("_Color"))
                                    {
                                        alphaDic[material] = material.GetColor("_Color");
                                    }
                                }
                            }
                        }
                        materials = renderer.sharedMaterials;
                        if (null != materials)
                        {
                            for (int j = 0; j < materials.Length; j++)
                            {
                                Material material = materials[j];
                                if (null != material)
                                {
                                    if (material.HasProperty("_Color"))
                                    {
                                        alphaDic[material] = material.GetColor("_Color");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        count++;
    }

    public void ModifyAlpha(float alpha)
    {
        Dictionary<Material, Color>.Enumerator enumerator = alphaDic.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.Key != null)
            {
                Color color = enumerator.Current.Key.GetColor("_Color");
                color.a *= alpha;
                enumerator.Current.Key.SetColor("_Color", color);
            }
        }
    }

    public void EndAlphaModify()
    {
        count--;
        if (count == 0)
        {
            Dictionary<Material, Color>.Enumerator enumerator = alphaDic.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Key != null)
                {
                    enumerator.Current.Key.SetColor("_Color", enumerator.Current.Value);
                }
            }
            alphaDic.Clear();
        }
    }
}
