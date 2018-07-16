using UnityEngine;
using Renko.Data;
using System.IO;
using RenDBCore;
using Renko.Matching;

using Guid = System.Guid;

public class RenDBTester : MonoBehaviour {

	public int InputI;
	public string InputS;

	private IDatabase<TestModel> database;


	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Alpha1)) {
			// Initialize database
			database = RenDB.InitDiskDatabase(
				"TestDB",
				new TestSerializer(),
				Path.Combine(Application.dataPath, "TestDB"),
				128
			);

			// Instantiate serializers
			var strSer = new StringSerializer();
			var intSer = new IntSerializer();
			// Initialize indexes
			database.RegisterIndex("n", "nickname", strSer, 4096, 256);
			database.RegisterIndex("m", "message", strSer, 4096, 32);
			database.RegisterIndex("s", "score", intSer, 512, 128);
			database.RegisterIndex("l", "level", intSer, 512, 128);
			Debug.Log("Initialized new test db.");
		}

		if(Input.GetKeyDown(KeyCode.Alpha2)) {
			TestModel model = new TestModel() {
				Id = Guid.NewGuid(),
				Level = Random.Range(0, 100),
				Score = Random.Range(10000, 50000),
				Message = "This is my test message " + Random.Range(100000, 1000000),
				Nickname = "Player " + Random.Range(100, 1000)
			};

			bool success = database.Insert(model);
			Debug.Log("Inserted new model: " + new JsonData(model).ToString());
			Debug.LogWarning("Success: " + success);
		}

		if(Input.GetKeyDown(KeyCode.Alpha3)) {
			TestModel updateModel = new TestModel() {
				Id = new Guid(InputS),
				Level = InputI,
				Score = InputI * 1000,
				Message = "Updated message " + InputI,
				Nickname = "Player " + InputI
			};

			bool success = database.Update(updateModel);
			Debug.Log("Updated model to: " + new JsonData(updateModel).ToString());
			Debug.LogWarning("Success: " + success);
		}

		if(Input.GetKeyDown(KeyCode.Alpha4)) {
			bool success = database.Delete(new Guid(InputS));
			Debug.Log("Deleted model with guid: " + InputS);
			Debug.LogWarning("Success: " + success);
		}

		if(Input.GetKeyDown(KeyCode.Alpha5)) {
			Debug.Log("Found model: " + new JsonData(database.Find(new Guid(InputS))).ToString());
		}

		if(Input.GetKeyDown(KeyCode.Alpha6)) {
			var query = database.Find()
				.Sort(Input.GetKey(KeyCode.LeftShift))
				.FindExact("nickname", "Player " + InputI)
				.GetAll();

			Debug.LogWarning("Getting results");
			while(query.MoveNext()) {
				Debug.Log("Found: " + new JsonData(query.Current).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha7)) {
			var query = database.Find()
				.Sort(Input.GetKey(KeyCode.LeftShift));

			if(string.IsNullOrEmpty(InputS))
				query.FindAll();
			else
				query.FindAll<string>(InputS);
			
			query.Skip(InputI);

			if(Input.GetKey(KeyCode.Z)) {
				Debug.LogWarning("Getting results");
				var a = query.GetAll();
				while(a.MoveNext()) {
					Debug.Log("Found: " + new JsonData(a.Current).ToString());
				}
			}
			else if(Input.GetKey(KeyCode.X)) {
				Debug.LogWarning("Getting results");
				var a = query.GetRange(10);
				while(a.MoveNext()) {
					Debug.Log("Found: " + new JsonData(a.Current).ToString());
				}
			}
			else {
				Debug.LogWarning("Getting results");
				var a = query.GetFirst();
				Debug.Log("Found: " + new JsonData(a).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha8)) {
			var levelMatcher = new MatchGroup<int>();
			levelMatcher.AddMatcher(new IntMatcher(0, IntMatcher.Types.GreaterOrEqual));
			levelMatcher.AddMatcher(new IntMatcher(50, IntMatcher.Types.LessOrEqual));

			var query = database.Find()
				.Sort(Input.GetKey(KeyCode.LeftShift))
				.FindMatch("level", levelMatcher)
				.GetAll();

			Debug.LogWarning("Getting results");
			while(query.MoveNext()) {
				Debug.Log("Found: " + new JsonData(query.Current).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha9)) {
			database.Dispose();
			Debug.Log("Disposed");
		}
	}
}
