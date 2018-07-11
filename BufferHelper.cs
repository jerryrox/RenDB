using System;
using System.IO;

namespace RenDBCore
{
	/// <summary>
	/// A helper class for reading/writing numeric values from/to a byte array, using LittleEndian helper script. 
	/// </summary>
	public static class BufferHelper {

		public static Guid ReadGuid(byte[] buffer, int offset)
		{
			var bytes = new byte[16];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 16);
			return new Guid(bytes);
		}

		public static int ReadInt32(byte[] buffer, int offset)
		{
			var bytes = new byte[4];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 4);
			return LittleEndian.GetInt32(bytes);
		}

		public static uint ReadUInt32(byte[] buffer, int offset)
		{
			var bytes = new byte[4];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 4);
			return LittleEndian.GetUInt32(bytes);
		}

		public static long ReadInt64(byte[] buffer, int offset)
		{
			var bytes = new byte[8];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 8);
			return LittleEndian.GetInt64(bytes);
		}

		public static ulong ReadUInt64(byte[] buffer, int offset)
		{
			var bytes = new byte[8];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 8);
			return LittleEndian.GetUInt64(bytes);
		}

		public static float ReadSingle(byte[] buffer, int offset)
		{
			var bytes = new byte[4];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 4);
			return LittleEndian.GetSingle(bytes);
		}

		public static double ReadDouble(byte[] buffer, int offset)
		{
			var bytes = new byte[4];
			Buffer.BlockCopy(buffer, offset, bytes, 0, 4);
			return LittleEndian.GetDouble(bytes);
		}

		public static void WriteBuffer(int value, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(LittleEndian.GetBytes(value), 0, buffer, offset, 4);
		}

		public static void WriteBuffer(uint value, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(LittleEndian.GetBytes(value), 0, buffer, offset, 4);
		}

		public static void WriteBuffer(long value, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(LittleEndian.GetBytes(value), 0, buffer, offset, 8);
		}

		public static void WriteBuffer(ulong value, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(LittleEndian.GetBytes(value), 0, buffer, offset, 8);
		}

		public static void WriteBuffer(float value, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(LittleEndian.GetBytes(value), 0, buffer, offset, 4);
		}

		public static void WriteBuffer(double value, byte[] buffer, int offset)
		{
			Buffer.BlockCopy(LittleEndian.GetBytes(value), 0, buffer, offset, 8);
		}
	}
}

