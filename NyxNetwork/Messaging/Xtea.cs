using System;

namespace NyxNetwork.Messaging;

public static class Xtea
{
	private const uint Delta = 0x9E3779B9;

	public static void Encrypt(byte[] data, int offset, int length, uint[] key)
	{
		if (key == null || key.Length < 4)
			throw new ArgumentException("XTEA key must be 128-bit (4 uints).", nameof(key));

		unchecked
		{
			for (int i = offset; i < offset + length; i += 8)
			{
				uint v0 = BitConverter.ToUInt32(data, i);
				uint v1 = BitConverter.ToUInt32(data, i + 4);
				uint sum = 0;
				for (int j = 0; j < 32; j++)
				{
					v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
					sum += Delta;
					v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
				}
				Buffer.BlockCopy(BitConverter.GetBytes(v0), 0, data, i, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(v1), 0, data, i + 4, 4);
			}
		}
	}

	public static void Decrypt(byte[] data, int offset, int length, uint[] key)
	{
		if (key == null || key.Length < 4)
			throw new ArgumentException("XTEA key must be 128-bit (4 uints).", nameof(key));

		unchecked
		{
			for (int i = offset; i < offset + length; i += 8)
			{
				uint v0 = BitConverter.ToUInt32(data, i);
				uint v1 = BitConverter.ToUInt32(data, i + 4);
				uint sum = Delta * 32;
				for (int j = 0; j < 32; j++)
				{
					v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
					sum -= Delta;
					v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
				}
				Buffer.BlockCopy(BitConverter.GetBytes(v0), 0, data, i, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(v1), 0, data, i + 4, 4);
			}
		}
	}
}
