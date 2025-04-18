using DirectorEditor;
using System;
using UnityEngine;

public class DirectorControlState
{
    public bool IsInPreviewMode;
    public bool IsSnapEnabled;
    public DirectorEditor.ResizeOption ResizeOption;
    public float TickDistance;
    public float ScrubberPosition;
    public Vector2 Translation;
    public Vector2 Scale;

    public float SnappedTime(float time)
    {
        if ((this.IsSnapEnabled && !Event.current.control) || (!this.IsSnapEnabled && Event.current.control))
        {
            time = ((int) ((time + (this.TickDistance / 2f)) / this.TickDistance)) * this.TickDistance;
        }
        return time;
    }

    public float TimeToPosition(float time) => 
        ((time * this.Scale.x) + this.Translation.x);
}

