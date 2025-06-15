namespace TimelineRuntime
{
    public interface IRecoverableObject
    {
        RevertMode RuntimeRevertMode { get; set; }

        RevertInfo[] CacheState();
    }
}
