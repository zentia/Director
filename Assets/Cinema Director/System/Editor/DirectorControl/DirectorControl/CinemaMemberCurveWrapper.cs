using UnityEngine;

public class CinemaMemberCurveWrapper
{
	public string Type;
	public string PropertyName;
	public Texture Texture;
	public bool IsVisible = true;
    public bool onlySelf = false;
	public bool IsFoldedOut = true;
	public CinemaAnimationCurveWrapper[] AnimationCurves;
}
