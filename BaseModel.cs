using System;
using System.Collections;
using System.Collections.Generic;

namespace RenDBCore
{
	/// <summary>
	/// A convenience base class for implementing IModel.
	/// You may directly implement IModel instead but this class provides automatic
	/// field management for you.
	/// </summary>
	public abstract class BaseModel<T> : IModel<T> {

		/// <summary>
		/// Dictionary of fields linked with value provider functions.
		/// </summary>
		protected Dictionary<string, Func<object>> fields;


		/// <summary>
		/// Returns the unique identifier of this model.
		/// </summary>
		public abstract Guid Id { get; set; }


		public BaseModel ()
		{
			fields = new Dictionary<string, Func<object>>();
		}

		/// <summary>
		/// Creates and stores a new Guid from specified string or just make one.
		/// </summary>
		public virtual Guid CreateId(string id = null)
		{
			if(string.IsNullOrEmpty(id))
				Id = Guid.NewGuid();
			else
				Id = new Guid(id);
			return Id;
		}

		/// <summary>
		/// Returns all fields in this model.
		/// </summary>
		public IEnumerator<string> GetAllFields ()
		{
			return fields.Keys.GetEnumerator();
		}

		/// <summary>
		/// Returns the field data associated with specified field name.
		/// </summary>
		public object GetFieldData (string field)
		{
			if(fields.ContainsKey(field))
				return fields[field].Invoke();
			return null;
		}

		/// <summary>
		/// Registers the specified params to the fields dictionary.
		/// </summary>
		protected void RegisterField(string field, Func<object> provider)
		{
			fields.Add(field, provider);
		}
	}
}

