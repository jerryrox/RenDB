using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Variant of IDatabase that stores data on disk.
	/// Used in general cases.
	/// </summary>
	public class DiskDatabase<T> : BaseDatabase<T>, IDisposable where T : class, IModel<T> {

		private string dbDirectory;


		public DiskDatabase(string name, ISerializer<T> modelSerializer, string directory, int blockSize)
			: base(name, modelSerializer)
		{
			this.dbDirectory = directory;

			// Create directory
			if(!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			dbStream = new FileStream(
				GetMainDbPath(),
				FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
				blockSize
			);
			RecStorage = new RecordStorage(new BlockStorage(
				dbStream, blockSize
			));
		}

		~DiskDatabase()
		{
			base.Dispose(false);
		}

		/// <summary>
		/// Disposes this database instance.
		/// </summary>
		public void Dispose()
		{
			base.Dispose(true);
		}

		/// <summary>
		/// Returns a new IO stream with specified params.
		/// </summary>
		protected override Stream GetNewIndexStream (string label)
		{
			return new FileStream(
				GetIndexPath(label),
				FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
				4096
			);
		}

		/// <summary>
		/// Returns a new node manager instance with specified parameters.
		/// </summary>
		protected override ITreeNodeManager<K, uint> GetNewNodeManager<K> (ISerializer<K> keySerializer,
			UintSerializer valueSerializer, IRecordStorage recordStorage, ushort minEntriesPerNode)
		{
			return new DiskTreeNodeManager<K, uint>(
				keySerializer,
				valueSerializer,
				recordStorage,
				new DiskNodeOptions(500, 200, minEntriesPerNode)
			);
		}

		/// <summary>
		/// Returns the path to the main db file.
		/// </summary>
		private string GetMainDbPath()
		{
			return Path.Combine(dbDirectory, name + ".rdb");
		}

		/// <summary>
		/// Returns the path to the index file.
		/// </summary>
		private string GetIndexPath(string label)
		{
			return Path.Combine(
				dbDirectory,
				string.Format("{0}.{1}.rdb", name, label)
			);
		}
	}
}

