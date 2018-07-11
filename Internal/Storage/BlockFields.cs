using System;

namespace RenDBCore.Internal
{
	public class BlockFields {

		public const int NextBlockId = 0;
		public const int PreviousBlockId = 1;
		public const int RecordLength = 2;
		public const int BlockContentLength = 3;
		public const int IsDeleted = 4;

		public const int TotalHeaderSize = 8 * 5;
	}
}

