using System;
using System.IO;

namespace RenDBCore.Internal
{
	public class Block : IBlock {
		
		public event DisposeHandler OnDisposed;

		readonly BlockStorage blockStorage;
		readonly uint blockId;
		readonly byte[] firstSector;
		readonly Stream stream;
		readonly long?[] cachedHeaders;

		bool isFirstSectorDirty;
		bool isDisposed;


		/// <summary>
		/// Returns the unique identifier of this block.
		/// </summary>
		public uint Id {
			get { return blockId; }
		}


		/// <summary>
		/// Delegate for handling on-dispose event.
		/// </summary>
		public delegate void DisposeHandler(Block block);


		public Block(BlockStorage blockStorage, uint blockId, byte[] firstSector, Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException("stream");
			if(firstSector == null)
				throw new ArgumentNullException("firstSector");
			if(firstSector.Length != blockStorage.DiskSectorSize)
				throw new ArgumentException("firstSector's length must equal: " + blockStorage.DiskSectorSize);

			this.blockStorage = blockStorage;
			this.blockId = blockId;
			this.firstSector = firstSector;
			this.stream = stream;

			// There are currently 5 headers reserved for each block.
			cachedHeaders = new long?[5];

			isFirstSectorDirty = false;
			isDisposed = false;
		}

		~Block()
		{
			Dispose(false);
		}

		/// <summary>
		/// Returns the header on specified field.
		/// </summary>
		public long GetHeader (int field)
		{
			if(isDisposed)
				throw new ObjectDisposedException("Block");
			if(field < 0)
				throw new IndexOutOfRangeException();
			if(field >= (blockStorage.BlockHeaderSize / 8))
				throw new ArgumentException("Invalid field: " + field);

			// If field is a reserved header, return from cache.
			if(field < cachedHeaders.Length) {
				if(cachedHeaders[field] == null)
					cachedHeaders[field] = BufferHelper.ReadInt64(firstSector, field * 8);
				return cachedHeaders[field].Value;
			}
			return BufferHelper.ReadInt64(firstSector, field * 8);
		}

		/// <summary>
		/// Sets the header value on specified field.
		/// </summary>
		public void SetHeader (int field, long value)
		{
			if(isDisposed)
				throw new ObjectDisposedException("Block");
			if(field < 0)
				throw new IndexOutOfRangeException("field must not be less than 0.");

			// Update cache if reserved header.
			if(field < cachedHeaders.Length)
				cachedHeaders[field] = value;

			BufferHelper.WriteBuffer(value, firstSector, field * 8);
			isFirstSectorDirty = true;
		}

		/// <summary>
		/// Reads content of this block to specified buffer with options.
		/// </summary>
		public void Read (byte[] buffer, int bufferOffset, int readOffset, int length)
		{
			int contentSize = blockStorage.BlockContentSize;

			if(isDisposed)
				throw new ObjectDisposedException("Block");
			if(length < 0 || length + readOffset > contentSize)
				throw new ArgumentOutOfRangeException("length");
			if(length + bufferOffset > buffer.Length)
				throw new ArgumentOutOfRangeException("length");

			int read = 0;
			int headerSize = blockStorage.BlockHeaderSize;
			int sectorSize = blockStorage.DiskSectorSize;
			bool readFromFirst = contentSize + readOffset < sectorSize;

			// If data to read starts from first sector
			if(readFromFirst) {
				int copyLength = Math.Min(
					sectorSize - headerSize - readOffset,
					length
				);

				Buffer.BlockCopy(
					firstSector,
					headerSize + readOffset,
					buffer,
					bufferOffset,
					copyLength
				);

				read += copyLength;
			}

			// If there are still data to be copied, move stream position
			if(read < length) {
				if(readFromFirst)
					stream.Position = (blockId * blockStorage.BlockSize) + sectorSize;
				else
					stream.Position = (blockId * blockStorage.BlockSize) + headerSize + readOffset;
			}

			// Copy the rest of data.
			while(read < length) {
				int copyLength = Math.Min(sectorSize, length - read);
				int curRead = stream.Read(buffer, read + bufferOffset, copyLength);
				if(curRead == 0)
					throw new EndOfStreamException();
				read += curRead;
			}
		}

		/// <summary>
		/// Writes specified buffer to content of this block with options.
		/// </summary>
		public void Write (byte[] buffer, int bufferOffset, int writeOffset, int length)
		{
			int contentSize = blockStorage.BlockContentSize;

			if(isDisposed)
				throw new ObjectDisposedException("Block");
			if(writeOffset < 0 || writeOffset + length > contentSize)
				throw new ArgumentOutOfRangeException("length");
			if(bufferOffset < 0 || bufferOffset + length > buffer.Length)
				throw new ArgumentOutOfRangeException("length");

			int headerSize = blockStorage.BlockHeaderSize;
			int sectorSize = blockStorage.DiskSectorSize;

			// If buffer should be written starting from first sector
			if(headerSize + writeOffset < sectorSize) {
				int writeCount = Math.Min(
					sectorSize - headerSize - writeOffset,
					length
				);
				Buffer.BlockCopy(
					buffer,
					bufferOffset,
					firstSector,
					writeOffset + headerSize,
					writeCount
				);
				isFirstSectorDirty = true;
			}

			// If there are still data to be copied after first sector
			if(headerSize + writeOffset + length > sectorSize) {
				// Set position
				stream.Position = blockId * blockStorage.BlockSize
					+ Math.Max(sectorSize, headerSize + writeOffset);

				// Calculate number of bytes already written and exclude from length.
				int written = sectorSize - headerSize - writeOffset;
				if(written > 0) {
					writeOffset += written;
					length -= written;
				}

				// Write the rest of data.
				int curWrite = 0;
				while(curWrite < length) {
					int toWrite = Math.Min(sectorSize, length - curWrite);
					stream.Write(buffer, curWrite + writeOffset, toWrite);
					stream.Flush();
					curWrite += toWrite;
				}
			}
		}

		public void Dispose ()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing && !isDisposed) {
				isDisposed = true;

				if(isFirstSectorDirty) {
					stream.Position = (blockId * blockStorage.BlockSize);
					stream.Write(firstSector, 0, blockStorage.DiskSectorSize);
					stream.Flush();
					isFirstSectorDirty = false;
				}

				if(OnDisposed != null)
					OnDisposed(this);
			}
		}
	}
}

