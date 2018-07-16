using System;
using System.IO;
using System.Text;
using RenDBCore;
using Renko.Data;

public class TestSerializer : ISerializer<TestModel> {

	public bool IsFixedSize {
		get { return false; }
	}

	public int Length {
		get { return 0; }
	}


	public byte[] Serialize (TestModel value)
	{
		var jsonData = Encoding.UTF8.GetBytes(new JsonData(value).ToString());

		// Write json data
		byte[] buffer = new byte[jsonData.Length];
		Buffer.BlockCopy(jsonData, 0, buffer, 0, jsonData.Length);

		return buffer;
	}

	public TestModel Deserialize (byte[] data, int offset, int length)
	{
		// Parse json and return
		return Json.Parse<TestModel>(
			Encoding.UTF8.GetString(data),
			new TestModel()
		);
	}
}
