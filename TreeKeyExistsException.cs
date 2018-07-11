using System;

namespace RenDBCore
{
	public class TreeKeyExistsException : Exception {
		
		public TreeKeyExistsException() : base("A duplicate key already exists.") {}

		public TreeKeyExistsException(object key) : base(string.Format(
			"A duplicate key '{0}' already exists.",
			key.ToString()
		)) {}
	}
}

