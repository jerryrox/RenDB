using System;
using System.IO;
using System.Text;
using RenDBCore;

public class TestSecondaryInxSerializer : ISerializer<Tuple<int, string>> {

	public bool IsFixedSize {
		get { return false; }
	}

	public int Length {
		get { return 0; }
	}


	public byte[] Serialize (Tuple<int, string> value)
	{
		byte[] item2Data = Encoding.UTF8.GetBytes(value.Item2);
		int item2Length = item2Data.Length;

		byte[] buffer = new byte[4 + 4 + item2Length];
		int bufferOffset = 0;

		// Write item 1
		BufferHelper.WriteBuffer(value.Item1, buffer, bufferOffset);
		bufferOffset += 4;

		// Write item 2 length
		BufferHelper.WriteBuffer(item2Length, buffer, bufferOffset);
		bufferOffset += 4;

		// Write item 2
		Buffer.BlockCopy(item2Data, 0, buffer, bufferOffset, item2Length);

		return buffer;
	}

	public Tuple<int, string> Deserialize (byte[] data, int offset, int length)
	{
		int bufferOffset = offset;

		// Read item1
		int item1 = BufferHelper.ReadInt32(data, bufferOffset);
		bufferOffset += 4;

		// Read item2 length
		int item2Length = BufferHelper.ReadInt32(data, bufferOffset);
		bufferOffset += 4;

		// Read item2
		string item2 = Encoding.UTF8.GetString(data, bufferOffset, item2Length);
		var tuple = new Tuple<int, string>(item1, item2);
		return tuple;
	}
}
