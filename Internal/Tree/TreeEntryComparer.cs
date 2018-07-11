using System;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	public class TreeEntryComparer<K, V> : IComparer<Tuple<K, V>> {

		private IComparer<K> keyComparer;


		public TreeEntryComparer(IComparer<K> keyComparer)
		{
			this.keyComparer = keyComparer;
		}

		public int Compare (Tuple<K, V> x, Tuple<K, V> y)
		{
			return keyComparer.Compare(x.Item1, y.Item1);
		}
	}
}

