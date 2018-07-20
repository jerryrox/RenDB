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

			IndexUtils.CreateUniqueIndex();
		}

		~DiskDatabase()
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
		/// Deletes the stream source with specified values.
		/// </summary>
		public override void DeleteIndex(string label)
		{
			string filePath = GetIndexPath(label);
			if(File.Exists(filePath))
				File.Delete(filePath);
		}

		/// <summary>
		/// Returns whether a stream file with specified label exists.
		/// </summary>
		public override bool StreamExists(string label)
		{
			return File.Exists(GetIndexPath(label));
		}

		/// <summary>
		/// Renames stream source file.
		/// </summary>
		public override void RenameStreamFile(string oldLabel, string newLabel)
		{
			string oldPath = GetIndexPath(oldLabel);
			string newPath = GetIndexPath(newLabel);
			File.Move(oldPath, newPath);
		}

		/// <summary>
		/// Returns a new IO stream with specified params.
		/// </summary>
		public override Stream GetNewIndexStream (string label)
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
		public override ITreeNodeManager<K, uint> GetNewNodeManager<K> (ISerializer<K> keySerializer,
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

