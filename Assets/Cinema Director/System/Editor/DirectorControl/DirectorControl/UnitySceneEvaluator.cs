using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitySceneEvaluator
{
	public static List<Transform> GetHighestRankedGameObjects(int amount)
	{
		List<Transform> list = new List<Transform>();
		Transform[] arg_11_0 = UnityEngine.Object.FindObjectsOfType<Transform>();
		List<KeyValuePair<float, Transform>> list2 = new List<KeyValuePair<float, Transform>>();
		Transform[] array = arg_11_0;
		for (int i = 0; i < array.Length; i++)
		{
			Transform transform = array[i];
			float score = UnitySceneEvaluator.GetScore(transform.gameObject);
			KeyValuePair<float, Transform> item = new KeyValuePair<float, Transform>(score, transform);
			list2.Add(item);
		}
		list2.Sort(new Comparison<KeyValuePair<float, Transform>>(UnitySceneEvaluator.Comparison));
		foreach (KeyValuePair<float, Transform> current in list2)
		{
			if (list.Count < amount)
			{
				list.Add(current.Value);
			}
		}
		return list;
	}

	public static float GetScore(GameObject gameObject)
	{
		float arg_9A_0 = 0f + (gameObject.isStatic ? 0f : 20f) + ((gameObject.tag == "Untagged") ? 0f : 20f);
		bool flag = false;
		if (gameObject.GetComponent<Animation>() != null)
		{
			flag = true;
		}
		if (gameObject.GetComponent<AudioSource>() != null)
		{
			flag = true;
		}
		if (gameObject.GetComponent<Camera>() != null)
		{
			flag = true;
		}
		if (gameObject.GetComponent<Light>() != null)
		{
			flag = true;
		}
		if (gameObject.GetComponent<Rigidbody>() != null)
		{
			flag = true;
		}
		return arg_9A_0 + (flag ? 20f : 0f);
	}

	private static int Comparison(KeyValuePair<float, Transform> x, KeyValuePair<float, Transform> y)
	{
		int result = 0;
		if (x.Key < y.Key)
		{
			result = 1;
		}
		else if (x.Key > y.Key)
		{
			result = -1;
		}
		return result;
	}
}
