using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	public class IndexTree<K, V> : IIndex<K, V> {

		readonly ITreeNodeManager<K, V> nodeManager;
		readonly bool allowDuplicateKeys;


		public IndexTree(ITreeNodeManager<K, V> nodeManager, bool allowDuplicateKeys = false)
		{
			if(nodeManager == null)
				throw new ArgumentNullException("nodeManager");
			
			this.nodeManager = nodeManager;
			this.allowDuplicateKeys = allowDuplicateKeys;
		}

		/// <summary>
		/// Creates a new entry with specified key and value.
		/// </summary>
		public void Insert (K key, V value)
		{
			// Find the node to insert the entry to.
			int insertIndex = 0;
			var leafNode = FindNode(key, ref insertIndex);

			// If index greater than or equal to 0, a key already exists.
			if(insertIndex >= 0 && !allowDuplicateKeys)
				throw new TreeKeyExistsException(key);

			// Insert new entry to node
			leafNode.InsertAsLeaf(key, value, insertIndex >= 0 ? insertIndex : ~insertIndex);

			// Split if overflow
			if(leafNode.IsOverflow)
				leafNode.Split();

			// Save
			nodeManager.Save();
		}

		/// <summary>
		/// Deletes an entry with matching KeyValue and optionally a comparer.
		/// Used with non-unique keys.
		/// </summary>
		public bool Delete (K key, V value, IComparer<V> valueComparer = null)
		{
			if(!allowDuplicateKeys) {
				throw new InvalidOperationException("Method not supported with unique keys. Use the other overload.");
			}

			// Determine which value comparer to use.
			valueComparer = valueComparer ?? Comparer<V>.Default;

			var isDeleted = false;
			var shouldContinue = true;
			try {
				while(shouldContinue) {
					using(var enumerator = (TreeEnumerator<K, V>)GetExactMatch(key, true).GetEnumerator()) {
						while(true) {
							// No more iteration, finish.
							if(!enumerator.MoveNext()) {
								shouldContinue = false;
								break;
							}

							// Get cur entry
							var entry = enumerator.Current;

							// If no more keys found, finish.
							if(nodeManager.KeyComparer.Compare(entry.Item1, key) > 0) {
								shouldContinue = false;
								break;
							}

							// If matching value, delete and restart scan.
							if(valueComparer.Compare(entry.Item2, value) == 0) {
								enumerator.CurrentNode.Remove(enumerator.CurrentIndex);
								isDeleted = true;
								break;
							}
						}
					}
				}
			}
			catch(Exception e) {
				throw e;
			}

			nodeManager.Save();
			return isDeleted;
		}

		/// <summary>
		/// Deletes all entries with matching key.
		/// Used with unique keys.
		/// </summary>
		public bool Delete (K key)
		{
			if(allowDuplicateKeys) {
				throw new InvalidOperationException("Method not supported with non-unique keys. " +
					"Use the other overload.");
			}

			// Find node to delete
			using(var enumerator = (TreeEnumerator<K, V>)GetExactMatch(key, true).GetEnumerator()) {
				// If there is a matching entry
				if(enumerator.MoveNext() && nodeManager.KeyComparer.Compare(key, enumerator.Current.Item1) == 0) {
					enumerator.CurrentNode.Remove(enumerator.CurrentIndex);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Finds and returns an entry with matching key.
		/// </summary>
		public Tuple<K, V> Get (K key)
		{
			// Find node containing the key.
			int index = 0;
			var node = FindNode(key, ref index);

			// If found a valid node and index, return entry from it.
			if(index >= 0 && node != null)
				return node.GetEntry(index);
			return null;
		}

		/// <summary>
		/// Finds all entries.
		/// </summary>
		public IEnumerable<Tuple<K, V>> GetAll(bool ascending)
		{
			TreeNode<K, V> startNode = null;
			int startIndex = 0;
			if(ascending)
				startNode = FindFirstNode(ref startIndex);
			else
				startNode = FindLastNode(ref startIndex);

			return new TreeScanner<K, V>(
				nodeManager,
				startNode,
				startIndex,
				ascending ? TreeScanDirections.Ascending : TreeScanDirections.Descending
			);
		}

		/// <summary>
		/// Finds all entries with keys matching the specified matcher.
		/// </summary>
		public IEnumerable<Tuple<K, V>> GetOptionMatch(bool ascending, IMatcher<K> matcher)
		{
			if(matcher == null)
				return GetAll(ascending);
			
			TreeNode<K, V> startNode = null;
			int startIndex = 0;
			if(ascending)
				startNode = FindFirstNode(ref startIndex);
			else
				startNode = FindLastNode(ref startIndex);

			return new TreeMatchScanner<K, V>(
				nodeManager,
				startNode,
				startIndex,
				matcher,
				ascending ? TreeScanDirections.Ascending : TreeScanDirections.Descending
			);
		}

		/// <summary>
		/// Finds all entries with the key larger than or equal to specified.
		/// </summary>
		public IEnumerable<Tuple<K, V>> GetExactMatch (K key, bool ascending)
		{
			int index = 0;
			var node = FindNodeIterative(key, nodeManager.RootNode, ascending, ref index);

			TreeScanDirections scanDirection = TreeScanDirections.Ascending;
			if(ascending) {
				index = (index >= 0 ? index : ~index) - 1;
			}
			else {
				index = index >= 0 ? index + 1 : ~index;
				scanDirection = TreeScanDirections.Descending;
			}

			return new TreeScanner<K, V>(
				nodeManager,
				node,
				index,
				scanDirection
			);
		}

		/// <summary>
		/// Finds and returns the node that contains the specified key starting from node.
		/// Outputs an iteration start index indicating the starting position of duplicate keys.
		/// Handles the case for duplicate keys.
		/// </summary>
		TreeNode<K, V> FindNodeIterative(K key, TreeNode<K, V> node, bool includeEqual, ref int startIndex)
		{
			// If empty node, just return that
			if(node.IsEmpty) {
				// Bitwise negation of 0 will make sure the caller doesn't recognize #0 as the insertion point.
				startIndex = ~0;
				return node;
			}

			// Peform a binary search with the key.
			int searchIndex = node.BinarySearch(key, includeEqual);
			// If an entry was found
			if(searchIndex >= 0) {
				// Deep-dive to find child nodes with matching key.
				if(!node.IsLeaf) {
					return FindNodeIterative(
						key, node.GetChildNode(searchIndex + (includeEqual ? 0 : 1)), includeEqual, ref startIndex
					);
				}
				// Result
				startIndex = searchIndex;
				return node;
			}
			// Else if not leaf
			else if(!node.IsLeaf) {
				// Continue search in the child node.
				return FindNodeIterative(key, node.GetChildNode(~searchIndex), includeEqual, ref startIndex);
			}

			// Just return current one
			startIndex = searchIndex;
			return node;
		}

		/// <summary>
		/// Finds and returns the node that contains the specified key starting from node.
		/// Outputs an insertion index indicating the ideal position for specified key to go in.
		/// </summary>
		TreeNode<K, V> FindNode(K key, TreeNode<K, V> node, ref int insertIndex)
		{
			// If empty node, just return that
			if(node.IsEmpty) {
				// Bitwise negation of 0 will make sure the caller doesn't recognize #0 as the insertion point.
				insertIndex = ~0;
				return node;
			}

			// Peform a binary search with the key.
			int searchIndex = node.BinarySearch(key);
			// If an entry was found
			if(searchIndex >= 0) {
				// Deep-dive to find child nodes with matching key.
				if(allowDuplicateKeys && !node.IsLeaf)
					return FindNode(key, node.GetChildNode(searchIndex), ref insertIndex);
				// Result
				insertIndex = searchIndex;
				return node;
			}
			// Else if not leaf
			else if(!node.IsLeaf) {
				// Continue search in the child node.
				return FindNode(key, node.GetChildNode(~searchIndex), ref insertIndex);
			}

			// Just return current one
			insertIndex = searchIndex;
			return node;
		}

		/// <summary>
		/// Finds and returns the node that contains the specified key starting from root node.
		/// Outputs an insertion index indicating the ideal position for specified key to go in.
		/// </summary>
		TreeNode<K, V> FindNode(K key, ref int insertIndex)
		{
			return FindNode(key, nodeManager.RootNode, ref insertIndex);
		}

		/// <summary>
		/// Finds and returns the node with the lowest value.
		/// </summary>
		TreeNode<K, V> FindFirstNode(ref int index)
		{
			// Find starting node
			TreeNode<K, V> startNode = nodeManager.RootNode;
			while(true) {
				if(startNode.IsLeaf)
					break;
				startNode = startNode.GetChildNode(0);
			}

			index = -1;
			return startNode;
		}

		/// <summary>
		/// Finds and returns the node with the highest value.
		/// </summary>
		TreeNode<K, V> FindLastNode(ref int index)
		{
			// Find starting node
			TreeNode<K, V> startNode = nodeManager.RootNode;
			while(true) {
				// Starting index should be assigned before leaf check!
				index = startNode.EntryCount;
				if(startNode.IsLeaf)
					break;
				startNode = startNode.GetChildNode(startNode.ChildNodesCount - 1);
			}

			return startNode;
		}
	}
}

