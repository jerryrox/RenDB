using UnityEngine;
using Renko.Data;
using System;
using RenDBCore;

using Guid = System.Guid;

public class RenDBTester : MonoBehaviour {

	public int InputI;
	public string InputS;

	private TestDatabase testDb;


	void Awake()
	{
		JsonAdaptor.Register(
			typeof(Tuple<Guid, uint>),
			delegate(object value) {
				var tuple = value as Tuple<Guid, uint>;
				JsonObject json = new JsonObject();
				json["Item1"] = tuple.Item1.ToString();
				json["Item2"] = tuple.Item2;
				return json;
			},
			null
		);
		JsonAdaptor.Register(
			typeof(Tuple<int, string>),
			delegate(object value) {
				var tuple = value as Tuple<int, string>;
				JsonObject json = new JsonObject();
				json["Item1"] = tuple.Item1;
				json["Item2"] = tuple.Item2;
				return json;
			},
			null
		);
	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Alpha1)) {
			testDb = new TestDatabase();
			Debug.Log("Initialized new test db.");
		}

		if(Input.GetKeyDown(KeyCode.Alpha2)) {
			TestModel model = new TestModel(InputI, "Test #" + InputI);

			testDb.Insert(model);
			Debug.Log("Inserted new model: " + new JsonData(model).ToString());
		}

		if(Input.GetKeyDown(KeyCode.Alpha3)) {
			Guid guid = new Guid(InputS);
			TestModel updateModel = new TestModel(guid, InputI, "Updated model to #" + InputI);

			testDb.Update(updateModel);
			Debug.Log("Updated model to: " + new JsonData(updateModel).ToString());
		}

		if(Input.GetKeyDown(KeyCode.Alpha4)) {
			testDb.Delete(new Guid(InputS));
			Debug.Log("Deleted model with guid: " + InputS);
		}

		if(Input.GetKeyDown(KeyCode.Alpha5)) {
			Debug.Log("Found model: " + new JsonData(testDb.Find(new Guid(InputS))).ToString());
		}

		if(Input.GetKeyDown(KeyCode.Alpha6)) {
			var results = testDb.Find(InputI, InputS);
			Debug.LogWarning("Getting results");
			foreach(var model in results) {
				Debug.Log("Found: " + new JsonData(model).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha7)) {
			var results = testDb.FindAll(Input.GetKey(KeyCode.LeftShift));
			Debug.LogWarning("Getting results");
			foreach(var model in results)
				Debug.Log("Found: " + new JsonData(model).ToString());
		}

		if(Input.GetKeyDown(KeyCode.Alpha8)) {
			MatchGroup<int> ageMatcher = new MatchGroup<int>(); {
				ageMatcher.AddMatcher(new IntMatcher(0, IntMatcher.Types.GreaterOrEqual));
				ageMatcher.AddMatcher(new IntMatcher(5, IntMatcher.Types.LessOrEqual));
			}
			StringMatcher nameMatcher = new StringMatcher("updated", StringMatcher.Types.StartWith, true);

			var results = testDb.Find(ageMatcher, nameMatcher);
			Debug.LogWarning("Getting results");
			foreach(var model in results)
				Debug.Log("Found: " + new JsonData(model).ToString());
		}

		if(Input.GetKeyDown(KeyCode.Alpha9)) {
			testDb.Dispose();
			Debug.Log("Disposed");
		}
	}
}
