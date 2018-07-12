using System;
using System.Collections;
using System.Collections.Generic;
using Renko.Matching;

namespace RenDBCore.Internal
{
	public class TreeMatchScanner<K, V> : IEnumerable<Tuple<K, V>> {

		readonly ITreeNodeManager<K, V> nodeManager;
		readonly TreeNode<K, V> node;
		readonly int startIndex;
		readonly IMatcher<K> matcher;
		readonly TreeScanDirections direction;


		public TreeMatchScanner(ITreeNodeManager<K, V> nodeManager, TreeNode<K, V> node,
			int startIndex, IMatcher<K> matcher, TreeScanDirections direction)
		{
			if(nodeManager == null)
				throw new ArgumentNullException("nodeManager");
			if(node == null)
				throw new ArgumentNullException("node");

			this.nodeManager = nodeManager;
			this.node = node;
			this.startIndex = startIndex;
			this.matcher = matcher;
			this.direction = direction;
		}

		IEnumerator<Tuple<K, V>> IEnumerable<Tuple<K, V>>.GetEnumerator()
		{
			var enumerator = new TreeEnumerator<K, V>(nodeManager, node, startIndex, direction);
			while(enumerator.MoveNext()) {
				var current = enumerator.Current;
				if(matcher.IsMatch(current.Item1))
					yield return current;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<Tuple<K, V>>)this).GetEnumerator();
		}
	}
}

