using System;
using UnityEngine;

public class TrackItemEventArgs : EventArgs
{
	public Behaviour item;

	public float firetime;

	public TrackItemEventArgs(Behaviour item, float firetime)
	{
		this.item = item;
		this.firetime = firetime;
	}
}
