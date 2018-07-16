using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Variant of IDatabase that stores data in memory.
	/// Used for cases where a database needs to be used temporarily.
	/// </summary>
	public class MemoryDatabase<T> : BaseDatabase<T>, IDisposable where T : class, IModel<T> {
		
		public MemoryDatabase(string name, ISerializer<T> modelSerializer, int blockSize)
			: base(name, modelSerializer)
		{
			dbStream = new MemoryStream();
			RecStorage = new RecordStorage(new BlockStorage(
				dbStream, blockSize
			));

			CreateUniqueIndex();
		}

		~MemoryDatabase()
		{
			base.Dispose(false);
		}

		/// <summary>
		/// Disposes this database instance.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose(true);
		}

		/// <summary>
		/// Returns a new IO stream with specified params.
		/// </summary>
		protected override Stream GetNewIndexStream (string label)
		{
			return new MemoryStream();
		}

		/// <summary>
		/// Returns a new node manager instance with specified parameters.
		/// </summary>
		protected override ITreeNodeManager<K, uint> GetNewNodeManager<K> (
			ISerializer<K> keySerializer,
			UintSerializer valueSerializer,
			IRecordStorage recordStorage,
			ushort minEntriesPerNode)
		{
			return new MemoryTreeNodeManager<K, uint>(minEntriesPerNode, Comparer<K>.Default);
		}
	}
}

