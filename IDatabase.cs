using System;
using RenDBCore.Internal;

namespace RenDBCore
{
	/// <summary>
	/// Interface to the RenDB database instance.
	/// </summary>
	public interface IDatabase<T> : IDisposable where T : class, IModel<T> {

		/// <summary>
		/// Returns the name of this database.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Returns the object that manages database index.
		/// </summary>
		IIndexUtility Indexes { get; }

		/// <summary>
		/// Returns whether the database instance is disposed.
		/// </summary>
		bool IsDisposed { get; }


		/// <summary>
		/// Inserts the specified model instance.
		/// </summary>
		bool Insert(T value);

		/// <summary>
		/// Updates the specified model instance.
		/// </summary>
		bool Update(T value);

		/// <summary>
		/// Deletes a single entry with specified Guid.
		/// </summary>
		bool Delete(Guid id);

		/// <summary>
		/// Finds a single entry with matching Guid.
		/// </summary>
		T Find(Guid id);

		/// <summary>
		/// Finds using returned query object.
		/// Assign estimatedCount to set internal lists' capacity value.
		/// </summary>
		IDatabaseQuery<T> Query(int estimatedCount = 0);

		/// <summary>
		/// Disposes this database instance.
		/// </summary>
		void Dispose();
	}
}

