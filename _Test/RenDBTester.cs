using UnityEngine;
using Renko.Data;
using System.IO;
using RenDBCore;

using Guid = System.Guid;

public class RenDBTester : MonoBehaviour {

	public int InputI;
	public string InputS;
	public string InputS2;
	public string InputS3;

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

			// Initialize indexes
			var indexes = database.Indexes;
			indexes.Register("n", "nickname", StringSerializer.Default, 4096, 256);
			indexes.Register("m", "message", StringSerializer.Default, 4096, 32);
			indexes.Register("s", "score", IntSerializer.Default, 512, 128);
			indexes.Register("l", "level", IntSerializer.Default, 512, 128);
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
			var query = database.Query()
				.Sort(Input.GetKey(KeyCode.LeftShift))
				.Find("nickname", "Player " + InputI)
				.GetAll();

			Debug.LogWarning("Getting results");
			while(query.MoveNext()) {
				Debug.Log("Found: " + new JsonData(query.Current).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha7)) {
			var query = database.Query()
				.Sort(Input.GetKey(KeyCode.LeftShift));

			if(string.IsNullOrEmpty(InputS))
				query.Find();
			else
				query.Find<string>(InputS);
			
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
			var query = database.Query()
				.Sort(Input.GetKey(KeyCode.LeftShift))
				.Find("level", delegate(int level) {
					return level >= 0 && level < 50;
				})
				.GetAll();

			Debug.LogWarning("Getting results");
			while(query.MoveNext()) {
				Debug.Log("Found: " + new JsonData(query.Current).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha9)) {
			var query = database.Query()
				.Sort(true)
				.Find(delegate(TestModel model) {
					return model.Level < 10;
				})
				.GetAll();


			Debug.LogWarning("Getting results");
			while(query.MoveNext()) {
				Debug.Log("Found: " + new JsonData(query.Current).ToString());
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha0)) {
			if(Input.GetKey(KeyCode.Z)) {
				if(Input.GetKey(KeyCode.LeftShift)) {
					database.Indexes.HardRebuild<string>(InputS, InputS2, StringSerializer.Default);
					Debug.Log("Rebuilt index with label: " + InputS + " and field: " + InputS2 + " (Hard)");
				}
				else {
					database.Indexes.SoftRebuild<string>(InputS);
					Debug.Log("Rebuilt index with field: " + InputS + " (Soft)");
				}
			}
			else if(Input.GetKey(KeyCode.X)) {
				database.Indexes.Delete(InputS, InputS2);
				Debug.Log("Deleted index with label: " + InputS + " and field: " + InputS2);
			}
			else if(Input.GetKey(KeyCode.C)) {
				database.Indexes.RenameLabel(InputS, InputS2, InputS3, StringSerializer.Default);
				Debug.Log("Renamed index label with field: " + InputS + " and label from: " + InputS2 + " to: " + InputS3);
			}
		}

		if(Input.GetKeyDown(KeyCode.M)) {
			database.Dispose();
			Debug.Log("Disposed");
		}
	}
}
