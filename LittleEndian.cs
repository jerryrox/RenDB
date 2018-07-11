using System;

namespace RenDBCore
{
	/// <summary>
	/// Helper class that converts data from/to little-endian bytes.
	/// </summary>
	public static class LittleEndian {
		
		public static byte[] GetBytes(int value)
		{
			var bytes = BitConverter.GetBytes(value);
			if(!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(uint value)
		{
			var bytes = BitConverter.GetBytes(value);
			if(!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(long value)
		{
			var bytes = BitConverter.GetBytes(value);
			if(!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(ulong value)
		{
			var bytes = BitConverter.GetBytes(value);
			if(!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(float value)
		{
			var bytes = BitConverter.GetBytes(value);
			if(!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(double value)
		{
			var bytes = BitConverter.GetBytes(value);
			if(!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static int GetInt32(byte[] bytes)
		{
			if(BitConverter.IsLittleEndian)
				return BitConverter.ToInt32(bytes, 0);

			var newBytes = new byte[bytes.Length];
			bytes.CopyTo(newBytes, 0);
			Array.Reverse(newBytes);
			return BitConverter.ToInt32(newBytes, 0);
		}

		public static uint GetUInt32(byte[] bytes)
		{
			if(BitConverter.IsLittleEndian)
				return BitConverter.ToUInt32(bytes, 0);

			var newBytes = new byte[bytes.Length];
			bytes.CopyTo(newBytes, 0);
			Array.Reverse(newBytes);
			return BitConverter.ToUInt32(newBytes, 0);
		}

		public static long GetInt64(byte[] bytes)
		{
			if(BitConverter.IsLittleEndian)
				return BitConverter.ToInt64(bytes, 0);

			var newBytes = new byte[bytes.Length];
			bytes.CopyTo(newBytes, 0);
			Array.Reverse(newBytes);
			return BitConverter.ToInt64(newBytes, 0);
		}

		public static ulong GetUInt64(byte[] bytes)
		{
			if(BitConverter.IsLittleEndian)
				return BitConverter.ToUInt64(bytes, 0);

			var newBytes = new byte[bytes.Length];
			bytes.CopyTo(newBytes, 0);
			Array.Reverse(newBytes);
			return BitConverter.ToUInt64(newBytes, 0);
		}

		public static float GetSingle(byte[] bytes)
		{
			if(BitConverter.IsLittleEndian)
				return BitConverter.ToSingle(bytes, 0);

			var newBytes = new byte[bytes.Length];
			bytes.CopyTo(newBytes, 0);
			Array.Reverse(newBytes);
			return BitConverter.ToSingle(newBytes, 0);
		}

		public static double GetDouble(byte[] bytes)
		{
			if(BitConverter.IsLittleEndian)
				return BitConverter.ToDouble(bytes, 0);

			var newBytes = new byte[bytes.Length];
			bytes.CopyTo(newBytes, 0);
			Array.Reverse(newBytes);
			return BitConverter.ToDouble(newBytes, 0);
		}
	}
}