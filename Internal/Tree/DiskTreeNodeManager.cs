using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace RenDBCore.Internal
{
	/// <summary>
	/// A variant of ITreeNodeManager that stores tree node in disk.
	/// </summary>
	public class DiskTreeNodeManager<K, V> : ITreeNodeManager<K, V> {

		readonly IRecordStorage recordStorage;
		readonly Dictionary<uint, TreeNode<K, V>> dirtyNodes;
		readonly Dictionary<uint, WeakReference<TreeNode<K, V>>> weakNodes;
		readonly Queue<TreeNode<K, V>> strongNodes;
		readonly DiskTreeNodeSerializer<K, V> serializer;
		readonly IComparer<K> keyComparer;
		readonly IComparer<Tuple<K, V>> entryComparer;

		readonly int weakNodeCleanThreshold = 1000;
		readonly int maxStrongNodes = 200;
		readonly ushort minEntriesPerNode = 36;

		private List<uint> deleteIds;
		private TreeNode<K, V> rootNode;
		private int cleanupCounter;


		/// <summary>
		/// Minimum number of entries a node should have.
		/// </summary>
		public ushort MinEntriesPerNode {
			get { return minEntriesPerNode; }
		}

		/// <summary>
		/// Maximum number of entries a node can have.
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


		public DiskTreeNodeManager(ISerializer<K> keySerializer, ISerializer<V> valueSerializer,
			IRecordStorage recordStorage) : this(keySerializer, valueSerializer, recordStorage, Comparer<K>.Default) {}

		public DiskTreeNodeManager(ISerializer<K> keySerializer, ISerializer<V> valueSerializer,
			IRecordStorage recordStorage, IComparer<K> keyComparer)
		{
			if(recordStorage == null)
				throw new ArgumentNullException("recordStorage");

			this.recordStorage = recordStorage;
			this.dirtyNodes = new Dictionary<uint, TreeNode<K, V>>();
			this.weakNodes = new Dictionary<uint, WeakReference<TreeNode<K, V>>>();
			this.strongNodes = new Queue<TreeNode<K, V>>();
			this.serializer = new DiskTreeNodeSerializer<K, V>(this, keySerializer, valueSerializer);
			this.keyComparer = keyComparer;
			this.entryComparer = new TreeEntryComparer<K, V>(keyComparer);

			this.deleteIds = new List<uint>(weakNodeCleanThreshold / 2);
			this.cleanupCounter = 0;

			// Find or create the root node.
			var firstData = recordStorage.Find(1U);
			if(firstData != null) {
				this.rootNode = Find(BufferHelper.ReadUInt32(firstData, 0));
			}
			else {
				this.rootNode = CreateFirstRoot();
			}
		}

		/// <summary>
		/// Creates a new node that contains specified entries and children ids.
		/// </summary>
		public TreeNode<K, V> Create (IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
		{
			TreeNode<K, V> node = null;
			recordStorage.Create(id => {
				// Create a new node
				node = new TreeNode<K, V>(this, id, 0, entries, childrenIds);

				// Keep reference to all created nodes
				OnNodeInitialized(node);

				// Return the serialized value.
				return serializer.Serialize(node);
			});

			if(node == null)
				throw new Exception("Failed to create a new node.");

			return node;
		}

		/// <summary>
		/// Returns a node with matching id.
		/// </summary>
		public TreeNode<K, V> Find (uint id)
		{
			// Check if the node exists in memory
			if(weakNodes.ContainsKey(id)) {
				// Try getting the reference
				TreeNode<K, V> node = null;

				// If reference exists, return it
				if(weakNodes[id].TryGetValue(out node))
					return node;

				// If doesn't exist, it must have been collected by GC.
				weakNodes.Remove(id);
			}

			// Get data from record
			var data = recordStorage.Find(id);
			if(data == null)
				return null;

			// Deserialize the data to create node.
			var diskNode = serializer.Deserialize(id, data);
			// Keep track of new nodes
			OnNodeInitialized(diskNode);
			// Return it
			return diskNode;
		}

		/// <summary>
		/// Creates a new root node by splitting an existing root node.
		/// </summary>
		public TreeNode<K, V> CreateNewRoot (K key, V value, uint leftNodeId, uint rightNodeId)
		{
			// Create a new node
			var node = Create(
				new Tuple<K, V>[] { new Tuple<K, V>(key, value) },
				new uint[] { leftNodeId, rightNodeId }
			);

			// Make it root and return
			MakeRoot(node);
			return rootNode;
		}

		/// <summary>
		/// Makes the specified node a "root node".
		/// </summary>
		public void MakeRoot (TreeNode<K, V> node)
		{
			rootNode = node;
			recordStorage.Update(1U, LittleEndian.GetBytes(node.Id));
		}

		/// <summary>
		/// Flags the specified node dirty to save later.
		/// </summary>
		public void MarkAsChanged (TreeNode<K, V> node)
		{
			if(!dirtyNodes.ContainsKey(node.Id))
				dirtyNodes.Add(node.Id, node);
		}

		/// <summary>
		/// Deletes the specified node.
		/// </summary>
		public void Delete (TreeNode<K, V> node)
		{
			if(node == rootNode)
				rootNode = null;

			recordStorage.Delete(node.Id);

			if(dirtyNodes.ContainsKey(node.Id))
				dirtyNodes.Remove(node.Id);
		}

		/// <summary>
		/// Writes all dirty nodes to the stream source.
		/// </summary>
		public void Save ()
		{
			foreach(var pair in dirtyNodes)
				recordStorage.Update(pair.Value.Id, serializer.Serialize(pair.Value));

			dirtyNodes.Clear();
		}

		/// <summary>
		/// Creates the first node and set it to root.
		/// </summary>
		TreeNode<K, V> CreateFirstRoot()
		{
			// Create the first block that refers to the next block which is id(2).
			// Due to the design of RecordStorage, creating the very first block will result in creating two,
			// one for the special #0 (deletion marker) and the other which was asked to create.
			recordStorage.Create(LittleEndian.GetBytes(2U));

			// Creating a new node after above should make this block's id 2.
			return Create(null, null);
		}

		/// <summary>
		/// A method used for keeping track of specified node in weak/strong reference.
		/// </summary>
		void OnNodeInitialized(TreeNode<K, V> node)
		{
			// Create a weak reference
			weakNodes.Add(node.Id, new WeakReference<TreeNode<K, V>>(node));

			// Create a strong reference
			strongNodes.Enqueue(node);

			// Cleanup excessive strong nodes
			if(strongNodes.Count >= maxStrongNodes) {
				for(int i=maxStrongNodes/2; i<strongNodes.Count; i++)
					strongNodes.Dequeue();
			}

			// Cleanup weak nodes
			if(cleanupCounter ++ >= weakNodeCleanThreshold) {
				cleanupCounter = 0;
				deleteIds.Clear();

				// Find all useless references
				foreach(var pair in weakNodes) {
					TreeNode<K, V> target;
					if(pair.Value.TryGetValue(out target))
						deleteIds.Add(pair.Key);
				}

				// Remove 
				for(int i=0; i<deleteIds.Count; i++)
					weakNodes.Remove(deleteIds[i]);

				deleteIds.Clear();
			}
		}
	}
}

