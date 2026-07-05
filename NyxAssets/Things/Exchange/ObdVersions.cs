namespace NyxAssets.Things.Exchange;

/// <summary>Object Builder Data (.obd) format versions.</summary>
public static class ObdVersions
{
    public const ushort Version1 = 100;
    public const ushort Version2 = 200;
    public const ushort Version3 = 300;

    public static bool IsSupported(ushort version) =>
        version is Version1 or Version2 or Version3;
}
