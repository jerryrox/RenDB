using System;
using System.IO;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a TreeNodeManager variant that stores data in different sources.
	/// </summary>
	public interface ITreeNodeManager<K, V> {
		
		/// <summary>
		/// Minimum number of entries a node should have.
		/// </summary>
		ushort MinEntriesPerNode { get; }

		/// <summary>
		/// Maximum number of entries a node can have.
		/// </summary>
		ushort MaxEntriesPerNode { get; }

		/// <summary>
		/// Returns the comparer used for comparing keys.
		/// </summary>
		IComparer<K> KeyComparer { get; }

		/// <summary>
		/// Returns the comparer used for comparing entries.
		/// </summary>
		IComparer<Tuple<K, V>> EntryComparer { get; }

		/// <summary>
		/// Returns the root node of the tree.
		/// </summary>
		TreeNode<K, V> RootNode { get; }


		/// <summary>
		/// Creates a new node that contains specified entries and children ids.
		/// </summary>
		TreeNode<K, V> Create (IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds);

		/// <summary>
		/// Returns a node with matching id.
		/// </summary>
		TreeNode<K, V> Find (uint id);

		/// <summary>
		/// Creates a new root node by splitting an existing root node.
		/// </summary>
		TreeNode<K, V> CreateNewRoot (K key, V value, uint leftNodeId, uint rightNodeId);

		/// <summary>
		/// Makes the specified node a "root node".
		/// </summary>
		void MakeRoot (TreeNode<K, V> node);

		/// <summary>
		/// Flags the specified node dirty to save later.
		/// </summary>
		void MarkAsChanged (TreeNode<K, V> node);

		/// <summary>
		/// Deletes the specified node.
		/// </summary>
		void Delete (TreeNode<K, V> node);

		/// <summary>
		/// Writes all dirty nodes to the stream source.
		/// </summary>
		void Save ();
	}

}

