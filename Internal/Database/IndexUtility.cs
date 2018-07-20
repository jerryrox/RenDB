using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using RenDBCore;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Submodule of a database object that manages db indexes.
	/// </summary>
	public class IndexUtility<T> : IIndexUtility, IDisposable where T : class, IModel<T> {
		
		/// <summary>
		/// Dictionary of all indexed unique fields.
		/// </summary>
		public IndexTree<Guid, uint> UniqueIndex;

		/// <summary>
		/// Dictionary of all indexed non-unique fields.
		/// </summary>
		private Dictionary<string, IIndex> normalIndexes;

		/// <summary>
		/// The unique index tree IO stream.
		/// </summary>
		private Stream uniqueIndexStream;

		/// <summary>
		/// Dictionary of IO streams associated with index (field) names.
		/// </summary>
		private Dictionary<string, Stream> indexStreams;

		/// <summary>
		/// The database instance that owns this object.
		/// </summary>
		private BaseDatabase<T> database;

		/// <summary>
		/// Whether this object is disposed.
		/// </summary>
		private bool isDisposed;


		public IndexUtility(BaseDatabase<T> database)
		{
			this.database = database;

			this.indexStreams = new Dictionary<string, Stream>();
			this.normalIndexes = new Dictionary<string, IIndex>();
		}

		~IndexUtility()
		{
			Dispose(false);
		}

		/// <summary>
		/// Registers a new index tree with sepcified params.
		/// </summary>
		public void Register<K> (string label, string field, ISerializer<K> keySerializer)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");

			Register(label, field, keySerializer, 4096, 128);
		}

		/// <summary>
		/// Registers a new index tree with specified params.
		/// </summary>
		public void Register<K> (string label, string field, ISerializer<K> keySerializer, int blockSize, ushort minEntriesPerNode)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			
			var indexes = normalIndexes;

			// Create index tree if not exists.
			if(!indexes.ContainsKey(field)) {
				var index = CreateIndex(label, field, keySerializer, blockSize, minEntriesPerNode);
				indexes.Add(field, index);
			}
		}

		/// <summary>
		/// Rebuilds the registered index tree from scratch with specified field and label.
		/// </summary>
		public void HardRebuild<K>(string label, string field, ISerializer<K> keySerializer)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			if(!normalIndexes.ContainsKey(field))
				throw new ArgumentException("The key ("+field+") doesn't exist.");
			
			DeleteIndex(label, field);
			Register<K>(label, field, keySerializer);
			FillMissingIndex<K>(field);
		}

		/// <summary>
		/// Rebuilds the registered index tree with specified field and label.
		/// </summary>
		public void SoftRebuild<K>(string field)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			if(!normalIndexes.ContainsKey(field))
				throw new ArgumentException("The key ("+field+") doesn't exist.");

			FillMissingIndex<K>(field);
		}

		/// <summary>
		/// Renames the registered index tree's label with specified values.
		/// </summary>
		public void RenameLabel<K>(string field, string oldLabel, string newLabel, ISerializer<K> keySerializer)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			if(!normalIndexes.ContainsKey(field))
				throw new ArgumentException("The key ("+field+") doesn't exist.");

			// If stream with old label exists and stream with new label doesn't exist, we are eligible to continue.
			if(database.StreamExists(oldLabel) && !database.StreamExists(newLabel)) {
				// Dispose current stream
				indexStreams[field].Dispose();
				// Remove KeyValue associated with field from dictionary
				normalIndexes.Remove(field);
				indexStreams.Remove(field);
				// Rename stream source file
				database.RenameStreamFile(oldLabel, newLabel);
				// Register
				Register(newLabel, field, keySerializer);
			}
		}

		/// <summary>
		/// Returns whether an index with specified field name exists.
		/// </summary>
		public bool ContainsField(string field)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			return normalIndexes.ContainsKey(field);
		}

		/// <summary>
		/// Returns whether index that uses the specified label exists.
		/// </summary>
		public bool ContainsLabel(string label)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			return database.StreamExists(label);
		}

		/// <summary>
		/// Deletes the index with specified values.
		/// </summary>
		public void Delete(string label, string field)
		{
			if(database.IsDisposed)
				throw new ObjectDisposedException("IndexUtility");
			if(!normalIndexes.ContainsKey(field))
				throw new ArgumentException("The key ("+field+") doesn't exist.");

			DeleteIndex(label, field);
		}

		/// <summary>
		/// Inserts the specified unique key to index tree.
		/// </summary>
		public void InsertUnique(Guid key, uint index)
		{
			UniqueIndex.Insert(key, index);
		}

		/// <summary>
		/// Finds and returns a normal index tree with specified field.
		/// May return null.
		/// </summary>
		public IndexTree<K, uint> GetNormalIndex<K>(string field)
		{
			if(normalIndexes.ContainsKey(field))
				return normalIndexes[field] as IndexTree<K, uint>;
			return null;
		}

		/// <summary>
		/// Convenience function for calling UniqueIdExists without out param.
		/// </summary>
		public bool UniqueIdExists(Guid id)
		{
			uint index;
			return UniqueIdExists(id, out index);
		}

		/// <summary>
		/// Returns whether specified Guid exists in unique index tree.
		/// Outputs the index associated with the id, if exists.
		/// </summary>
		public bool UniqueIdExists(Guid id, out uint index)
		{
			var entry = UniqueIndex.Get(id);
			if(entry != null) {
				index = entry.Item2;
				return true;
			}
			index = 0;
			return false;
		}

		/// <summary>
		/// Iterates through all registered index trees.
		/// Returns whether all indexHandler invocation returned true.
		/// </summary>
		public bool IterateIndexes(T value, Func<IIndex, string, bool> indexHandler)
		{
			var fields = value.GetAllFields();
			while(fields.MoveNext()) {
				string field = fields.Current;

				// If this field is being indexed
				if(normalIndexes.ContainsKey(field)) {
					if(!indexHandler(normalIndexes[field], field))
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Creates a new unique index tree for Guid.
		/// </summary>
		public void CreateUniqueIndex()
		{
			uniqueIndexStream = database.GetNewIndexStream("_id");
			UniqueIndex = new IndexTree<Guid, uint>(
				database.GetNewNodeManager(
					new GuidSerializer(),
					new UintSerializer(),
					new RecordStorage(new BlockStorage(uniqueIndexStream)),
					256
				)
			);
		}

		/// <summary>
		/// Disposes all streams used in this object.
		/// </summary>
		public void Dispose() 
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if(disposing && !isDisposed) {
				isDisposed = true;

				uniqueIndexStream.Dispose();
				foreach(var stream in indexStreams.Values) {
					stream.Dispose();
				}
			}
		}

		/// <summary>
		/// Completely removes an index's tree and dicitonary item.
		/// </summary>
		void DeleteIndex(string label, string field)
		{
			// Remove the existing index tree.
			normalIndexes.Remove(field);

			// Get the stream used by index.
			var stream = indexStreams[field];

			// Delete stream
			stream.Dispose();
			database.DeleteIndex(label);
			indexStreams.Remove(field);
		}

		/// <summary>
		/// Rebuilds the index tree by adding missing entries' index
		/// </summary>
		void FillMissingIndex<K>(string field)
		{
			// Get the index tree with given field
			var targetIndex = normalIndexes[field] as IndexTree<K, uint>;
			if(targetIndex == null)
				return;

			// Setup finder
			var finder = database.Query() as DatabaseQuery<T>;
			// Get all entries in the database
			finder.Sort(true).Find().GetAll(
				delegate(uint index, T curEntry) {
					
					// Get identifier values
					K data = (K)curEntry.GetFieldData(field);
					Guid id = curEntry.Id;

					// If there is no existing index entry
					if(!ExistsInIndex<K>(field, data, id)) {
						// Insert entry to index tree.
						targetIndex.Insert(data, index);
					}
				}
			);
		}

		/// <summary>
		/// Returns whether an index entry exists that matches the specified values.
		/// </summary>
		bool ExistsInIndex<K>(string field, K data, Guid id)
		{
			// Find index entries
			var results = database.Query().Sort(true).Find(field, data).GetAll();
			while(results.MoveNext()) {
				var curEntry = results.Current;
				if(curEntry.Id == id)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Creates a new index tree with specified params.
		/// </summary>
		IndexTree<K, uint> CreateIndex<K>(string label, string field, ISerializer<K> keySerializer,
			int blockSize, ushort minEntriesPerNode)
		{
			var indexStream = database.GetNewIndexStream(label);
			indexStreams.Add(field, indexStream);

			// Create a new index tree
			var newManager = database.GetNewNodeManager(
				keySerializer,
				new UintSerializer(),
				new RecordStorage(new BlockStorage(
					indexStream,
					blockSize
				)),
				minEntriesPerNode
			);
			return new IndexTree<K, uint>(newManager, true);
		}
	}
}

