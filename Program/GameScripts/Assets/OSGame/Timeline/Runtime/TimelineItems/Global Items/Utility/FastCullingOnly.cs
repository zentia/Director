namespace TimelineRuntime
{
    [TimelineItem("Utility", "FastCullingOnly", TimelineItemGenre.GlobalItem)]
    public class FastCullingOnly : TimelineGlobalEvent
    {
        private bool m_CacheFastCullingOnly;

        public override void Trigger()
        {
            m_CacheFastCullingOnly = Yarp.GfxCullingParameters.fastCullingOnly;
            Yarp.GfxCullingParameters.fastCullingOnly = true;

            SystemSetting.GetInstance().OptimizeSettingFlagsMode |= SystemSetting.OptimizeSettingFlags.FastCulling;
        }

        public override void Stop()
        {
            Yarp.GfxCullingParameters.fastCullingOnly = m_CacheFastCullingOnly;
            if (m_CacheFastCullingOnly)
            {
                SystemSetting.GetInstance().OptimizeSettingFlagsMode ^= SystemSetting.OptimizeSettingFlags.FastCulling;
            }
        }
    }
}
