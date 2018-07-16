using System;
using System.IO;
using System.Text;

namespace RenDBCore
{
	/// <summary>
	/// Variant of ISerializer for serializing string.
	/// </summary>
	public class StringSerializer : ISerializer<string> {

		private static StringSerializer DefaultSerializer;


		/// <summary>
		/// Returns a reusable static instance of this serializer.
		/// </summary>
		public static StringSerializer Default {
			get {
				if(DefaultSerializer == null)
					DefaultSerializer = new StringSerializer();
				return DefaultSerializer;
			}
		}

		/// <summary>
		/// Returns whether specified type T has a fixed size.
		/// </summary>
		public bool IsFixedSize {
			get { return false; }
		}

		/// <summary>
		/// Returns the length of the T object.
		/// </summary>
		public int Length {
			get { return 0; }
		}


		/// <summary>
		/// Serializes the specified value to byte array.
		/// </summary>
		public byte[] Serialize (string value)
		{
			var data = Encoding.UTF8.GetBytes(value);
			int length = data.Length;

			byte[] buffer = new byte[4 + length];

			// Write data length
			BufferHelper.WriteBuffer(length, buffer, 0);

			// Write data
			Buffer.BlockCopy(data, 0, buffer, 4, length);

			return buffer;
		}

		/// <summary>
		/// Deserializes the specified data to T object.
		/// </summary>
		public string Deserialize (byte[] data, int offset, int length)
		{
			return Encoding.UTF8.GetString(
				data, offset + 4, length - 4
			);
		}
	}
}

