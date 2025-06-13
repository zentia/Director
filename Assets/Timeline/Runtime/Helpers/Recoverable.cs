namespace TimelineRuntime
{
    internal interface IRecoverableObject
    {
        RevertMode RuntimeRevertMode { get; set; }

        RevertInfo[] CacheState();
    }
}
