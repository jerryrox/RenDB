using System;
using System.Collections.Generic;
using Renko.Data;
using RenDBCore;

public class TestModel : BaseModel<TestModel>, IJsonable {

	public string Nickname;
	public string Message;
	public int Score;
	public int Level;

	[JsonAllowSerialize]
	private Guid id;

	/// <summary>
	/// Returns the unique identifier of this model.
	/// </summary>
	public override Guid Id {
		get { return id; }
		set { id = value; }
	}


	public TestModel() : base()
	{
		// Setup fields
		RegisterField("nickname", () => Nickname);
		RegisterField("message", () => Message);
		RegisterField("score", () => Score);
		RegisterField("level", () => Level);
	}

	public JsonObject ToJsonObject ()
	{
		JsonObject json = new JsonObject();
		json["Nickname"] = Nickname;
		json["Message"] = Message;
		json["Score"] = Score;
		json["Level"] = Level;
		json["_id"] = id.ToString();
		return json;
	}

	public void FromJsonObject (JsonObject json)
	{
		Nickname = json["Nickname"].AsString();
		Message = json["Message"].AsString();
		Score = json["Score"].AsInt();
		Level = json["Level"].AsInt();
		id = new Guid(json["_id"].AsString());
	}
}
