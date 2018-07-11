using System;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a data block that represents a chunk of data stored in any storage files.
	/// </summary>
	public interface IBlock : IDisposable {

		/// <summary>
		/// Returns the unique identifier of this block.
		/// </summary>
		uint Id { get; }


		/// <summary>
		/// Returns the header on specified field.
		/// </summary>
		long GetHeader(int field);

		/// <summary>
		/// Sets the header value on specified field.
		/// </summary>
		void SetHeader(int field, long value);

		/// <summary>
		/// Reads content of this block to specified buffer with options.
		/// </summary>
		void Read(byte[] buffer, int bufferOffset, int readOffset, int length);

		/// <summary>
		/// Writes specified buffer to content of this block with options.
		/// </summary>
		void Write(byte[] buffer, int bufferOffset, int writeOffset, int length);
	}
}