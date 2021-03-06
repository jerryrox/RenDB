﻿using System;
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

		/// <summary>
		/// A list of labels just for tracking purposes.
		/// </summary>
		private List<string> labels;


		public MemoryDatabase(string name, ISerializer<T> modelSerializer, int blockSize)
			: base(name, modelSerializer)
		{
			dbStream = new MemoryStream();
			RecStorage = new RecordStorage(new BlockStorage(
				dbStream, blockSize
			));
			labels = new List<string>();

			IndexUtils.CreateUniqueIndex();
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
		/// Deletes the stream source with specified values.
		/// </summary>
		public override void DeleteIndex(string label)
		{
			labels.Remove(label);
		}

		/// <summary>
		/// Returns whether a stream file with specified label exists.
		/// </summary>
		public override bool StreamExists(string label)
		{
			return labels.Contains(label);
		}

		/// <summary>
		/// Renames stream source file.
		/// </summary>
		public override void RenameStreamFile(string oldLabel, string newLabel)
		{
			labels.Remove(oldLabel);
			labels.Add(newLabel);
		}

		/// <summary>
		/// Returns a new IO stream with specified params.
		/// </summary>
		public override Stream GetNewIndexStream (string label)
		{
			labels.Add(label);
			return new MemoryStream();
		}

		/// <summary>
		/// Returns a new node manager instance with specified parameters.
		/// </summary>
		public override ITreeNodeManager<K, uint> GetNewNodeManager<K> (
			ISerializer<K> keySerializer,
			UintSerializer valueSerializer,
			IRecordStorage recordStorage,
			ushort minEntriesPerNode)
		{
			return new MemoryTreeNodeManager<K, uint>(minEntriesPerNode, Comparer<K>.Default);
		}
	}
}

