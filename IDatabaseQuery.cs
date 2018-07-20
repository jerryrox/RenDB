using System;
using System.Collections.Generic;

namespace RenDBCore
{
	/// <summary>
	/// Interface for developers to access the DatabaseQuery object methods.
	/// </summary>
	public interface IDatabaseQuery<T> where T : class, IModel<T> {

		/// <summary>
		/// Resets query state to initial state.
		/// </summary>
		IDatabaseQuery<T> Reset();

		/// <summary>
		/// Sets sorting flag for specified direction.
		/// </summary>
		IDatabaseQuery<T> Sort(bool isAscending);

		/// <summary>
		/// Finds all entries by unique index.
		/// </summary>
		IDatabaseQuery<T> Find();

		/// <summary>
		/// Finds all unindexed entries with condition-checker function.
		/// Always search for indexed fields for performance!
		/// </summary>
		IDatabaseQuery<T> Find(Func<T, bool> condition);

		/// <summary>
		/// Finds all entries by specified field.
		/// </summary>
		IDatabaseQuery<T> Find<K>(string field);

		/// <summary>
		/// Finds all entries with exact field data.
		/// </summary>
		IDatabaseQuery<T> Find<K>(string field, K key);

		/// <summary>
		/// Finds all entries with condition-checker function.
		/// </summary>
		IDatabaseQuery<T> Find<K>(string field, Func<K, bool> condition);

		/// <summary>
		/// Skips entries by specified amount.
		/// </summary>
		IDatabaseQuery<T> Skip(int count);

		/// <summary>
		/// Returns all entries found.
		/// </summary>
		IEnumerator<T> GetAll();

		/// <summary>
		/// Returns all entries found clamped to specified count.
		/// </summary>
		IEnumerator<T> GetRange(int count);

		/// <summary>
		/// Returns the first found entry.
		/// </summary>
		T GetFirst();

		/// <summary>
		/// Returns the total indexes found.
		/// May not always be valid if index is not complete or somethow the database data is missing.
		/// </summary>
		int GetCount();
	}
}
