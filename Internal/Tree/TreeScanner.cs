using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	public class TreeScanner<K, V> : IEnumerable<Tuple<K, V>> {

		readonly ITreeNodeManager<K, V> nodeManager;
		readonly TreeNode<K, V> node;
		readonly int startIndex;
		readonly TreeScanDirections direction;


		public TreeScanner(ITreeNodeManager<K, V> nodeManager, TreeNode<K, V> node,
			int startIndex, TreeScanDirections direction)
		{
			if(nodeManager == null)
				throw new ArgumentNullException("nodeManager");
			if(node == null)
				throw new ArgumentNullException("node");

			this.nodeManager = nodeManager;
			this.node = node;
			this.startIndex = startIndex;
			this.direction = direction;
		}

		IEnumerator<Tuple<K, V>> IEnumerable<Tuple<K, V>>.GetEnumerator()
		{
			return new TreeEnumerator<K, V>(nodeManager, node, startIndex, direction);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<Tuple<K, V>>)this).GetEnumerator();
		}
	}
}

