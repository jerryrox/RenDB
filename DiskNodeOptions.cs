﻿using System;

namespace RenDBCore
{
	/// <summary>
	/// Options class for initializing DiskTreeNodeManager object.
	/// </summary>
	public class DiskNodeOptions {
		
		public int WeakNodeCleanInterval = 1000;
		public int MaxStrongNode = 200;
		public ushort MinEntriesPerNode = 36;


		public DiskNodeOptions (int weakNodeCleanInterval = 1000, int maxStrongNode = 200,
			ushort minEntriesPerNode = 32)
		{
			WeakNodeCleanInterval = weakNodeCleanInterval;
			MaxStrongNode = maxStrongNode;
			MinEntriesPerNode = minEntriesPerNode;
		}
	}
}

