using System;

namespace RenDBCore
{
	/// <summary>
	/// Interface of a Matcher variant used for checking certain match conditions.
	/// i.e. Regex, String.Contains, etc...
	/// </summary>
	public interface IMatcher<K> {
		
		/// <summary>
		/// Returns whether specified key matches with initialized condition.
		/// </summary>
		bool IsMatch(K key);
	}
}

