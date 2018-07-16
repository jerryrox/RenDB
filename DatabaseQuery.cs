using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Renko.Matching;
using RenDBCore.Internal;

namespace RenDBCore
{
	/// <summary>
	/// Class that returns data from databases using queries.
	/// </summary>
	public class DatabaseQuery<T> where T : class, IModel<T> {

		private BaseDatabase<T> database;
		private List<uint> indexes;
		private List<uint> tempIndexes;
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
			this.indexes = new List<uint>(estimatedCount);
			this.tempIndexes = new List<uint>(estimatedCount);
			this.isAscending = false;
			this.isInitiallyIncluded = false;
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
		/// Finds all entries by unique index.
		/// </summary>
		public DatabaseQuery<T> FindAll()
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;
			
			// Enumerate through all unique keys
			foreach(var entry in database.UniqueIndex.GetAll(isAscending)) {
				// Add indexes to temp list.
				tempIndexes.Add(entry.Item2);
			}

			// Intersect indexes
			IntersectIndexes();
			return this;
		}

		/// <summary>
		/// Finds all entries by specified field.
		/// </summary>
		public DatabaseQuery<T> FindAll<K>(string field)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;
			
			// Find the index tree
			IndexTree<K, uint> index = null;
			if(database.NormalIndexes.ContainsKey(field)) {
				// Get index.
				index = database.NormalIndexes[field] as IndexTree<K, uint>;
				if(index != null) {
					// Enumerate through all keys
					foreach(var entry in index.GetAll(isAscending)) {
						// Add indexes to temp list.
						tempIndexes.Add(entry.Item2);
					}

					// Intersect indexes
					IntersectIndexes();
				}
			}

			return this;
		}

		/// <summary>
		/// Finds entries with exact field data.
		/// </summary>
		public DatabaseQuery<T> FindExact<K>(string field, K key)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;
			
			// Check if an index tree exists for field.
			if(database.NormalIndexes.ContainsKey(field)) {
				
				// Get the index as specific type.
				var tree = database.NormalIndexes[field] as IndexTree<K, uint>;
				if(tree != null) {

					// Get comparer
					var comparer = Comparer<K>.Default;

					// Enumerate through matched entries
					foreach(var entry in tree.GetExactMatch(key, isAscending)) {
						
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
			}
			return this;
		}

		/// <summary>
		/// Finds entries with matching field data.
		/// </summary>
		public DatabaseQuery<T> FindMatch<K>(string field, IMatcher<K> matcher)
		{
			// Ignore find if required.
			if(ShouldIgnore)
				return this;
			
			// Check if an index tree exists for field.
			if(database.NormalIndexes.ContainsKey(field)) {

				// Get the index as specific type.
				var tree = database.NormalIndexes[field] as IndexTree<K, uint>;
				if(tree != null) {

					// Enumerate through matched entries
					foreach(var entry in tree.GetOptionMatch(matcher, isAscending)) {
						// Add indexes to temp list.
						tempIndexes.Add(entry.Item2);
					}

					// Intersect indexes
					IntersectIndexes();
				}
			}
			return this;
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
		/// Returns all entries found.
		/// </summary>
		public IEnumerator<T> GetAll()
		{
			for(int i=0; i<indexes.Count; i++) {
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
					if(!tempIndexes.Contains(indexes[i]))
						indexes.RemoveAt(i);
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

