using System;
using System.Collections.Generic;
using System.IO;

namespace RenDBCore.Internal
{
	public class BlockStorage : IBlockStorage {

		private const int MaxDiskSectorSize = 4096;
		private const int MinDiskSectorSize = 128;

		readonly Stream stream;
		readonly int blockSize;
		readonly int blockHeaderSize;
		readonly int blockContentSize;
		readonly int diskSectorSize;
		readonly Dictionary<uint, Block> blocks;


		/// <summary>
		/// Returns the total number of bytes in a block.
		/// </summary>
		public int BlockSize {
			get { return blockSize; }
		}

		/// <summary>
		/// Returns the size of a block's header.
		/// </summary>
		public int BlockHeaderSize {
			get { return blockHeaderSize; }
		}

		/// <summary>
		/// Returns the size of a block's content.
		/// </summary>
		public int BlockContentSize {
			get { return blockContentSize; }
		}

		/// <summary>
		/// Returns the size of a unit to process during streams.
		/// </summary>
		public int DiskSectorSize {
			get { return diskSectorSize; }
		}


		public BlockStorage(Stream stream,
			int blockSize = MaxDiskSectorSize,
			int blockHeaderSize = BlockFields.TotalHeaderSize)
		{
			if(stream == null)
				throw new ArgumentNullException("storage");
			if(blockHeaderSize >= blockSize)
				throw new ArgumentException("blockHeaderSize must be less than blockSize.");
			if(blockSize < MinDiskSectorSize)
				throw new ArgumentException("blockSize must be at least " + MinDiskSectorSize);

			blocks = new Dictionary<uint, Block>();

			this.stream = stream;
			this.blockSize = blockSize;
			this.blockHeaderSize = blockHeaderSize;
			this.blockContentSize = blockSize - blockHeaderSize;
			this.diskSectorSize = blockSize < MaxDiskSectorSize ? MinDiskSectorSize : MaxDiskSectorSize;
		}

		/// <summary>
		/// Finds a block with speicfied id.
		/// </summary>
		public IBlock Find (uint blockId)
		{
			// If contained in initialized collection, just return from there.
			if(blocks.ContainsKey(blockId))
				return blocks[blockId];

			// Make sure a valid block at blockPosition exists.
			var blockPosition = blockId * blockSize;
			if(blockPosition + blockSize > stream.Length)
				return null;

			// Read the block data.
			var firstSector = new byte[diskSectorSize];
			stream.Position = blockPosition;
			stream.Read(firstSector, 0, diskSectorSize);

			var block = new Block(this, blockId, firstSector, stream);
			OnBlockInitialized(block);
			return block;
		}

		/// <summary>
		/// Creates a new block in storage.
		/// </summary>
		public IBlock CreateNew ()
		{
			if((this.stream.Length % blockSize) != 0) {
				throw new DataMisalignedException(string.Format(
					"Unexpected stream length ({0}) and blockSize ({1}).",
					this.stream.Length, blockSize
				));
			}

			// Calculate new block id
			var blockId = (uint)(stream.Length / blockSize);

			// Set new length
			stream.SetLength((long)(blockId * blockSize) + blockSize);
			stream.Flush();

			// Return new block
			var block = new Block(this, blockId, new byte[diskSectorSize], stream);
			OnBlockInitialized(block);
			return block;
		}

		/// <summary>
		/// Event called when a block is initialized.
		/// </summary>
		protected virtual void OnBlockInitialized(Block block)
		{
			blocks[block.Id] = block;
			block.OnDisposed += OnBlockDisposed;
		}

		/// <summary>
		/// Event called when a block is disposed.
		/// </summary>
		protected virtual void OnBlockDisposed(Block block)
		{
			block.OnDisposed -= OnBlockDisposed;
			blocks.Remove(block.Id);
		}
	}
}

