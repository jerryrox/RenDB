using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore
{
	/// <summary>
	/// Interface of a model used in RenDB databases.
	/// </summary>
	public interface IModel<T> {

		/// <summary>
		/// Returns the unique identifier of this model.
		/// </summary>
		Guid Id { get; }


		/// <summary>
		/// Returns all fields in this model.
		/// </summary>
		IEnumerator<string> GetAllFields();

		/// <summary>
		/// Returns the field data associated with specified field name.
		/// </summary>
		object GetFieldData(string field);
	}
}

