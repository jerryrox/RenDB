using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	public class TreeEnumerator<K, V> : IEnumerator<Tuple<K, V>> {

		readonly ITreeNodeManager<K, V> nodeManager;
		readonly Func<bool> scanNext;

		private TreeNode<K, V> curNode;
		private Tuple<K, V> curEntry;
		private int curIndex;
		private bool isFinished;


		/// <summary>
		/// Returns the node currently on enumeration.
		/// </summary>
		public TreeNode<K, V> CurrentNode {
			get { return curNode; }
		}

		/// <summary>
		/// Returns the current entry's index.
		/// </summary>
		public int CurrentIndex {
			get { return curIndex;}
		}

		/// <summary>
		/// Returns the current entry being yielded.
		/// </summary>
		public Tuple<K, V> Current {
			get { return curEntry; }
		}

		/// <summary>
		/// Returns the current entry being yielded.
		/// </summary>
		object IEnumerator.Current {
			get { return (object)curEntry; }
		}


		public TreeEnumerator(ITreeNodeManager<K, V> nodeManager, TreeNode<K, V> node,
			int startIndex, TreeScanDirections direction)
		{
			this.nodeManager = nodeManager;
			this.curNode = node;
			this.curIndex = startIndex;

			this.scanNext = (
				direction == TreeScanDirections.Ascending ?
				new Func<bool>(Ascend) :
				new Func<bool>(Descend)
			);
		}

		/// <summary>
		/// Enumerate through nodes for next entry.
		/// </summary>
		public bool MoveNext()
		{
			if(isFinished)
				return false;

			return scanNext();
		}

		public void Reset()
		{
			// No need to reset!
		}

		public void Dispose()
		{
			// Nothing to dispose!
		}

		/// <summary>
		/// Tries moving to next entry by ascending order.
		/// </summary>
		bool Ascend()
		{
			curIndex ++;

			if(curNode.IsLeaf) {
				while(true) {
					// If a valid entry in node was found, return it.
					if(curIndex < curNode.EntryCount) {
						curEntry = curNode.GetEntry(curIndex);
						return true;
					}
					// Else if no valid entry in node and there is a parent node
					else if(curNode.ParentId != 0) {
						// Adjust index to point to current node in parent.
						curIndex = curNode.IndexInParent();
						// Set target node one level up.
						curNode = nodeManager.Find(curNode.ParentId);

						// Validate parent
						if(curIndex < 0 || curNode == null)
							throw new Exception("An error in BTree was detected.");
					}
					// No more parent nodes and valid entries. We should just finish off here
					else {
						curEntry = null;
						isFinished = true;
						return false;
					}
				}
			}
			else {
				do {
					// Get the next child node on first iteration,
					// or the first child node on further iterations.
					curNode = curNode.GetChildNode(curIndex);
					// Reset index to 0 to indicate first child and entry in the newly found node.
					curIndex = 0;

					// Validate new node
					if(curNode == null)
						throw new Exception("An error in BTree was detected.");
				}
				while(!curNode.IsLeaf);

				curEntry = curNode.GetEntry(curIndex);
				return true;
			}
		}

		/// <summary>
		/// Tries moving to next entry by descending order.
		/// </summary>
		bool Descend()
		{
			curIndex --;

			if(curNode.IsLeaf) {
				while(true) {
					// If a valid entry in node was found, return it.
					if(curIndex >= 0) {
						curEntry = curNode.GetEntry(curIndex);
						return true;
					}
					// Else if no valid entry in node and there is a parent node
					else if(curNode.ParentId != 0) {
						// Adjust index to point to current node in parent.
						curIndex = curNode.IndexInParent() - 1;
						// Set target node one level up.
						curNode = nodeManager.Find(curNode.ParentId);

						// Validate parent
						if(curNode == null)
							throw new Exception("An error in BTree was detected.");
					}
					// No more parent nodes and valid entries. We should just finish off here
					else {
						curEntry = null;
						isFinished = true;
						return false;
					}
				}
			}
			else {
				do {
					// Get the next child node on first iteration,
					// or the last child node on further iterations.
					curNode = curNode.GetChildNode(curIndex);
					// Reset index to 0 to indicate first child and entry in the newly found node.
					curIndex = curNode.EntryCount;

					// Validate new node
					if(curIndex < 0 || curNode == null)
						throw new Exception("An error in BTree was detected.");
				}
				while(!curNode.IsLeaf);

				// Converting count to 0-based index.
				curIndex --;
				curEntry = curNode.GetEntry(curIndex);
				return true;
			}
		}
	}
}

