using System;

namespace RenDBCore
{
	/// <summary>
	/// A matcher variant for handling IsMatch operation using a delegate method.
	/// </summary>
	public class DelegatedMatcher<K> : IMatcher<K> {

		private MatchHandler handler;

		/// <summary>
		/// Delegate for handling match outside of this class.
		/// </summary>
		public delegate bool MatchHandler(K key);


		/// <summary>
		/// Delegate function that evaluates IsMatch operation.
		/// </summary>
		public MatchHandler Function {
			get { return handler; }
			set {
				if(value == null)
					throw new ArgumentNullException("value");
				handler = value;
			}
		}


		public DelegatedMatcher (MatchHandler handler)
		{
			this.Function = handler;
		}

		/// <summary>
		/// Returns whether specified key matches with initialized condition.
		/// </summary>
		public bool IsMatch(K key)
		{
			return handler(key);
		}
	}
}

