using System;

public class CutsceneItemControlAttribute : Attribute
{
    private Type itemType;
    private int drawPriority;

    public CutsceneItemControlAttribute(Type type)
    {
        this.itemType = type;
        this.drawPriority = 0;
    }

    public CutsceneItemControlAttribute(Type type, int drawPriority)
    {
        this.itemType = type;
        this.drawPriority = drawPriority;
    }

    public Type ItemType =>
        this.itemType;

    public int DrawPriority =>
        this.drawPriority;
}

