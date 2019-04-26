using System;
using UnityEngine;

public class CurveClipItemEventArgs : EventArgs
{
	public Behaviour curveClipItem;

	public float firetime;

	public float duration;

	public CurveClipItemEventArgs(Behaviour curveClipItem, float firetime, float duration)
	{
		this.curveClipItem = curveClipItem;
		this.firetime = firetime;
		this.duration = duration;
	}
}
