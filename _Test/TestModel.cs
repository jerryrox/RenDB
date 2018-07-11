using System;
using System.Collections.Generic;
using Renko.Data;

public class TestModel : IJsonable {
	
	public readonly Guid Id;

	public int Age;
	public string Name;


	public Tuple<int, string> SecondaryIndex {
		get { return new Tuple<int, string>(Age, Name); }
	}


	public TestModel(int age, string name)
	{
		this.Id = Guid.NewGuid();
		this.Age = age;
		this.Name = name;
	}

	public TestModel (Guid id, int age, string name)
	{
		this.Id = id;
		this.Age = age;
		this.Name = name;
	}

	public JsonObject ToJsonObject ()
	{
		JsonObject json = new JsonObject();
		json["Id"] = Id.ToString();
		json["Age"] = Age;
		json["Name"] = Name;
		return json;
	}

	public void FromJsonObject (JsonObject json)
	{
		throw new NotImplementedException ();
	}
}
