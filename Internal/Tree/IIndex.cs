using System;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a class that implements indexing functionality.
	/// </summary>
	public interface IIndex<K, V> {

		/// <summary>
		/// Creates a new entry with specified key and value.
		/// </summary>
		void Insert(K key, V value);

		/// <summary>
		/// Deletes an entry with matching KeyValue and optionally a comparer.
		/// Used with non-unique keys.
		/// </summary>
		bool Delete(K key, V value, IComparer<V> valueComparer = null);

		/// <summary>
		/// Deletes all entries with matching key.
		/// Used with unique keys.
		/// </summary>
		bool Delete(K key);

		/// <summary>
		/// Finds and returns an entry with matching key.
		/// </summary>
		Tuple<K, V> Get(K key);

		/// <summary>
		/// Finds all entries.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetAll(bool ascending);

		/// <summary>
		/// Finds all entries with the key larger than or equal to specified.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetLargerOrEqual(K key);

		/// <summary>
		/// Finds all entries with the key larger than specified.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetLarger(K key);

		/// <summary>
		/// Finds all entries with the key lesser than or equal to specified.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetLesserOrEqual(K key);

		/// <summary>
		/// Finds all entries with the key lesser than specified.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetLesser(K key);
	}
}

