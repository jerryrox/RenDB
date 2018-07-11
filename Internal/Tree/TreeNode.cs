using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Represents a single node of an Index Tree object.
	/// </summary>
	public class TreeNode<K, V> {

		protected readonly ITreeNodeManager<K, V> nodeManager;
		protected readonly List<uint> childrenIds;
		protected readonly List<Tuple<K, V>> entries;

		protected uint id;
		protected uint parentId;


		/// <summary>
		/// The id of parent node.
		/// </summary>
		public uint ParentId {
			get { return parentId; }
			private set {
				parentId = value;
				nodeManager.MarkAsChanged(this);
			}
		}

		/// <summary>
		/// Returns the id of this node.
		/// </summary>
		public uint Id {
			get { return id; }
		}

		/// <summary>
		/// Returns the key of the last entry.
		/// </summary>
		public K MaxKey {
			get { return entries[entries.Count-1].Item1; }
		}

		/// <summary>
		/// Returns the key of the first entry.
		/// </summary>
		public K MinKey {
			get { return entries[0].Item1; }
		}

		/// <summary>
		/// Returns whether the entry list is empty.
		/// </summary>
		public bool IsEmpty {
			get { return entries.Count == 0; }
		}

		/// <summary>
		/// Returns whether this node doesn't contain any children.
		/// </summary>
		public bool IsLeaf {
			get { return childrenIds.Count == 0; }
		}

		/// <summary>
		/// Returns whether entry count has exceeded the node manager's max entry limit and it should split up.
		/// </summary>
		public bool IsOverflow {
			get { return entries.Count > nodeManager.MaxEntriesPerNode; }
		}

		/// <summary>
		/// Returns the number of entries in this node.
		/// </summary>
		public int EntryCount {
			get { return entries.Count; }
		}

		/// <summary>
		/// Returns the number of child nodes in this node.
		/// </summary>
		public int ChildNodesCount {
			get { return childrenIds.Count; }
		}

		/// <summary>
		/// Returns the array representation of children id list.
		/// </summary>
		public uint[] ChildrenIdArray {
			get { return childrenIds.ToArray(); }
		}

		/// <summary>
		/// Returns the array representation of entries list.
		/// </summary>
		public Tuple<K, V>[] Entries {
			get { return entries.ToArray(); }
		}


		public TreeNode(ITreeNodeManager<K, V> nodeManager, uint id, uint parentId,
			IEnumerable<Tuple<K, V>> entries = null,
			IEnumerable<uint> childrenIds = null)
		{
			if(nodeManager == null)
				throw new ArgumentNullException("nodeManager");
			
			this.nodeManager = nodeManager;
			this.childrenIds = new List<uint>();
			this.entries = new List<Tuple<K, V>>(nodeManager.MaxEntriesPerNode);

			this.id = id;
			this.parentId = parentId;

			// Make shallow copies of lists.
			if(entries != null)
				this.entries.AddRange(entries);
			if(childrenIds != null)
				this.childrenIds.AddRange(childrenIds);
		}

		/// <summary>
		/// Inserts a new entry to this node, assuming it's a leaf node.
		/// </summary>
		public void InsertAsLeaf(K key, V value, int index)
		{
			Debug.Assert(IsLeaf, "This node is not a leaf node.");

			entries.Insert(index, new Tuple<K, V>(key, value));
			nodeManager.MarkAsChanged(this);
		}

		/// <summary>
		/// Inserts a new entry to this node, assuming it's a parent node.
		/// </summary>
		public void InsertAsParent(K key, V value, uint left, uint right, out int insertIndex)
		{
			Debug.Assert(!IsLeaf, "This node is not a parent node.");

			// Find target insert index.
			insertIndex = BinarySearch(key);
			if(insertIndex < 0)
				insertIndex = ~insertIndex;

			// Insert entry
			entries.Insert(insertIndex, new Tuple<K, V>(key, value));

			// Insert left child index and assign right child index.
			childrenIds.Insert(insertIndex, left);
			childrenIds[insertIndex+1] = right;

			// Mark this node dirty
			nodeManager.MarkAsChanged(this);
		}

		/// <summary>
		/// Splits this node to left and right nodes.
		/// </summary>
		public void Split(out TreeNode<K, V> leftNode, out TreeNode<K, V> rightNode)
		{
			Debug.Assert(IsOverflow, "This node is not yet overflowing.");

			bool isLeaf = IsLeaf;
			var halfCount = nodeManager.MinEntriesPerNode;
			var middleEntry = entries[halfCount];

			// Get larger entries and children for new node.
			var largeEntries = new Tuple<K, V>[halfCount];
			uint[] largeChildren = null;
			// Copy larger entries to newer node.
			entries.CopyTo(halfCount+1, largeEntries, 0, halfCount);
			// Copy child node ids if not a leaf.
			if(!isLeaf) {
				largeChildren = new uint[halfCount + 1];
				childrenIds.CopyTo(halfCount+1, largeChildren, 0, halfCount);
			}

			// Create larger node
			var largeNode = nodeManager.Create(largeEntries, largeChildren);

			// All child nodes moved to newer node must refresh their parentId.
			if(!isLeaf) {
				for(int i=0; i<largeChildren.Length; i++) {
					nodeManager.Find(largeChildren[i]).ParentId = largeNode.id;
				}
			}

			// Remove all large entries and children ids from this node.
			// Entries list must also remove the middle entry because it will go to the parent node.
			entries.RemoveRange(halfCount);
			if(!isLeaf) {
				childrenIds.RemoveRange(halfCount + 1);
			}

			// Try getting parent node
			var parentNode = parentId == 0 ? null : nodeManager.Find(parentId);

			// If no parent, create a new parent node with the middle entry
			if(parentNode == null) {
				parentNode = nodeManager.CreateNewRoot(
					middleEntry.Item1,
					middleEntry.Item2,
					id,
					largeNode.id
				);

				// Reassign parent id
				this.ParentId = parentNode.id;
				largeNode.ParentId = parentNode.id;
			}
			// Move the middle element to the parent node.
			else {
				int insertIndex = 0;
				parentNode.InsertAsParent(
					middleEntry.Item1,
					middleEntry.Item2,
					id,
					largeNode.id,
					out insertIndex
				);

				// Reassign parent id for large node only because this node's already been set.
				largeNode.ParentId = parentNode.id;

				// Split parent node if overflowing.
				if(parentNode.IsOverflow)
					Split();
			}

			// Output nodes
			leftNode = this;
			rightNode = largeNode;

			// Mark this node dirty
			nodeManager.MarkAsChanged(this);
		}

		/// <summary>
		/// Allows simple Split() call without assigning the output params.
		/// </summary>
		public void Split()
		{
			TreeNode<K, V> left, right;
			Split(out left, out right);
		}

		/// <summary>
		/// Removes an entry at specified index.
		/// </summary>
		public void Remove(int index)
		{
			if(index < 0 || index >= entries.Count)
				throw new ArgumentOutOfRangeException("index");

			if(IsLeaf) {
				// Remove the entry first
				entries.RemoveAt(index);
				nodeManager.MarkAsChanged(this);

				// If entry count meets the minimum number or there is no parent, just finish up.
				if(EntryCount < nodeManager.MinEntriesPerNode || parentId == 0)
					return;

				// If entry count doesn't meet the minimum, fix the node.
				FixNode();
			}
			else {
				// Get the largest node from the left subtree.
				TreeNode<K, V> largestNode;
				int largestIndex;
				var leftTree = nodeManager.Find(childrenIds[index]);
				leftTree.FindLargest(out largestNode, out largestIndex);

				// Get the largest entry from the largest node found.
				var largestEntry = largestNode.GetEntry(largestIndex);

				// Replace entry
				entries[index] = largestEntry;
				nodeManager.MarkAsChanged(this);

				// Remove the largestEntry from the node we took from
				largestNode.Remove(largestIndex);
			}
		}

		/// <summary>
		/// Finds the largest entry in this node or deep child nodes.
		/// </summary>
		public void FindLargest(out TreeNode<K, V> node, out int index)
		{
			// If already reached the bottom of the tree, output the last entry.
			if(IsLeaf) {
				node = this;
				index = entries.Count - 1;
				return;
			}

			// Find the largest entry from the right-most child.
			var rightMostNode = nodeManager.Find(childrenIds[childrenIds.Count - 1]);
			rightMostNode.FindLargest(out node, out index);
		}

		/// <summary>
		/// Finds the smallest entry in this node or deep child nodes.
		/// </summary>
		public void FindSmallest(out TreeNode<K, V> node, out int index)
		{
			// If already reached the bottom of the tree, output the first entry.
			if(IsLeaf) {
				node = this;
				index = 0;
				return;
			}

			// Find the smallest entry from the left-most child.
			var leftMostNode = nodeManager.Find(childrenIds[0]);
			leftMostNode.FindSmallest(out node, out index);
		}

		/// <summary>
		/// Returns the index of this node in parent's entry list.
		/// </summary>
		public int IndexInParent()
		{
			var parent = nodeManager.Find(parentId);
			if(parent == null)
				throw new Exception("Parent node not found with id: " + parentId);

			var siblings = parent.childrenIds;
			for(int i=0; i<siblings.Count; i++) {
				if(siblings[i] == id)
					return i;
			}
			throw new Exception("Current node not found with id: " + id);
		}

		/// <summary>
		/// Performs a binary search for specified key.
		/// </summary>
		public int BinarySearch(K key)
		{
			return entries.BinarySearch(
				new Tuple<K, V>(key, default(V)),
				nodeManager.EntryComparer
			);
		}

		/// <summary>
		/// Performs a binary search for the specified key.
		/// There may be multiple occurrences so firstOccurrence flag will decide whether first or last of them
		/// will be returned.
		/// </summary>
		public int BinarySearch(K key, bool firstOccurrence)
		{
			if(firstOccurrence)
				return entries.BinarySearchFirst(new Tuple<K, V>(key, default(V)), nodeManager.EntryComparer);
			else
				return entries.BinarySearchLast(new Tuple<K, V>(key, default(V)), nodeManager.EntryComparer);
		}

		/// <summary>
		/// Returns the child node with specified index, relative to this node's childrenId list.
		/// </summary>
		public TreeNode<K, V> GetChildNode(int index)
		{
			return nodeManager.Find(childrenIds[index]);
		}

		/// <summary>
		/// Returns the entry located at specified index.
		/// </summary>
		public Tuple<K, V> GetEntry(int index)
		{
			return entries[index];
		}

		/// <summary>
		/// Returns whether an entry exists at specified index.
		/// </summary>
		public bool EntryExists(int index)
		{
			return index >= 0 && index < entries.Count;
		}

		/// <summary>
		/// Fixes this node after removing an entry which leads unbalancing.
		/// </summary>
		void FixNode()
		{
			var minEntryCount = nodeManager.MinEntriesPerNode;
			var indexInParent = IndexInParent();
			var parent = nodeManager.Find(parentId);

			var rightSibling = GetRightSibling(parent, indexInParent);
			if(FixByRotateLeft(minEntryCount, indexInParent, rightSibling, parent))
				return;
			
			var leftSibling = GetLeftSibling(parent, indexInParent);
			if(FixByRotateRight(minEntryCount, indexInParent, leftSibling, parent))
				return;
			
			FixByCombine(
				rightSibling != null ? indexInParent : indexInParent - 1,
				rightSibling != null ? this : leftSibling,
				rightSibling != null ? rightSibling : this,
				parent
			);
		}

		/// <summary>
		/// Tries fixing this node by left rotation.
		/// Returns whether it's a success.
		/// </summary>
		bool FixByRotateLeft(ushort minEntryCount, int indexInParent,
			TreeNode<K, V> rightSibling, TreeNode<K, V> parent)
		{
			// If the right sibling exists and has more than minimum entries
			if(rightSibling != null && rightSibling.EntryCount > minEntryCount) {
				// Add the first larger entry from parent to this node.
				entries.Add(parent.GetEntry(indexInParent));

				// Replace the entry we just added with the right sibling's left-most entry.
				parent.entries[indexInParent] = rightSibling.entries[0];
				rightSibling.entries.RemoveAt(0);

				// If right sibling is not a leaf
				if(!rightSibling.IsLeaf) {
					// Get the right sibling's first child.
					var firstChild = nodeManager.Find(rightSibling.childrenIds[0]);
					// Change the child's owner to this node.
					firstChild.parentId = id;
					nodeManager.MarkAsChanged(firstChild);

					// Change child ids list.
					childrenIds.Add(firstChild.id);
					rightSibling.childrenIds.RemoveAt(0);
				}

				// Finalize
				nodeManager.MarkAsChanged(this);
				nodeManager.MarkAsChanged(parent);
				nodeManager.MarkAsChanged(rightSibling);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Tries fixing this node by right rotation.
		/// Returns whether it's a success.
		/// </summary>
		bool FixByRotateRight(ushort minEntryCount, int indexInParent,
			TreeNode<K, V> leftSibling, TreeNode<K, V> parent)
		{
			// If the left sibling exists and has more than minimum entries
			if(leftSibling != null && leftSibling.EntryCount > minEntryCount) {
				// Reference to leftSibling's lists.
				var leftEntries = leftSibling.entries;
				var leftChildIds = leftSibling.childrenIds;

				// Add the last smaller entry from parent to this node.
				entries.Insert(0, parent.GetEntry(indexInParent - 1));

				// Replace the entry we just added with the left sibling's right-most entry.
				parent.entries[indexInParent - 1] = leftEntries[leftEntries.Count - 1];
				leftEntries.RemoveAt(leftEntries.Count - 1);

				// If left sibling is not a leaf
				if(!leftSibling.IsLeaf) {
					// Get the left sibling's last child.
					var lastChild = nodeManager.Find(leftChildIds[leftChildIds.Count - 1]);
					// Change the child's owner to this node.
					lastChild.parentId = id;
					nodeManager.MarkAsChanged(lastChild);

					// Change child ids
					childrenIds.Insert(0, lastChild.id);
					leftChildIds.RemoveAt(leftChildIds.Count - 1);
				}

				// Finalize
				nodeManager.MarkAsChanged(this);
				nodeManager.MarkAsChanged(parent);
				nodeManager.MarkAsChanged(leftSibling);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Fixes this node by combining parent entry downwards.
		/// </summary>
		void FixByCombine(int targetParentIndex,
			TreeNode<K, V> leftChild, TreeNode<K, V> rightChild, TreeNode<K, V> parent)
		{
			// Move the entry at targetParentIndex from parent to left child.
			leftChild.entries.Add(parent.GetEntry(targetParentIndex));

			// Move all elements in the right child to the left child.
			leftChild.entries.AddRange(rightChild.entries);
			leftChild.childrenIds.AddRange(rightChild.childrenIds);
			// Update parent of the children moved from right child.
			for(int i=0; i<rightChild.childrenIds.Count; i++) {
				var childNode = nodeManager.Find(rightChild.childrenIds[i]);
				childNode.parentId = leftChild.id;
				nodeManager.MarkAsChanged(childNode);
			}

			// Remove the entry at targetParentIndex and rightChild node from parent.
			parent.entries.RemoveAt(targetParentIndex);
			parent.childrenIds.RemoveAt(targetParentIndex + 1);
			// Node no longer used.
			nodeManager.Delete(rightChild);

			// If the parent node is a root and no longer has an entry
			if(parent.parentId == 0 && parent.EntryCount == 0) {
				// Make the left child root
				leftChild.parentId = 0;
				nodeManager.MarkAsChanged(leftChild);
				nodeManager.MakeRoot(leftChild);
				nodeManager.Delete(parent);
			}
			// If the parent node is not a root and doesn't have the minimum entry count
			else if(parent.parentId != 0 && parent.EntryCount < nodeManager.MinEntriesPerNode) {
				nodeManager.MarkAsChanged(leftChild);
				nodeManager.MarkAsChanged(parent);
				// Fix the parent node.
				parent.FixNode();
			}
			// Else, just finalize.
			else {
				nodeManager.MarkAsChanged(leftChild);
				nodeManager.MarkAsChanged(parent);
			}
		}

		/// <summary>
		/// Returns the right sibling of the node under specified parent at index.
		/// </summary>
		TreeNode<K, V> GetRightSibling(TreeNode<K, V> parent, int indexInParent)
		{
			indexInParent ++;
			if(indexInParent < parent.ChildNodesCount)
				return parent.GetChildNode(indexInParent);
			return null;
		}

		/// <summary>
		/// Returns the left sibling of the node under specified parent at index.
		/// </summary>
		TreeNode<K, V> GetLeftSibling(TreeNode<K, V> parent, int indexInParent)
		{
			indexInParent --;
			if(indexInParent >= 0)
				return parent.GetChildNode(indexInParent);
			return null;
		}
	}
}

