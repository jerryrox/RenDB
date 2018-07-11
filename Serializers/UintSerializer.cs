using System;
using System.IO;

namespace RenDBCore
{
	public class UintSerializer : ISerializer<uint> {

		public bool IsFixedSize {
			get { return true; }
		}
		public int Length {
			get { return 4; }
		}


		public byte[] Serialize (uint value)
		{
			return LittleEndian.GetBytes(value);
		}

		public uint Deserialize (byte[] data, int offset, int length)
		{
			return BufferHelper.ReadUInt32(data, offset);
		}
	}
}

