using DirectorEditor;
using UnityEngine;

public class DirectorControlState
{
	public bool IsInPreviewMode;

	public bool IsSnapEnabled;

	public ResizeOption ResizeOption;

	public int DefaultTangentMode;

	public float TickDistance;

	public float ScrubberPosition;
	public Vector2 Translation;

	public Vector2 Scale;

	public float TimeToPosition(float time)
	{
		return time * Scale.x + Translation.x;
	}

	public float SnappedTime(float time)
	{
	    bool flag = Event.current.control;

        if ((IsSnapEnabled && !flag) || (!IsSnapEnabled && flag))
		{
			time = ((int)((time + TickDistance / 2f) / TickDistance)) * TickDistance;
		}
		return time;
	}
}
