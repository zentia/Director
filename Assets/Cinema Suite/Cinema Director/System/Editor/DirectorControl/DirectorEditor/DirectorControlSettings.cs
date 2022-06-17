using System;

internal class DirectorControlSettings
{
    private bool m_HRangeLocked;
    private float m_HRangeMax = float.PositiveInfinity;
    private float m_HRangeMin = float.NegativeInfinity;
    private bool m_HSlider = true;
    private bool m_ScaleWithWindow = true;

    internal bool hRangeLocked
    {
        get => 
            this.m_HRangeLocked;
        set => 
            (this.m_HRangeLocked = value);
    }

    internal float hRangeMax
    {
        get => 
            this.m_HRangeMax;
        set => 
            (this.m_HRangeMax = value);
    }

    internal float HorizontalRangeMin
    {
        get => 
            this.m_HRangeMin;
        set => 
            (this.m_HRangeMin = value);
    }

    internal bool hSlider
    {
        get => 
            this.m_HSlider;
        set => 
            (this.m_HSlider = value);
    }

    internal bool scaleWithWindow
    {
        get => 
            this.m_ScaleWithWindow;
        set => 
            (this.m_ScaleWithWindow = value);
    }
}

