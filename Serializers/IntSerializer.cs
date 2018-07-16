using System;

namespace RenDBCore
{
	/// <summary>
	/// Variant of ISerializer for serializing int.
	/// </summary>
	public class IntSerializer : ISerializer<int> {

		private static IntSerializer DefaultSerializer;


		/// <summary>
		/// Returns a reusable static instance of this serializer.
		/// </summary>
		public static IntSerializer Default {
			get {
				if(DefaultSerializer == null)
					DefaultSerializer = new IntSerializer();
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
		public byte[] Serialize (int value)
		{
			return LittleEndian.GetBytes(value);
		}

		/// <summary>
		/// Deserializes the specified data to T object.
		/// </summary>
		public int Deserialize (byte[] data, int offset, int length)
		{
			return BufferHelper.ReadInt32(data, offset);
		}
	}
}

