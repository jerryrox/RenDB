using System;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a storage that manages multiple blocks of equal size under its control.
	/// </summary>
	public interface IBlockStorage {

		/// <summary>
		/// Returns the total number of bytes in a block.
		/// </summary>
		int BlockSize { get; }

		/// <summary>
		/// Returns the size of a block's header.
		/// </summary>
		int BlockHeaderSize { get; }

		/// <summary>
		/// Returns the size of a block's content.
		/// </summary>
		int BlockContentSize { get; }


		/// <summary>
		/// Finds a block with speicfied id.
		/// </summary>
		IBlock Find(uint blockId);

		/// <summary>
		/// Creates a new block in storage.
		/// </summary>
		IBlock CreateNew();
	}
}