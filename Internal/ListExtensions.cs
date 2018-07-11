using System;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	public static class ListExtensions {

		public static void RemoveRange<T>(this List<T> context, int from)
		{
			context.RemoveRange(from, context.Count - from);
		}

		public static int BinarySearchFirst<T>(this List<T> context, T value, IComparer<T> comparer)
		{
			if(comparer == null)
				throw new ArgumentNullException("comparer");

			// Perform the binary search first
			var result = context.BinarySearch(value, comparer);
			if(result >= 1) {
				// Find the first entry in multiple occurrences with same value.
				for(int i=(result-1); i>=0; i--) {
					if(comparer.Compare(context[i], value) != 0)
						break;

					result = i;
				}
			}
			return result;
		}

		public static int BinarySearchLast<T>(this List<T> context, T value, IComparer<T> comparer)
		{
			if(comparer == null)
				throw new ArgumentNullException("comparer");

			// Perform the binary search first
			var result = context.BinarySearch(value, comparer);
			if(result >= 0 && result+1 < context.Count) {
				// Find the last entry in multiple occurrences with same value.
				for(int i=result+1; i<context.Count; i++) {
					if(comparer.Compare(context[i], value) != 0)
						break;

					result = i;
				}
			}
			return result;
		}
	}
}

