namespace Nager.MtaMilter.Models
{
    [Flags]
    public enum MtaFeatureFlags : int
    {
        CanModifyHeader = 0x01,
        CanModidyBody = 0x02,
        SupportsQuarantine = 0x04,
        SupportsMacroExtension = 0x08
    }
}
