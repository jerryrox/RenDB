using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using RenDBCore;
using RenDBCore.Internal;

public class TestDatabase : IDisposable {
	
	private Stream dbStream;
	private Stream pIndexStream;
	private Stream sIndexStream;

	private IndexTree<Guid, uint> primaryIndexes;
	private IndexTree<Tuple<int, string>, uint> secondaryIndexes;

	private RecordStorage recordStorage;
	private TestSerializer testSerializer;

	private bool isDisposed;


	public bool IsDisposed {
		get { return isDisposed; }
	}


	public TestDatabase()
	{
		string basePath = Path.Combine(Application.dataPath, "TestDB");

		if(!Directory.Exists(basePath))
			Directory.CreateDirectory(basePath);
		
		dbStream = new FileStream(Path.Combine(basePath, "Test.rdb"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 128);
		pIndexStream = new FileStream(Path.Combine(basePath, "Test.rdba"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
		sIndexStream = new FileStream(Path.Combine(basePath, "Test.rdbb"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
//		dbStream = new MemoryStream();
//		pIndexStream = new MemoryStream();
//		sIndexStream = new MemoryStream();

		recordStorage = new RecordStorage(new BlockStorage(dbStream, 128));
		primaryIndexes = new IndexTree<Guid, uint>(
			new DiskTreeNodeManager<Guid, uint>(
				new GuidSerializer(),
				new UintSerializer(),
				new RecordStorage(new BlockStorage(pIndexStream))
			)
		);
		secondaryIndexes = new IndexTree<Tuple<int, string>, uint>(
			new DiskTreeNodeManager<Tuple<int, string>, uint>(
				new TestSecondaryInxSerializer(),
				new UintSerializer(),
				new RecordStorage(new BlockStorage(sIndexStream))
			),
			true
		);

		testSerializer = new TestSerializer();
	}

	~TestDatabase()
	{
		Dispose(false);
	}

	public void Insert(TestModel model)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");
		
		uint newId = recordStorage.Create(testSerializer.Serialize(model));
		primaryIndexes.Insert(model.Id, newId);
		secondaryIndexes.Insert(model.SecondaryIndex, newId);
	}

	public void Update(TestModel model)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");

		// Find the index entry with guid
		var entry = primaryIndexes.Get(model.Id);
		if(entry == null)
			return;

		uint index = entry.Item2;

		// Get the original data
		var origData = recordStorage.Find(index);
		if(origData == null)
			return;
		var origModel = testSerializer.Deserialize(origData, 0, origData.Length);

		// Update the main database
		recordStorage.Update(index, testSerializer.Serialize(model));

		// Delete secondary index
		if(secondaryIndexes.Delete(origModel.SecondaryIndex, index)) {
			secondaryIndexes.Insert(model.SecondaryIndex, index);
		}
	}

	public bool Delete(Guid id)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");
		
		// Find index of entry for specified id
		var entry = primaryIndexes.Get(id);
		if(entry == null)
			return false;

		// Find the actual entry object
		var data = recordStorage.Find(entry.Item2);
		if(data == null)
			return false;

		uint index = entry.Item2;

		// Parse model and delete
		var model = testSerializer.Deserialize(data, 0, data.Length);
		recordStorage.Delete(index);
		primaryIndexes.Delete(id);
		secondaryIndexes.Delete(new Tuple<int, string>(model.Age, model.Name), index);
		return true;
	}

	public TestModel Find(Guid id)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");
		
		var entry = primaryIndexes.Get(id);
		if(entry == null) {
			Debug.LogWarning("A");
			return null;
		}
		var data = recordStorage.Find(entry.Item2);
		if(data == null) {
			Debug.LogWarning("B");
			return null;
		}

		return testSerializer.Deserialize(data, 0, data.Length);
	}

	public IEnumerable<TestModel> Find(int age, string name)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");
		
		var comparer = Comparer<Tuple<int, string>>.Default;
		var key = new Tuple<int, string>(age, name);

		bool isAscending = true;
		foreach(var entry in secondaryIndexes.GetExactMatch(key, isAscending)) {
			// If invalid key, break out
			int compare = comparer.Compare(entry.Item1, key);
			if(compare > 0) {
				Debug.Log("Key compare ended: " + compare);
				break;
			}

			var data = recordStorage.Find(entry.Item2);
			if(data == null) {
				Debug.Log("Data is null!");
				continue;
			}
			
			yield return testSerializer.Deserialize(data, 0, data.Length);
		}
	}

	public IEnumerable<TestModel> FindAll(bool ascending)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");
		
		foreach(var entry in primaryIndexes.GetAll(ascending)) {
			if(entry == null)
				continue;
			
			var data = recordStorage.Find(entry.Item2);
			if(data == null)
				continue;

			yield return testSerializer.Deserialize(data, 0, data.Length);
		}
	}

	public IEnumerable<TestModel> Find(IMatcher<int> ageMatcher, IMatcher<string> nameMatcher)
	{
		if(isDisposed)
			throw new ObjectDisposedException("TestDatabase");

		IMatcher<Tuple<int, string>> indexMatcher = new DelegatedMatcher<Tuple<int, string>>(
			delegate(Tuple<int, string> key) {
				return ageMatcher.IsMatch(key.Item1) && nameMatcher.IsMatch(key.Item2);
			}
		);

		foreach(var entry in secondaryIndexes.GetOptionMatch(true, indexMatcher)) {
			if(entry == null)
				continue;

			var data = recordStorage.Find(entry.Item2);
			if(data == null)
				continue;

			yield return testSerializer.Deserialize(data, 0, data.Length);
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	void Dispose(bool dispose) {
		if(dispose && !isDisposed) {
			isDisposed = true;

			dbStream.Dispose();
			pIndexStream.Dispose();
			sIndexStream.Dispose();
		}
	}
}
