using System;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// A variant of ITreeNodeManager that stores tree node in memory.
	/// </summary>
	public class MemoryTreeNodeManager<K, V> : ITreeNodeManager<K, V> {

		readonly Dictionary<uint, TreeNode<K, V>> nodes;
		readonly ushort minEntriesPerNode;
		readonly IComparer<K> keyComparer;
		readonly IComparer<Tuple<K, V>> entryComparer;

		private int idCounter;
		private TreeNode<K, V> rootNode;


		/// <summary>
		/// Returns the minimum number of entries that can exist per node.
		/// </summary>
		public ushort MinEntriesPerNode {
			get { return minEntriesPerNode; }
		}

		/// <summary>
		/// Returns the maximum number of entries that can exist per node.
		/// This is equal to MinEntriesPerNode * 2.
		/// </summary>
		public ushort MaxEntriesPerNode {
			get { return (ushort)(minEntriesPerNode * 2); }
		}

		/// <summary>
		/// Returns the comparer used for comparing keys.
		/// </summary>
		public IComparer<K> KeyComparer {
			get { return keyComparer; }
		}

		/// <summary>
		/// Returns the comparer used for comparing entries.
		/// </summary>
		public IComparer<Tuple<K, V>> EntryComparer {
			get { return entryComparer; }
		}

		/// <summary>
		/// Returns the root node of the tree.
		/// </summary>
		public TreeNode<K, V> RootNode {
			get { return rootNode; }
		}


		public MemoryTreeNodeManager(ushort minEntriesPerNode, IComparer<K> keyComparer)
		{
			this.nodes = new Dictionary<uint, TreeNode<K, V>>();
			this.minEntriesPerNode = minEntriesPerNode;
			this.keyComparer = keyComparer;
			this.entryComparer = new TreeEntryComparer<K, V>(keyComparer);

			this.idCounter = 1;
			this.rootNode = Create(null, null);
		}

		/// <summary>
		/// Creates a new node that contains specified entries and children ids.
		/// </summary>
		public TreeNode<K, V> Create (IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
		{
			var newNode = new TreeNode<K, V>(
				this,
				(uint)idCounter++,
				0,
				entries,
				childrenIds
			);
			nodes[newNode.Id] = newNode;
			return newNode;
		}

		/// <summary>
		/// Returns a node with matching id.
		/// </summary>
		public TreeNode<K, V> Find (uint id)
		{
			if(!nodes.ContainsKey(id))
				throw new ArgumentException("Node not found with id: " + id);
			return nodes[id];
		}

		/// <summary>
		/// Creates a new root node by splitting an existing root node.
		/// </summary>
		public TreeNode<K, V> CreateNewRoot (K key, V value, uint leftNodeId, uint rightNodeId)
		{
			var node = Create(
				new Tuple<K, V>[] { new Tuple<K, V>(key, value) },
				new uint[] { leftNodeId, rightNodeId }
			);

			MakeRoot(node);
			return node;
		}

		/// <summary>
		/// Makes the specified node a "root node".
		/// </summary>
		public void MakeRoot (TreeNode<K, V> node)
		{
			this.rootNode = node;
		}

		/// <summary>
		/// Flags the specified node dirty to save later.
		/// </summary>
		public void MarkAsChanged (TreeNode<K, V> node)
		{
			// Nothing to do
		}

		/// <summary>
		/// Deletes the specified node.
		/// </summary>
		public void Delete (TreeNode<K, V> node)
		{
			if(node == rootNode)
				rootNode = null;
			if(nodes.ContainsKey(node.Id))
				nodes.Remove(node.Id);
		}

		/// <summary>
		/// Writes all dirty nodes to the stream source.
		/// </summary>
		public void Save ()
		{
			// Nothing to do
		}
	}
}

