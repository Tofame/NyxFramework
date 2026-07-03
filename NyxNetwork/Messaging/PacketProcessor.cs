using System;
using System.IO;
using System.IO.Compression;
using NyxNetwork.Core;

namespace NyxNetwork.Messaging;

public static class PacketProcessor
{
	public static byte[] Process(byte[] rawPayload)
	{
		bool useComp = NyxNetworkConfig.UseCompression;
		bool useEnc = NyxNetworkConfig.UseEncryption;

		if (!useComp && !useEnc)
		{
			return rawPayload;
		}

		byte[] current = rawPayload;
		int uncompressedLength = rawPayload.Length;

		if (useComp)
		{
			using var ms = new MemoryStream();
			using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true))
			{
				deflate.Write(rawPayload, 0, rawPayload.Length);
			}
			current = ms.ToArray();
		}

		if (useEnc)
		{
			byte[] payloadWithHeader;
			if (useComp)
			{
				payloadWithHeader = new byte[8 + current.Length];
				Buffer.BlockCopy(BitConverter.GetBytes(current.Length), 0, payloadWithHeader, 0, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(uncompressedLength), 0, payloadWithHeader, 4, 4);
				Buffer.BlockCopy(current, 0, payloadWithHeader, 8, current.Length);
			}
			else
			{
				payloadWithHeader = new byte[4 + current.Length];
				Buffer.BlockCopy(BitConverter.GetBytes(uncompressedLength), 0, payloadWithHeader, 0, 4);
				Buffer.BlockCopy(current, 0, payloadWithHeader, 4, current.Length);
			}

			int remainder = payloadWithHeader.Length % 8;
			int padLength = remainder == 0 ? 0 : 8 - remainder;
			byte[] padded = new byte[payloadWithHeader.Length + padLength];
			Buffer.BlockCopy(payloadWithHeader, 0, padded, 0, payloadWithHeader.Length);

			Xtea.Encrypt(padded, 0, padded.Length, NyxNetworkConfig.EncryptionKey);
			return padded;
		}
		else
		{
			byte[] result = new byte[4 + current.Length];
			Buffer.BlockCopy(BitConverter.GetBytes(uncompressedLength), 0, result, 0, 4);
			Buffer.BlockCopy(current, 0, result, 4, current.Length);
			return result;
		}
	}

	public static byte[] Revert(byte[] processedPayload)
	{
		bool useComp = NyxNetworkConfig.UseCompression;
		bool useEnc = NyxNetworkConfig.UseEncryption;

		if (!useComp && !useEnc)
		{
			return processedPayload;
		}

		if (useEnc)
		{
			byte[] decrypted = new byte[processedPayload.Length];
			Buffer.BlockCopy(processedPayload, 0, decrypted, 0, processedPayload.Length);
			Xtea.Decrypt(decrypted, 0, decrypted.Length, NyxNetworkConfig.EncryptionKey);

			if (useComp)
			{
				int compressedLength = BitConverter.ToInt32(decrypted, 0);
				int uncompressedLength = BitConverter.ToInt32(decrypted, 4);
				if (compressedLength < 0 || compressedLength > decrypted.Length - 8)
					throw new InvalidDataException("Invalid decrypted compressed length.");

				byte[] compressed = new byte[compressedLength];
				Buffer.BlockCopy(decrypted, 8, compressed, 0, compressedLength);

				return Decompress(compressed, uncompressedLength);
			}
			else
			{
				int uncompressedLength = BitConverter.ToInt32(decrypted, 0);
				if (uncompressedLength < 0 || uncompressedLength > decrypted.Length - 4)
					throw new InvalidDataException("Invalid decrypted payload length.");

				byte[] raw = new byte[uncompressedLength];
				Buffer.BlockCopy(decrypted, 4, raw, 0, uncompressedLength);
				return raw;
			}
		}
		else
		{
			int uncompressedLength = BitConverter.ToInt32(processedPayload, 0);
			byte[] compressed = new byte[processedPayload.Length - 4];
			Buffer.BlockCopy(processedPayload, 4, compressed, 0, compressed.Length);

			return Decompress(compressed, uncompressedLength);
		}
	}

	private static byte[] Decompress(byte[] compressed, int uncompressedLength)
	{
		using var msInput = new MemoryStream(compressed);
		using var deflate = new DeflateStream(msInput, CompressionMode.Decompress);
		byte[] result = new byte[uncompressedLength];
		int totalRead = 0;
		while (totalRead < uncompressedLength)
		{
			int read = deflate.Read(result, totalRead, uncompressedLength - totalRead);
			if (read == 0)
				break;
			totalRead += read;
		}
		return result;
	}
}
