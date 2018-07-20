using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Variant of IDatabase for providing the base foundation for any other IDatabase implementations.
	/// This class isn't used directly, but through other Database classes deriving from this.
	/// </summary>
	public abstract class BaseDatabase<T> : IDatabase<T> where T : class, IModel<T> {
		
		/// <summary>
		/// The serializer of the model.
		/// </summary>
		public ISerializer<T> ModelSerializer;

		/// <summary>
		/// The record storage.
		/// </summary>
		public RecordStorage RecStorage;

		/// <summary>
		/// The database index manager.
		/// </summary>
		public IndexUtility<T> IndexUtils;

		/// <summary>
		/// Backing field of Name property.
		/// </summary>
		protected readonly string name;

		/// <summary>
		/// The main IO stream to database.
		/// </summary>
		protected Stream dbStream;

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
		/// Returns the object that manages database index.
		/// </summary>
		public IIndexUtility Indexes {
			get { return IndexUtils; }
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

			this.IndexUtils = new IndexUtility<T>(this);
			this.isDisposed = false;
		}

		~BaseDatabase()
		{
			Dispose(false);
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
				if(IndexUtils.UniqueIdExists(value.Id))
					throw new Exception("An entry with _id ("+value.Id.ToString()+") already exists.");
				
				// Insert a new data to record storage.
				uint newId = RecStorage.Create(ModelSerializer.Serialize(value));

				// Add unique key
				IndexUtils.InsertUnique(value.Id, newId);

				// Iterate through all fields in the model instance.
				IndexUtils.IterateIndexes(value, delegate(IIndex indexTree, string field) {
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
				if(!IndexUtils.UniqueIdExists(value.Id, out index))
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
				IndexUtils.IterateIndexes(value, delegate(IIndex indexTree, string field) {
					
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
			if(IndexUtils.UniqueIdExists(id, out index)) {
				var data = RecStorage.Find(index);
				if(data == null)
					return false;

				// Get model
				var model = ModelSerializer.Deserialize(data, 0, data.Length);

				// Delete from main db
				RecStorage.Delete(index);

				// Delete from index trees
				IndexUtils.IterateIndexes(model, delegate(IIndex indexTree, string field) {
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
			if(IndexUtils.UniqueIdExists(id, out index)) {
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
		public virtual IDatabaseQuery<T> Query(int estimatedCount = 0)
		{
			if(isDisposed)
				throw new ObjectDisposedException("IDatabase");
			
			return new DatabaseQuery<T>(this, estimatedCount);
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
		/// Deletes the stream source with specified label.
		/// </summary>
		public abstract void DeleteIndex(string label);

		/// <summary>
		/// Returns whether a stream file with specified label exists.
		/// </summary>
		public abstract bool StreamExists(string label);

		/// <summary>
		/// Renames stream source file.
		/// </summary>
		public abstract void RenameStreamFile(string oldLabel, string newLabel);

		/// <summary>
		/// Returns a new IO stream with specified params.
		/// </summary>
		public abstract Stream GetNewIndexStream(string label);

		/// <summary>
		/// Returns a new node manager instance with specified parameters.
		/// </summary>
		public abstract ITreeNodeManager<K, uint> GetNewNodeManager<K>(ISerializer<K> keySerializer,
			UintSerializer valueSerializer, IRecordStorage recordStorage, ushort minEntriesPerNode);
		
		protected virtual void Dispose(bool disposing)
		{
			if(disposing && !isDisposed) {
				isDisposed = true;

				dbStream.Dispose();
				IndexUtils.Dispose();
			}
		}
	}
}

