using System;

namespace RenDBCore
{
	public class IntSerializer : ISerializer<int> {

		public bool IsFixedSize {
			get { return true; }
		}
		public int Length {
			get { return 4; }
		}


		public byte[] Serialize (int value)
		{
			return LittleEndian.GetBytes(value);
		}

		public int Deserialize (byte[] data, int offset, int length)
		{
			return BufferHelper.ReadInt32(data, offset);
		}
	}
}

