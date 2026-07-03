namespace NyxNetwork.Core;

public static class NyxNetworkConfig
{
	public static bool UseCompression { get; set; } = false;
	public static bool UseEncryption { get; set; } = false;
	public static uint[] EncryptionKey { get; set; } = new uint[4];
}
