using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RenDBCore;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Class that returns data from databases using queries.
	/// </summary>
	public class DatabaseQuery<T> : IDatabaseQuery<T> where T : class, IModel<T> {

		private BaseDatabase<T> database;
		private IndexUtility<T> indexUtils;
		private List<uint> indexes;
		private List<uint> tempIndexes;
		private Dictionary<uint, T> cachedData;
		private string sortField;
		private bool isAscending;

		private bool isInitiallyIncluded;


		/// <summary>
		/// Returns whether all further find requests should be ignored.
		/// This can happen if user queried more than once and there are no indexes found
		/// in the list.
		/// In this case, we should skip further find requests for performance.
		/// </summary>
		private bool ShouldIgnore {
			get { return isInitiallyIncluded && indexes.Count == 0; }
		}


		public DatabaseQuery(BaseDatabase<T> database, int estimatedCount = 0)
		{
			this.database = database;
			this.indexUtils = database.IndexUtils;
			this.indexes = new List<uint>(estimatedCount);
			this.tempIndexes = new List<uint>(estimatedCount);
			this.cachedData = new Dictionary<uint, T>(estimatedCount / 2);
			this.isAscending = true;
			this.isInitiallyIncluded = false;
		}

		/// <summary>
		/// Resets query state to initial state.
		/// </summary>
		public IDatabaseQuery<T> Reset()
		{
			indexes.Clear();
			tempIndexes.Clear();
			cachedData.Clear();
			isAscending = true;
			isInitiallyIncluded = false;
			return this;
		}

		/// <summary>
		/// Resets query state to initial state.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Reset()
		{
			return Reset();
		}

		/// <summary>
		/// Sets sorting flag for specified direction.
		/// </summary>
		public DatabaseQuery<T> Sort(bool isAscending)
		{
			this.isAscending = isAscending;
			return this;
		}

		/// <summary>
		/// Sets sorting flag for specified direction.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Sort(bool isAscending)
		{
			return Sort(isAscending);
		}

		/// <summary>
		/// Finds all entries by unique index.
		/// </summary>
		public DatabaseQuery<T> Find()
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;
			
			// Enumerate through all unique keys
			foreach(var entry in indexUtils.UniqueIndex.GetAll(isAscending)) {
				// Add indexes to temp list.
				tempIndexes.Add(entry.Item2);
			}

			// Intersect indexes
			IntersectIndexes();
			return this;
		}

		/// <summary>
		/// Finds all entries by unique index.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Find()
		{
			return Find();
		}

		/// <summary>
		/// Finds unindexed entries with condition-checker function.
		/// Always search for indexed fields for performance!
		/// </summary>
		public DatabaseQuery<T> Find(Func<T, bool> condition)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;

			// If was not initially searched once, include all entries for search.
			if(!isInitiallyIncluded)
				Find();

			// For each index
			for(int i=indexes.Count-1; i>=0; i--) {
				// Get data for cur index.
				uint curIndex = indexes[i];
				var data = database.RecStorage.Find(curIndex);
				if(data == null)
					continue;

				// Deserialize model at cur index.
				T model = database.ModelSerializer.Deserialize(data, 0, data.Length);

				// If matches the given condition
				if(condition(model)) {
					cachedData.Add(curIndex, model);
				}
				// If doesn't match the condition
				else {
					indexes.RemoveAt(i);
				}
			}

			return this;
		}

		/// <summary>
		/// Finds unindexed entries with exact field data.
		/// Always search for indexed fields for performance!
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Find(Func<T, bool> condition)
		{
			return Find(condition);
		}

		/// <summary>
		/// Finds all entries by specified field.
		/// </summary>
		public DatabaseQuery<T> Find<K>(string field)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;
			
			// Find the index tree
			IndexTree<K, uint> index = indexUtils.GetNormalIndex<K>(field);
			if(index != null) {
				// Enumerate through all keys
				foreach(var entry in index.GetAll(isAscending)) {
					// Add indexes to temp list.
					tempIndexes.Add(entry.Item2);
				}

				// Intersect indexes
				IntersectIndexes();
			}

			return this;
		}

		/// <summary>
		/// Finds all entries by specified field.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Find<K>(string field)
		{
			return Find<K>(field);
		}

		/// <summary>
		/// Finds entries with exact field data.
		/// </summary>
		public DatabaseQuery<T> Find<K>(string field, K key)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;

			// Find the index tree
			IndexTree<K, uint> index = indexUtils.GetNormalIndex<K>(field);
			if(index != null) {
				// Get comparer
				var comparer = Comparer<K>.Default;

				// Enumerate through matched entries
				foreach(var entry in index.GetExactMatch(key, isAscending)) {

					// If not matching key, finish immediately
					int compare = comparer.Compare(entry.Item1, key);
					if(compare != 0)
						break;

					// Add indexes to temp list.
					tempIndexes.Add(entry.Item2);
				}

				// Intersect indexes
				IntersectIndexes();
			}

			return this;
		}

		/// <summary>
		/// Finds entries with exact field data.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Find<K>(string field, K key)
		{
			return Find<K>(field, key);
		}

		/// <summary>
		/// Finds entries with condition-checker function.
		/// </summary>
		public DatabaseQuery<T> Find<K>(string field, Func<K, bool> condition)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;

			// Find the index tree
			var index = indexUtils.GetNormalIndex<K>(field);
			if(index != null) {
				// Enumerate through matched entries
				foreach(var entry in index.GetAll(isAscending)) {
					// If this entry passes the specified condition, add index to temp list.
					if(condition(entry.Item1))
						tempIndexes.Add(entry.Item2);
				}

				// Intersect indexes
				IntersectIndexes();
			}
			return this;
		}

		/// <summary>
		/// Finds entries with condition-checker function.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Find<K>(string field, Func<K, bool> condition)
		{
			return Find<K>(field, condition);
		}

		/// <summary>
		/// Skips entries by specified amount.
		/// </summary>
		public DatabaseQuery<T> Skip(int count)
		{
			if(count < indexes.Count)
				indexes.RemoveRange(0, count);
			else
				indexes.Clear();
			return this;
		}

		/// <summary>
		/// Skips entries by specified amount.
		/// </summary>
		IDatabaseQuery<T> IDatabaseQuery<T>.Skip(int count)
		{
			return Skip(count);
		}

		/// <summary>
		/// Returns all entries found using an iterator delegate.
		/// </summary>
		public void GetAll(Action<uint, T> iterator)
		{
			for(int i=0; i<indexes.Count; i++) {
				var data = database.RecStorage.Find(indexes[i]);
				if(data == null)
					continue;
				iterator(
					indexes[i],
					database.ModelSerializer.Deserialize(data, 0, data.Length)
				);
			}
		}

		/// <summary>
		/// Returns all entries found.
		/// </summary>
		public IEnumerator<T> GetAll()
		{
			for(int i=0; i<indexes.Count; i++) {
				// If there is a cached entry, return that.
				T cache = null;
				if(cachedData.TryGetValue(indexes[i], out cache)) {
					yield return cache;
					continue;
				}

				var data = database.RecStorage.Find(indexes[i]);
				if(data == null)
					continue;
				yield return database.ModelSerializer.Deserialize(data, 0, data.Length);
			}
		}

		/// <summary>
		/// Returns all entries found clamped to specified count.
		/// </summary>
		public IEnumerator<T> GetRange(int count)
		{
			int loop = Math.Min(count, indexes.Count);
			for(int i=0; i<loop; i++) {
				// If there is a cached entry, return that.
				T cache = null;
				if(cachedData.TryGetValue(indexes[i], out cache)) {
					yield return cache;
					continue;
				}
				
				var data = database.RecStorage.Find(indexes[i]);
				if(data == null)
					continue;
				yield return database.ModelSerializer.Deserialize(data, 0, data.Length);
			}
		}

		/// <summary>
		/// Returns the first found entry.
		/// </summary>
		public T GetFirst()
		{
			for(int i=0; i<indexes.Count; i++) {
				// If there is a cached entry, return that.
				T cache = null;
				if(cachedData.TryGetValue(indexes[i], out cache))
					return cache;
				
				var data = database.RecStorage.Find(indexes[i]);
				if(data == null)
					continue;
				return database.ModelSerializer.Deserialize(data, 0, data.Length);
			}
			return null;
		}

		/// <summary>
		/// Returns the total indexes found.
		/// May not always be valid if somehow the database is damaged.
		/// </summary>
		public int GetCount()
		{
			return indexes.Count;
		}

		/// <summary>
		/// Intersects index and tempIndex lists.
		/// </summary>
		private void IntersectIndexes()
		{
			if(isInitiallyIncluded) {
				for(int i=indexes.Count-1; i>=0; i--) {
					if(!tempIndexes.Contains(indexes[i])) {
						cachedData.Remove(indexes[i]);
						indexes.RemoveAt(i);
					}
				}
			}
			else {
				indexes.AddRange(tempIndexes);
			}

			isInitiallyIncluded = true;
			tempIndexes.Clear();
		}
	}
}

