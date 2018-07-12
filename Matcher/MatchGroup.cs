using System;
using System.Collections.Generic;

namespace RenDBCore
{
	/// <summary>
	/// A class containing a list of IMatcher instances.
	/// </summary>
	public class MatchGroup<K> : IMatcher<K> {

		private List<IMatcher<K>> matchers;


		public IMatcher<K> this[int index] {
			get {
				if(index < 0 || index >= matchers.Count)
					throw new ArgumentOutOfRangeException("index");
				return matchers[index];
			}
		}


		public MatchGroup()
		{
			matchers = new List<IMatcher<K>>();
		}

		/// <summary>
		/// Adds specified matcher to this group.
		/// </summary>
		public void AddMatcher(IMatcher<K> matcher)
		{
			matchers.Add(matcher);
		}

		/// <summary>
		/// Returns whether specified key matches with all matchers in this group.
		/// </summary>
		public bool IsMatch(K key)
		{
			for(int i=0; i<matchers.Count; i++) {
				if(!matchers[i].IsMatch(key))
					return false;
			}
			return true;
		}
	}
}

