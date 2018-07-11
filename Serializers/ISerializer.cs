using System;

namespace RenDBCore
{
	/// <summary>
	/// Interface of a serializer variant that serializes an object to byte array.
	/// </summary>
	public interface ISerializer<T> {

		/// <summary>
		/// Returns whether specified type T has a fixed size.
		/// </summary>
		bool IsFixedSize { get; }

		/// <summary>
		/// Returns the length of the T object.
		/// </summary>
		int Length { get; }


		/// <summary>
		/// Serializes the specified value to byte array.
		/// </summary>
		byte[] Serialize(T value);

		/// <summary>
		/// Deserializes the specified data to T object.
		/// </summary>
		T Deserialize(byte[] data, int offset, int length);
	}
}

