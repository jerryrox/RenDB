using System;
using System.IO;
using System.Text;
using RenDBCore;

public class TestSerializer : ISerializer<TestModel> {

	public bool IsFixedSize {
		get { return false; }
	}

	public int Length {
		get { return 0; }
	}


	public byte[] Serialize (TestModel value)
	{
		var nameData = Encoding.UTF8.GetBytes(value.Name);
		int nameLength = nameData.Length;

		byte[] buffer = new byte[16 + 4 + 4 + nameLength];
		int bufferOffset = 0;

		// Write guid
		Buffer.BlockCopy(value.Id.ToByteArray(), 0, buffer, bufferOffset, 16);
		bufferOffset += 16;

		// Write age
		BufferHelper.WriteBuffer(value.Age, buffer, bufferOffset);
		bufferOffset += 4;

		// Write name length
		BufferHelper.WriteBuffer(nameLength, buffer, bufferOffset);
		bufferOffset += 4;

		// Write name
		Buffer.BlockCopy(nameData, 0, buffer, bufferOffset, nameLength);

		return buffer;
	}

	public TestModel Deserialize (byte[] data, int offset, int length)
	{
		int bufferOffset = offset;

		// Read guid
		Guid id = BufferHelper.ReadGuid(data, bufferOffset);
		bufferOffset += 16;

		// Read age
		int age = BufferHelper.ReadInt32(data, bufferOffset);
		bufferOffset += 4;

		// Read name length
		int nameLength = BufferHelper.ReadInt32(data, bufferOffset);
		bufferOffset += 4;

		// Read name
		string name = Encoding.UTF8.GetString(data, bufferOffset, nameLength);

		return new TestModel(id, age, name);
	}
}
