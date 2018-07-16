using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Renko.Matching;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Variant of IDatabase for providing the base foundation for any other IDatabase implementations.
	/// This class isn't used directly, but through other Database classes deriving from this.
	/// </summary>
	public abstract class BaseDatabase<T> : IDatabase<T> where T : class, IModel<T> {
		
		/// <summary>
		/// Dictionary of all indexed unique fields.
		/// </summary>
		public IndexTree<Guid, uint> UniqueIndex;

		/// <summary>
		/// Dictionary of all indexed non-unique fields.
		/// </summary>
		public Dictionary<string, IIndex> NormalIndexes;

		/// <summary>
		/// The serializer of the model.
		/// </summary>
		public ISerializer<T> ModelSerializer;

		/// <summary>
		/// The record storage.
		/// </summary>
		public RecordStorage RecStorage;

		/// <summary>
		/// Backing field of Name property.
		/// </summary>
		protected readonly string name;

		/// <summary>
		/// The main IO stream to database.
		/// </summary>
		protected Stream dbStream;

		/// <summary>
		/// The unique index tree IO stream.
		/// </summary>
		protected Stream uniqueIndexStream;

		/// <summary>
		/// Dictionary of IO streams associated with index (field) names.
		/// </summary>
		protected Dictionary<string, Stream> indexStreams;

		/// <summary>
		/// Whether this database instance is disposed.
		/// </summary>
		protected bool isDisposed;


		/// <summary>
		/// Returns the name of this database.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Returns whether this database instance is disposed.
		/// </summary>
		public virtual bool IsDisposed {
			get { return isDisposed; }
		}


		public BaseDatabase(string name, ISerializer<T> modelSerializer)
		{
			this.name = name;
			this.ModelSerializer = modelSerializer;

			this.indexStreams = new Dictionary<string, Stream>();
			this.NormalIndexes = new Dictionary<string, IIndex>();
			this.isDisposed = false;
		}

		~BaseDatabase()
		{
			Dispose(false);
		}

		/// <summary>
		/// Registers a new index tree to database with specified params.
		/// </summary>
		public virtual void RegisterIndex<K>(string label, string field, ISerializer<K> keySerializer)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			RegisterIndex(label, field, keySerializer, 4096, 128);
		}

		/// <summary>
		/// Registers a new index tree to database with specified params.
		/// </summary>
		public virtual void RegisterIndex<K>(string label, string field, ISerializer<K> keySerializer,
			int blockSize, ushort minEntriesPerNode)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			// Decide which collection to use
			var indexes = NormalIndexes;

			// Create the index tree if not exists.
			if(!indexes.ContainsKey(field)) {
				var index = CreateIndex(label, field, keySerializer, blockSize, minEntriesPerNode);
				indexes.Add(field, index);
			}
		}

		/// <summary>
		/// Inserts the specified model instance.
		/// </summary>
		public virtual bool Insert(T value)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			try {
				// If unique key already exists, throw error
				if(UniqueIdExists(value.Id))
					throw new Exception("An entry with _id ("+value.Id.ToString()+") already exists.");
				
				// Insert a new data to record storage.
				uint newId = RecStorage.Create(ModelSerializer.Serialize(value));

				// Add unique key
				UniqueIndex.Insert(value.Id, newId);

				// Iterate through all fields in the model instance.
				IterateIndexes(value, delegate(IIndex indexTree, string field) {
					indexTree.Insert(value.GetFieldData(field), newId);
					return true;
				});
				return true;
			}
			catch(Exception e) {
				// Need to do something with thrown exceptions...
				return false;
			}
		}

		/// <summary>
		/// Updates the specified model instance.
		/// </summary>
		public virtual bool Update(T value)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			try {
				// If unique key doesn't exist, throw error
				uint index = 0;
				if(!UniqueIdExists(value.Id, out index))
					throw new Exception("No entry with _id ("+value.Id+") was found.");

				// Get the original data.
				var origData = RecStorage.Find(index);
				if(origData == null)
					throw new Exception("Failed to update while retrieving original data.");

				// Deserialize data to model.
				var origModel = ModelSerializer.Deserialize(origData, 0, origData.Length);

				// Update the main DB.
				RecStorage.Update(index, ModelSerializer.Serialize(value));

				// Delete any indexes associated with the original model instance and reinsert them with new data.
				IterateIndexes(value, delegate(IIndex indexTree, string field) {
					
					// If the value did change
					var origFieldValue = origModel.GetFieldData(field);
					var newFieldValue = value.GetFieldData(field);
					if(origFieldValue != newFieldValue) {
						
						// Delete existing index and reinsert the new one.
						if(indexTree.Delete(origFieldValue, index))
							indexTree.Insert(newFieldValue, index);
					}
					return true;
				});
				return true;
			}
			catch(Exception e) {
				// Need to do something with thrown exceptions...
				return false;
			}
		}

		/// <summary>
		/// Deletes a single entry with specified Guid.
		/// </summary>
		public virtual bool Delete(Guid id)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			// If id exists
			uint index = 0;
			if(UniqueIdExists(id, out index)) {
				var data = RecStorage.Find(index);
				if(data == null)
					return false;

				// Get model
				var model = ModelSerializer.Deserialize(data, 0, data.Length);

				// Delete from main db
				RecStorage.Delete(index);

				// Delete from index trees
				IterateIndexes(model, delegate(IIndex indexTree, string field) {
					indexTree.Delete(model.GetFieldData(field), index);
					return true;
				});
				return true;
			}

			// Return false by default.
			return false;
		}

		/// <summary>
		/// Finds a single entry with matching Guid.
		/// </summary>
		public virtual T Find(Guid id)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			// If id exists
			uint index = 0;
			if(UniqueIdExists(id, out index)) {
				var data = RecStorage.Find(index);
				if(data == null)
					return null;
				return ModelSerializer.Deserialize(data, 0, data.Length);
			}

			// Return null by default.
			return null;
		}

		/// <summary>
		/// Finds using returned query object.
		/// Assign estimatedCount to set internal lists' capacity value.
		/// </summary>
		public virtual DatabaseQuery<T> Find(int estimatedCount = 0)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			return new DatabaseQuery<T>(this, estimatedCount);
		}

		/// <summary>
		/// Finds and returns the index tree instance associated with specified field.
		/// </summary>
		public IIndex<K, uint> GetIndexTree<K>(string field)
		{
			if(NormalIndexes.ContainsKey(field))
				return NormalIndexes[field] as IIndex<K, uint>;
			return null;
		}

		/// <summary>
		/// Disposes this database instance.
		/// </summary>
		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Iterates through all registered index trees.
		/// Returns whether all indexHandler invocation returned true.
		/// </summary>
		protected bool IterateIndexes(T value, Func<IIndex, string, bool> indexHandler)
		{
			var fields = value.GetAllFields();
			while(fields.MoveNext()) {
				string field = fields.Current;

				// If this field is being indexed
				if(NormalIndexes.ContainsKey(field)) {
					if(!indexHandler(
						NormalIndexes[field],
						field)) {

						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Convenience function for calling UniqueIdExists without out param.
		/// </summary>
		protected bool UniqueIdExists(Guid id)
		{
			uint index;
			return UniqueIdExists(id, out index);
		}

		/// <summary>
		/// Returns whether specified Guid exists in unique index tree.
		/// Outputs the index associated with the id, if exists.
		/// </summary>
		protected bool UniqueIdExists(Guid id, out uint index)
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
		/// Creates a new unique index tree for Guid.
		/// </summary>
		protected void CreateUniqueIndex()
		{
			uniqueIndexStream = GetNewIndexStream("_id");
			UniqueIndex = new IndexTree<Guid, uint>(
				GetNewNodeManager(
					new GuidSerializer(),
					new UintSerializer(),
					new RecordStorage(new BlockStorage(uniqueIndexStream)),
					256
				)
			);
		}

		/// <summary>
		/// Creates a new index tree with specified params.
		/// </summary>
		protected IndexTree<K, uint> CreateIndex<K>(string label, string field,
			ISerializer<K> keySerializer, int blockSize, ushort minEntriesPerNode)
		{
			var indexStream = GetNewIndexStream(label);
			indexStreams.Add(field, indexStream);

			// Create a new index tree
			var newManager = GetNewNodeManager(
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

		/// <summary>
		/// Returns a new IO stream with specified params.
		/// </summary>
		protected abstract Stream GetNewIndexStream(string label);

		/// <summary>
		/// Returns a new node manager instance with specified parameters.
		/// </summary>
		protected abstract ITreeNodeManager<K, uint> GetNewNodeManager<K>(ISerializer<K> keySerializer,
			UintSerializer valueSerializer, IRecordStorage recordStorage, ushort minEntriesPerNode);

		protected virtual void Dispose(bool disposing)
		{
			if(disposing && !isDisposed) {
				isDisposed = true;

				dbStream.Dispose();
				uniqueIndexStream.Dispose();
				foreach(var stream in indexStreams.Values)
					stream.Dispose();
			}
		}
	}
}

