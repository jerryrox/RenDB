using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a model used in RenDB databases.
	/// </summary>
	public interface IModel<T> {

		/// <summary>
		/// Returns the unique identifier of this model.
		/// </summary>
		Guid Id { get; }

		ISerializer<K> GetFieldSerializer<K>(string field);

		/// <summary>
		/// Returns all unique and non-unique fields in this model.
		/// </summary>
		IEnumerator<string> GetAllFields();

		object GetFieldData(string field);
	}
}

