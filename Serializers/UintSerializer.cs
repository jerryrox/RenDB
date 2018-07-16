using System;
using System.IO;

namespace RenDBCore
{
	/// <summary>
	/// Variant of ISerializer for serializing uint.
	/// </summary>
	public class UintSerializer : ISerializer<uint> {

		private static UintSerializer DefaultSerializer;


		/// <summary>
		/// Returns a reusable static instance of this serializer.
		/// </summary>
		public static UintSerializer Default {
			get {
				if(DefaultSerializer == null)
					DefaultSerializer = new UintSerializer();
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
			get { return 4; }
		}


		/// <summary>
		/// Serializes the specified value to byte array.
		/// </summary>
		public byte[] Serialize (uint value)
		{
			return LittleEndian.GetBytes(value);
		}

		/// <summary>
		/// Deserializes the specified data to T object.
		/// </summary>
		public uint Deserialize (byte[] data, int offset, int length)
		{
			return BufferHelper.ReadUInt32(data, offset);
		}
	}
}

