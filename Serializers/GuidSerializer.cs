using System;
using System.IO;

namespace RenDBCore
{
	public class GuidSerializer : ISerializer<Guid> {

		public bool IsFixedSize {
			get { return true; }
		}

		public int Length {
			get { return 16; }
		}


		public byte[] Serialize (Guid value)
		{
			return value.ToByteArray();
		}

		public Guid Deserialize (byte[] data, int offset, int length)
		{
			if(length != 16)
				throw new ArgumentException("Invalid Guid buffer length: " + length);

			return BufferHelper.ReadGuid(data, offset);
		}
	}
}

