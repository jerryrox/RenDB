using System;

namespace RenDBCore
{
	/// <summary>
	/// Interface for IndexUtility that manages a database's indexes.
	/// </summary>
	public interface IIndexUtility {
		
		/// <summary>
		/// Registers a new index tree with sepcified params.
		/// </summary>
		void Register<K>(string label, string field, ISerializer<K> keySerializer);

		/// <summary>
		/// Registers a new index tree with specified params.
		/// </summary>
		void Register<K>(string label, string field, ISerializer<K> keySerializer, int blockSize,
			ushort minEntriesPerNode);

		/// <summary>
		/// Rebuilds the registered index tree from scratch with specified field and label.
		/// </summary>
		void HardRebuild<K>(string label, string field, ISerializer<K> keySerializer);

		/// <summary>
		/// Rebuilds the registered index tree with specified field and label.
		/// </summary>
		void SoftRebuild<K>(string field);

		/// <summary>
		/// Renames the registered index tree's label with specified values.
		/// </summary>
		void RenameLabel<K>(string field, string oldLabel, string newLabel, ISerializer<K> keySerializer);

		/// <summary>
		/// Returns whether an index with specified field name exists.
		/// </summary>
		bool ContainsField(string field);

		/// <summary>
		/// Returns whether index that uses the specified label exists.
		/// </summary>
		bool ContainsLabel(string label);

		/// <summary>
		/// Deletes the index with specified values.
		/// </summary>
		void Delete(string label, string field);
	}
}

