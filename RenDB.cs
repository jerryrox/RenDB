using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RenDBCore.Internal;

namespace RenDBCore
{
	/// <summary>
	/// RenDB library manager.
	/// </summary>
	public static class RenDB {

		/// <summary>
		/// Initializes a new database instance with IO stream using disk.
		/// </summary>
		public static IDatabase<T> InitDiskDatabase<T>(string name, ISerializer<T> modelSerializer,
			string directory, int blockSize = 4096) where T : class, IModel<T>
		{
			return new DiskDatabase<T>(name, modelSerializer, directory, blockSize);
		}

		/// <summary>
		/// Initializes a new database instance with IO stream using memory.
		/// This database is used for temporary cases only.
		/// </summary>
		public static IDatabase<T> InitMemoryDatabase<T>(string name, ISerializer<T> modelSerializer,
			int blockSize = 4096) where T : class, IModel<T>
		{
			return new MemoryDatabase<T>(name, modelSerializer, blockSize);
		}
	}
}