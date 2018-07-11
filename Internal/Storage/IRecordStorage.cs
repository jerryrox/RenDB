using System;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Interface of a storage designed to function on top of a BlockStorage to support saving data with variable
	/// lengths using numerous blocks of equal size.
	/// </summary>
	public interface IRecordStorage {

		/// <summary>
		/// Creates a new record with empty data.
		/// </summary>
		uint Create();

		/// <summary>
		/// Creates a new record with empty data and specified headers.
		/// </summary>
		uint Create(long nextBlockId, long prevBlockId, long recordLength, long blockContentSize, long isDeleted);

		/// <summary>
		/// Creates a new record with specified data.
		/// </summary>
		uint Create(byte[] data);

		/// <summary>
		/// Creates a new record with specified function that invokes after a new record is created.
		/// </summary>
		uint Create(Func<uint, byte[]> dataProvider);

		/// <summary>
		/// Updates the record of specified id with data.
		/// </summary>
		void Update(uint recordId, byte[] data);

		/// <summary>
		/// Deletes the record of specified id.
		/// </summary>
		void Delete(uint recordId);

		/// <summary>
		/// Finds a record of specified id and returns its data.
		/// </summary>
		byte[] Find(uint recordId);

		/// <summary>
		/// Returns a list of all block id flagged as deleted.
		/// </summary>
		List<uint> GetDeletedId();
	}
}