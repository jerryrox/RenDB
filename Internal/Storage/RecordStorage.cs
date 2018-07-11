using System;
using System.Collections.Generic;
using System.IO;

namespace RenDBCore.Internal
{
	public class RecordStorage : IRecordStorage {

		private const int MaxRecordSize = 1024 * 1024 * 1024;

		readonly IBlockStorage blockStorage;


		public RecordStorage (IBlockStorage blockStorage)
		{
			if(blockStorage == null)
				throw new ArgumentNullException("blockStorage");
			if(blockStorage.BlockHeaderSize < BlockFields.TotalHeaderSize) {
				throw new ArgumentException(string.Format(
					"RecordStorage requires at least {0} bytes for header size.",
					BlockFields.TotalHeaderSize
				));
			}

			this.blockStorage = blockStorage;
		}

		/// <summary>
		/// Creates a new record with empty data.
		/// </summary>
		public uint Create ()
		{
			using(IBlock firstBlock = blockStorage.CreateNew()) {
				return firstBlock.Id;
			}
		}

		/// <summary>
		/// Creates a new record with empty data and specified headers.
		/// </summary>
		public uint Create(long nextBlockId, long prevBlockId, long recordLength,
			long blockContentSize, long isDeleted)
		{
			using(IBlock firstBlock = blockStorage.CreateNew()) {
				firstBlock.SetHeader(BlockFields.NextBlockId, nextBlockId);
				firstBlock.SetHeader(BlockFields.PreviousBlockId, prevBlockId);
				firstBlock.SetHeader(BlockFields.RecordLength, recordLength);
				firstBlock.SetHeader(BlockFields.BlockContentLength, blockContentSize);
				firstBlock.SetHeader(BlockFields.IsDeleted, isDeleted);
				return firstBlock.Id;
			}
		}

		/// <summary>
		/// Creates a new record with specified data.
		/// </summary>
		public uint Create (byte[] data)
		{
			if(data == null)
				throw new ArgumentException("data must not be null.");

			return Create((id) => data);
		}

		/// <summary>
		/// Creates a new record with specified data.
		/// </summary>
		public uint Create (Func<uint, byte[]> dataProvider)
		{
			if(dataProvider == null)
				throw new ArgumentNullException("dataProvider");

			using(IBlock firstBlock = AllocateBlock()) {
				uint returnId = firstBlock.Id;
				byte[] data = dataProvider(returnId);
				int dataWritten = 0;
				int dataToWrite = data.Length;
				firstBlock.SetHeader(BlockFields.RecordLength, dataToWrite);

				// If no data to write, just return it
				if(dataToWrite == 0)
					return returnId;

				// Start writing data.
				IBlock curBlock = firstBlock;
				while(dataWritten < dataToWrite) {
					IBlock nextBlock = null;

					using(curBlock) {
						// Write data to block as much as possible.
						int curWriteSize = Math.Min(blockStorage.BlockContentSize, dataToWrite - dataWritten);
						curBlock.Write(data, dataWritten, 0, curWriteSize);
						curBlock.SetHeader(BlockFields.BlockContentLength, curWriteSize);
						dataWritten += curWriteSize;

						// If there are still remaining data, move to next block
						if(dataWritten < dataToWrite) {
							nextBlock = AllocateBlock();
							bool success = false;
							try {
								nextBlock.SetHeader(BlockFields.PreviousBlockId, curBlock.Id);
								curBlock.SetHeader(BlockFields.NextBlockId, nextBlock.Id);
								success = true;
							}
							finally {
								if(!success && nextBlock != null) {
									nextBlock.Dispose();
									nextBlock = null;
								}
							}
						}
						// If all data written, just break out.
						else
							break;
					}

					// If there is a next block to fill, reset curBlock
					if(nextBlock != null)
						curBlock = nextBlock;
				}

				// Return the firstBlock id.
				return returnId;
			}
		}

		/// <summary>
		/// Updates the record of specified id with data.
		/// </summary>
		public void Update (uint recordId, byte[] data)
		{
			int written = 0;
			int total = data.Length;
			int blocksUsed = 0;
			int contentSize = blockStorage.BlockContentSize;
			List<IBlock> blocks = FindBlocks(recordId);
			IBlock prevBlock = null;

			try {
				while(written < total) {
					// Amount of bytes to write on current block.
					int toWrite = Math.Min(contentSize, total - written);

					// Calculate the index of block where data will be written on.
					int blockInx = written / contentSize;

					// Get the target block
					IBlock target = null;
					if(blockInx < blocks.Count)
						target = blocks[blockInx];
					else {
						target = AllocateBlock();
						if(target == null)
							throw new Exception("Failed to allocate a new block.");
						blocks.Add(target);
					}

					// Link with previous block if exists.
					if(prevBlock != null) {
						prevBlock.SetHeader(BlockFields.NextBlockId, target.Id);
						target.SetHeader(BlockFields.PreviousBlockId, prevBlock.Id);
					}

					// Write the data
					target.Write(data, written, 0, toWrite);
					target.SetHeader(BlockFields.BlockContentLength, toWrite);
					target.SetHeader(BlockFields.NextBlockId, 0);
					if(written == 0)
						target.SetHeader(BlockFields.RecordLength, total);

					// Setup for next iteration
					blocksUsed ++;
					written += toWrite;
					prevBlock = target;
				}

				// Remove the remaining blocks that are unused.
				if(blocksUsed < blocks.Count)
					Delete(blocks[blocksUsed].Id);
			}
			finally {
				// Dispose all blocks used in this method.
				for(int i=0; i<blocks.Count; i++)
					blocks[i].Dispose();
			}
		}

		/// <summary>
		/// Deletes the record of specified id.
		/// </summary>
		public void Delete (uint recordId)
		{
			using(IBlock block = blockStorage.Find(recordId)) {
				IBlock curBlock = block;
				while(true) {
					IBlock nextBlock = null;

					using(curBlock) {
						// Instead of actually "deleting" the block, we simply flag it deleted for reuse.
						uint nextId = 0;
						MarkAsReusable(curBlock, out nextId);
						curBlock.SetHeader(BlockFields.IsDeleted, 1L);

						// If curBlock doesn't have a reference to the next block, just break out
						if(nextId == 0)
							break;
						// If there is a next block's id
						else {
							nextBlock = blockStorage.Find(nextId);
							if(nextBlock == null)
								throw new InvalidDataException("Block not found with id: " + nextId);
						}
					}

					// Move to next block
					if(nextBlock != null)
						curBlock = nextBlock;
				}
			}
		}

		/// <summary>
		/// Finds a record of specified id and returns its data.
		/// </summary>
		public byte[] Find (uint recordId)
		{
			// #0 record is reserved for deletion flagging.
			if(recordId == 0)
				return null;
			
			// Find the block
			using(IBlock block = blockStorage.Find(recordId)) {
				// No block found
				if(block == null)
					return null;
				// Deleted block
				if(block.GetHeader(BlockFields.IsDeleted) == 1L)
					return null;
				// Child block
				if(block.GetHeader(BlockFields.PreviousBlockId) != 0L)
					return null;

				// Get total size and allocate bytes.
				long totalSize = block.GetHeader(BlockFields.RecordLength);
				if(totalSize > MaxRecordSize)
					throw new NotSupportedException("Record size too large: " + totalSize);
				
				byte[] bytes = new byte[totalSize];
				int bytesRead = 0;

				// Fill data from blocks
				IBlock curBlock = block;
				while(true) {
					uint nextBlockId = 0;

					using(curBlock) {
						int curBlockContentSize = (int)curBlock.GetHeader(BlockFields.BlockContentLength);
						if(curBlockContentSize > blockStorage.BlockContentSize)
							throw new InvalidDataException("Block content size invalid: " + curBlockContentSize);

						// Read all data from current block
						curBlock.Read(bytes, bytesRead, 0, curBlockContentSize);
						bytesRead += curBlockContentSize;

						// End if next block doesn't exist.
						nextBlockId = (uint)curBlock.GetHeader(BlockFields.NextBlockId);
						if(nextBlockId == 0)
							return bytes;
					}

					// Prepare next block iteration
					curBlock = blockStorage.Find(nextBlockId);
					if(curBlock == null)
						throw new InvalidDataException("Block not found with id: " + nextBlockId);
				}
			}
		}

		/// <summary>
		/// Returns a list of all block id flagged as deleted.
		/// </summary>
		public List<uint> GetDeletedId()
		{
			List<IBlock> blocks = FindBlocks(0);
			List<uint> ids = new List<uint>(blocks.Count-1);

			for(int i=1; i<blocks.Count; i++)
				ids.Add(blocks[i].Id);
			return ids;
		}

		/// <summary>
		/// Finds and returns a list of all blocks matching the specified record id.
		/// </summary>
		List<IBlock> FindBlocks(uint recordId)
		{
			List<IBlock> blocks = new List<IBlock>();
			bool success = false;

			try {
				uint curBlockId = recordId;

				do {
					IBlock block = blockStorage.Find(curBlockId);
					if(block == null) {
						// If the first block was never created, create one now
						if(curBlockId == 0)
							block = blockStorage.CreateNew();
						else
							throw new Exception("Block not found with id: " + curBlockId);
					}
					// If not looking for #0 record and the block is somehow deleted, we should ignore it.
					if(recordId > 0 && block.GetHeader(BlockFields.IsDeleted) == 1L)
						throw new InvalidDataException("Block with id " + curBlockId + " is already deleted.");
					
					blocks.Add(block);
					curBlockId = (uint)block.GetHeader(BlockFields.NextBlockId);
				}
				while(curBlockId != 0);

				success = true;
				return blocks;
			}
			finally {
				// Dispose all blocks used if somehow failed.
				if(!success) {
					for(int i=0; i<blocks.Count; i++)
						blocks[i].Dispose();
				}
			}
		}

		/// <summary>
		/// Returns a block from recycle queue or by creating new.
		/// </summary>
		IBlock AllocateBlock()
		{
			uint blockId = 0;
			IBlock newBlock = null;

			// If no block available for reuse, create new
			if(!TryGetReusableBlock(out blockId)) {
				newBlock = blockStorage.CreateNew();
				if(newBlock == null)
					throw new Exception("Failed to create new block.");
			}
			// Else, recycle
			else {
				newBlock = blockStorage.Find(blockId);
				if(newBlock == null)
					throw new InvalidDataException("Block not found with id: " + blockId);
				
				newBlock.SetHeader(BlockFields.BlockContentLength, 0L);
				newBlock.SetHeader(BlockFields.IsDeleted, 0L);
				newBlock.SetHeader(BlockFields.NextBlockId, 0L);
				newBlock.SetHeader(BlockFields.PreviousBlockId, 0L);
				newBlock.SetHeader(BlockFields.RecordLength, 0L);
			}
			return newBlock;
		}

		/// <summary>
		/// Tries to find a reusable block from #0 record stack and outputs it.
		/// </summary>
		bool TryGetReusableBlock(out uint blockId)
		{
			blockId = 0;
			IBlock lastBlock = null;
			IBlock secondLastBlock = null;
			GetSpaceTrackerBlock(out lastBlock, out secondLastBlock);

			using(lastBlock) {
				using(secondLastBlock) {
					// If cur block is the #0 record, we can't dequeue this block for reuse.
					if(lastBlock.Id == 0)
						return false;

					// Remove next block id from secondLastBlock
					secondLastBlock.SetHeader(BlockFields.NextBlockId, 0L);
					// Remove prev block id from lastBlock
					lastBlock.SetHeader(BlockFields.PreviousBlockId, 0L);

					// Set blockId to lastBlock's id.
					blockId = lastBlock.Id;

					// Can dequeue
					return true;
				}
			}
		}

		/// <summary>
		/// Finds last two blocks in the recycle queue.
		/// </summary>
		void GetSpaceTrackerBlock(out IBlock lastBlock, out IBlock secondLastBlock)
		{
			lastBlock = null;
			secondLastBlock = null;

			// Find all unused blocks
			var blocks = FindBlocks(0);

			try {
				if(blocks == null || blocks.Count == 0)
					throw new Exception("Blocks with id 0 does not exist.");

				lastBlock = blocks[blocks.Count - 1];
				if(blocks.Count > 1)
					secondLastBlock = blocks[blocks.Count - 2];
			}
			finally {
				// Dispose all unused blocks
				if(blocks != null) {
					for(int i=0; i<blocks.Count; i++) {
						var block = blocks[i];
						if((lastBlock == null || block != lastBlock) &&
							(secondLastBlock == null || block != secondLastBlock)) {

							block.Dispose();
						}
					}
				}
			}
		}

		/// <summary>
		/// Flags the specified block reusable for later block allocation.
		/// Outputs nextBlockId that indicates the next block's id linked on specified block.
		/// </summary>
		void MarkAsReusable(IBlock block, out uint nextBlockId)
		{
			nextBlockId = 0;

			IBlock lastBlock = null;
			IBlock secondLastBlock = null;
			GetSpaceTrackerBlock(out lastBlock, out secondLastBlock);

			using(lastBlock) {
				using(secondLastBlock) {
					try {
						// Set last block's next block id.
						lastBlock.SetHeader(BlockFields.NextBlockId, block.Id);

						// Set specified block's prev block id.
						block.SetHeader(BlockFields.PreviousBlockId, lastBlock.Id);
						// Also, reset the specified block's next block id.
						block.SetHeader(BlockFields.NextBlockId, 0L);

						// Set next block's id so the Delete method can continue its job.
						nextBlockId = (uint)block.GetHeader(BlockFields.NextBlockId);
					}
					catch(Exception e) {
						if(lastBlock != null)
							lastBlock.Dispose();
						if(secondLastBlock != null)
							secondLastBlock.Dispose();
						
						throw e;
					}
				}
			}
		}

		/// <summary>
		/// Simplifies using MarkAsReusable by handling the out param within this method.
		/// </summary>
		void MarkAsReusable(IBlock block)
		{
			uint dummy = 0;
			MarkAsReusable(block, out dummy);
		}
	}
}

