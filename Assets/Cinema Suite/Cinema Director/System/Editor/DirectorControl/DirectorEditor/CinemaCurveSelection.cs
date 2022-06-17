using System;

public class CinemaCurveSelection
{
    public string Type = string.Empty;
    public string Property = string.Empty;
    public int CurveId = -1;
    public int KeyId = -1;

    internal void Reset()
    {
        this.Type = string.Empty;
        this.Property = string.Empty;
        this.CurveId = -1;
        this.KeyId = -1;
    }
}

