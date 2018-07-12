using System;
using UnityEngine;
using RenDBCore;

public class MatcherTest : MonoBehaviour {
	
	private MatchQuery<string> matchQuery;
	private MatchGroup<string> matchGroup;


	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Alpha1)) {
			Debug.Log("Case 1");
			ClearMatch();
			if(Input.GetKey(KeyCode.LeftShift)) {
				InitMatcher1();
			}
			else if(Input.GetKey(KeyCode.RightShift)) {
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha2)) {
			Debug.Log("Case 2");
			ClearMatch();
			if(Input.GetKey(KeyCode.LeftShift)) {
				InitMatcher2();
			}
			else if(Input.GetKey(KeyCode.RightShift)) {
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha3)) {
			Debug.Log("Case 3");
			ClearMatch();
			if(Input.GetKey(KeyCode.LeftShift)) {
				InitMatcher3();
			}
			else if(Input.GetKey(KeyCode.RightShift)) {
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha4)) {
			Debug.Log("Case 4");
			ClearMatch();
			if(Input.GetKey(KeyCode.LeftShift)) {
				InitMatcher4();
			}
			else if(Input.GetKey(KeyCode.RightShift)) {
			}
		}

		if(Input.GetKeyDown(KeyCode.Alpha5)) {
			Debug.Log("Case 5");
			ClearMatch();
			if(Input.GetKey(KeyCode.LeftShift)) {
				InitMatcher5();
			}
			else if(Input.GetKey(KeyCode.RightShift)) {
			}
		}

		if(Input.GetKeyDown(KeyCode.Q)) {
			Debug.Log(
				new StringMatcher("HI_", StringMatcher.Types.StartWith).IsMatch("HI_-_IH")
			);
			Debug.Log(
				new StringMatcher("_IH", StringMatcher.Types.EndWith).IsMatch("HI_-_IH")
			);
			Debug.Log(
				new StringMatcher("HI_-_IH", StringMatcher.Types.Equal).IsMatch("HI_-_IH")
			);
			Debug.Log(
				new StringMatcher("_-_", StringMatcher.Types.Contain).IsMatch("HI_-_IH")
			);
		}
	}

	void InitMatcher1()
	{
		// Nothing
		matchQuery = new MatchQuery<string>();

		// Should return false
		DoCheck("Blah_-_blaH");
	}

	void InitMatcher2()
	{
		// 1 group, empty
		matchQuery = new MatchQuery<string>();
		{
			var group1 = matchQuery.AddMatchGroup();
		}

		// Should return true
		DoCheck("Blah_-_blaH");
	}

	void InitMatcher3()
	{
		// 1 group, 1 matcher
		matchQuery = new MatchQuery<string>(); {
			var group1 = matchQuery.AddMatchGroup(); {
				group1.AddMatcher(new StringMatcher("A", StringMatcher.Types.Contain));
			}
		}

		// Should return false
		DoCheck("Blah_-_blaH");
	}

	void InitMatcher4()
	{
		// 1 group, 2 matchers
		matchQuery = new MatchQuery<string>(); {
			var group1 = matchQuery.AddMatchGroup(); {
				group1.AddMatcher(new StringMatcher("a", StringMatcher.Types.Contain));
				group1.AddMatcher(new StringMatcher("H", StringMatcher.Types.EndWith));
			}
		}

		// Should return true
		DoCheck("Blah_-_blaH");
	}

	void InitMatcher5()
	{
		// 1 group, 2 matchers
		// 1 group, 1 matcher
		matchQuery = new MatchQuery<string>(); {
			var group1 = matchQuery.AddMatchGroup(); {
				group1.AddMatcher(new StringMatcher("_-_", StringMatcher.Types.Contain));
				group1.AddMatcher(new StringMatcher("blah", StringMatcher.Types.StartWith));
			}
			var group2 = matchQuery.AddMatchGroup(); {
				group2.AddMatcher(new RegexMatcher("^Blah_-_blaHH$"));
			}
		}

		// Should return false
		DoCheck("Blah_-_blaH");
	}

	void ClearMatch()
	{
		matchQuery = null;
		matchGroup = null;
	}

	void DoCheck(string key)
	{
		if(matchQuery != null) {
			Debug.LogWarningFormat(
				"DoCheck - matchQuery.IsMatch({0}) == {1}",
				key, matchQuery.IsMatch(key)
			);
		}
		else if(matchGroup != null) {
			Debug.LogWarningFormat(
				"DoCheck - matchGroup.IsMatch({0}) == {1}",
				key, matchGroup.IsMatch(key)
			);
		}
	}
}
