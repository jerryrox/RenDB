using System;
using RenDBCore.Internal;

namespace RenDBCore
{
	public class BaseModel<K> : IModel<K> {

		protected Guid id;


		/// <summary>
		/// Returns the unique identifier of this model.
		/// </summary>
		public Guid Id {
			get { return id; }
		}

		public ISerializer<K> Serializer {
			get {
				throw new NotImplementedException ();
			}
		}


		public ISerializer<K> GetFieldSerializer<K> (string field)
		{
			throw new NotImplementedException ();
		}

		public System.Collections.Generic.IEnumerator<string> GetUniqueFields ()
		{
			throw new NotImplementedException ();
		}

		public System.Collections.Generic.IEnumerator<string> GetAllFields ()
		{
			throw new NotImplementedException ();
		}

		public object GetFieldData (string field)
		{
			throw new NotImplementedException ();
		}


		public BaseModel()
		{

		}
	}
}

