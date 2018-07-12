using System;
using System.Collections.Generic;

namespace RenDBCore
{
	/// <summary>
	/// A query object used for RenDB during a "match" search operation.
	/// </summary>
	public class MatchQuery<K> : IMatcher<K> {

		private List<MatchGroup<K>> groupConditions;


		public MatchGroup<K> this[int index] {
			get {
				if(index < 0 || index >= groupConditions.Count)
					throw new ArgumentOutOfRangeException("index");
				return groupConditions[index];
			}
		}

		/// <summary>
		/// A convenience property for generating a new empty group instead of calling its constructor.
		/// </summary>
		public MatchGroup<K> NewMatchGroup {
			get { return new MatchGroup<K>(); }
		}


		public MatchQuery ()
		{
			groupConditions = new List<MatchGroup<K>>();
		}

		/// <summary>
		/// Adds specified match query group to list.
		/// The group will act as an "OR" condition during IsMatch check.
		/// </summary>
		public void AddMatchGroup(MatchGroup<K> group)
		{
			groupConditions.Add(group);
		}

		/// <summary>
		/// Adds a new empty match group to list and returns it.
		/// The group will act as an "OR" condition during IsMatch check.
		/// </summary>
		public MatchGroup<K> AddMatchGroup()
		{
			var group = NewMatchGroup;
			AddMatchGroup(group);
			return group;
		}

		/// <summary>
		/// Adds specified IMatcher interface to targetMatchGroup.
		/// This is the same as calling AddMatcher directly on the MatchGroup object.
		/// </summary>
		public void AddMatcher(IMatcher<K> matcher, MatchGroup<K> targetGroup)
		{
			targetGroup.AddMatcher(matcher);
		}

		/// <summary>
		/// Returns whether specified key matches with any MatchGroup object in this query.
		/// </summary>
		public bool IsMatch(K key)
		{
			// If any condition group matches the specified key, it's a match.
			for(int i=0; i<groupConditions.Count; i++) {
				if(groupConditions[i].IsMatch(key))
					return true;
			}
			return false;
		}
	}
}

