using System;
using System.IO;

namespace RenDBCore
{
	/// <summary>
	/// Variant of ISerializer for serializing Guid.
	/// </summary>
	public class GuidSerializer : ISerializer<Guid> {

		private static GuidSerializer DefaultSerializer;


		/// <summary>
		/// Returns a reusable static instance of this serializer.
		/// </summary>
		public static GuidSerializer Default {
			get {
				if(DefaultSerializer == null)
					DefaultSerializer = new GuidSerializer();
				return DefaultSerializer;
			}
		}

		/// <summary>
		/// Returns whether specified type T has a fixed size.
		/// </summary>
		public bool IsFixedSize {
			get { return true; }
		}

		/// <summary>
		/// Returns the length of the T object.
		/// </summary>
		public int Length {
			get { return 16; }
		}


		/// <summary>
		/// Serializes the specified value to byte array.
		/// </summary>
		public byte[] Serialize (Guid value)
		{
			return value.ToByteArray();
		}

		/// <summary>
		/// Deserializes the specified data to T object.
		/// </summary>
		public Guid Deserialize (byte[] data, int offset, int length)
		{
			if(length != 16)
				throw new ArgumentException("Invalid Guid buffer length: " + length);

			return BufferHelper.ReadGuid(data, offset);
		}
	}
}

