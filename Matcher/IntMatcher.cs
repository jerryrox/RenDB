using System;
using System.Collections.Generic;

namespace RenDBCore
{
	public class IntMatcher : IMatcher<int> {
		
		private Func<int, bool> doMatch;
		private int number;


		/// <summary>
		/// The value to evaluate IsMatch against.
		/// </summary>
		public int Number {
			get { return number; }
			set { number = value; }
		}


		public IntMatcher(int number, Types matchType)
		{
			this.Number = number;

			SetMatchType(matchType);
		}

		/// <summary>
		/// Sets specified match type for next IsMatch operation.
		/// </summary>
		public void SetMatchType(Types matchType)
		{
			switch((int)matchType) {
			case 0: doMatch = new Func<int, bool>(MatchEqual); break;
			case 1: doMatch = new Func<int, bool>(MatchNotEqual); break;
			case 2: doMatch = new Func<int, bool>(MatchGreater); break;
			case 3: doMatch = new Func<int, bool>(MatchGreaterOrEqual); break;
			case 4: doMatch = new Func<int, bool>(MatchLess); break;
			case 5: doMatch = new Func<int, bool>(MatchLessOrEqual); break;
			}
		}

		/// <summary>
		/// Returns whether specified key matches with initialized condition.
		/// </summary>
		public bool IsMatch(int key) { return doMatch(key); }

		bool MatchEqual(int key) { return key == number; }

		bool MatchNotEqual(int key) { return key != number; }

		bool MatchGreater(int key) { return key > number; }

		bool MatchGreaterOrEqual(int key) { return key >= number; }

		bool MatchLess(int key) { return key < number; }

		bool MatchLessOrEqual(int key) { return key <= number; }


		/// <summary>
		/// Types of string match operation to perform.
		/// </summary>
		public enum Types {
			Equal = 0,
			NotEqual,
			/// <summary>
			/// Index key > number
			/// </summary>
			Greater,
			/// <summary>
			/// Index key >= number
			/// </summary>
			GreaterOrEqual,
			/// <summary>
			/// Index key < number
			/// </summary>
			Less,
			/// <summary>
			/// Index key <= number
			/// </summary>
			LessOrEqual
		}
	}
}

