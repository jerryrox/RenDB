using System;
using System.Collections;
using System.Collections.Generic;
using Renko.Matching;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a class that implements indexing functionality.
	/// </summary>
	public interface IIndex {

		/// <summary>
		/// Creates a new entry with specified key and value.
		/// </summary>
		void Insert(object key, object value);

		/// <summary>
		/// Deletes an entry with matching KeyValue and optionally a comparer.
		/// Used with non-unique keys.
		/// </summary>
		bool Delete(object key, object value, IComparer valueComparer = null);

		/// <summary>
		/// Deletes all entries with matching key.
		/// Used with unique keys.
		/// </summary>
		bool Delete(object key);

		/// <summary>
		/// Finds and returns an entry with matching key.
		/// </summary>
		object Get(object key);

		/// <summary>
		/// Finds all entries.
		/// </summary>
		IEnumerable GetAllNonGeneric(bool ascending); // Explicitly named due to compile errors.

		/// <summary>
		/// Finds all entries with keys matching specified matcher.
		/// </summary>
		IEnumerable GetOptionMatch(IMatcher matcher, bool ascending);

		/// <summary>
		/// Finds all entries with the keys matching or (larger if ascending, lesser if descending) to specified key.
		/// </summary>
		IEnumerable GetExactMatch(object key, bool ascending);
	}

	/// <summary>
	/// Generic interface of a class that implements indexing functionality.
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
		/// Finds all entries with keys matching specified matcher.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetOptionMatch(IMatcher<K> matcher, bool ascending);

		/// <summary>
		/// Finds all entries with the keys matching or (larger if ascending, lesser if descending) to specified key.
		/// </summary>
		IEnumerable<Tuple<K, V>> GetExactMatch(K key, bool ascending);
	}
}

