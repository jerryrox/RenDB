using System;
using System.Text.RegularExpressions;

namespace RenDBCore
{
	/// <summary>
	/// A matcher object for matching strings using Regex.
	/// </summary>
	public class RegexMatcher : IMatcher<string> {

		private Regex regex;


		public Regex MatchTarget {
			get { return regex; }
			set {
				if(value == null)
					throw new ArgumentNullException("value");
				regex = value;
			}
		}


		public RegexMatcher(string regex, RegexOptions options = RegexOptions.None)
		{
			MatchTarget = new Regex(regex, options);
		}

		public RegexMatcher(Regex regex)
		{
			MatchTarget = regex;
		}

		/// <summary>
		/// Returns whether specified key matches with initialized condition.
		/// </summary>
		public bool IsMatch(string key)
		{
			return regex.IsMatch(key);
		}
	}
}

