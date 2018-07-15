using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore
{
	public class DatabaseResult<T> : IEnumerable<T> {



		public IEnumerator<T> GetEnumerator ()
		{
			throw new NotImplementedException ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator();
		}
	}
}

