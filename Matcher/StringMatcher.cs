using System;
using System.Collections.Generic;

namespace RenDBCore
{
	/// <summary>
	/// A matcher variant for matching strings with various options.
	/// Use RegexMatcher if you're looking for a Regex search.
	/// </summary>
	public class StringMatcher : IMatcher<string> {
		
		public bool IsIgnoreCase;

		private Func<string, string, bool> doMatch;
		private string query;


		/// <summary>
		/// The string value to evaluate IsMatch against.
		/// </summary>
		public string Query {
			get { return query; }
			set {
				if(value == null)
					throw new ArgumentNullException("value");
				query = value;
			}
		}


		public StringMatcher(string query, Types matchType,
			bool isIgnoreCase = false)
		{
			this.Query = query;
			this.IsIgnoreCase = isIgnoreCase;

			SetMatchType(matchType);
		}

		/// <summary>
		/// Sets specified match type for next IsMatch operation.
		/// </summary>
		public void SetMatchType(Types matchType)
		{
			switch((int)matchType) {
			case 0: doMatch = new Func<string, string, bool>(MatchContain); break;
			case 1: doMatch = new Func<string, string, bool>(MatchStartWith); break;
			case 2: doMatch = new Func<string, string, bool>(MatchEndWith); break;
			case 3: doMatch = new Func<string, string, bool>(MatchEqual); break;
			case 4: doMatch = new Func<string, string, bool>(MatchNotEqual); break;
			}
		}

		/// <summary>
		/// Returns whether specified key matches with initialized condition.
		/// </summary>
		public bool IsMatch(string key)
		{
			string q = query;
			if(IsIgnoreCase) {
				q = q.ToLower();
				key = key.ToLower();
			}

			return doMatch(q, key);
		}

		bool MatchContain(string q, string key) { return key.Contains(q); }

		bool MatchStartWith(string q, string key) { return key.StartsWith(q); }

		bool MatchEndWith(string q, string key) { return key.EndsWith(q); }

		bool MatchEqual(string q, string key) { return key.Equals(q); }

		bool MatchNotEqual(string q, string key) { return !key.Equals(q); }


		/// <summary>
		/// Types of string match operation to perform.
		/// </summary>
		public enum Types {
			Contain = 0,
			StartWith,
			EndWith,
			Equal,
			NotEqual
		}
	}
}

