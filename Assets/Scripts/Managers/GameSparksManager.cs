using UnityEngine;
using System.Collections.Generic;
using GameSparks.Api;
using GameSparks.Api.Messages;

public class GameSparksManager : MonoBehaviour
{
	/// <summary>The GameSparks Manager singleton</summary>
	public static GameSparksManager Current { get; private set; }
	public event System.Action<AchievementEarnedMessage> AchievementEarned;
	List<TopScore> _topScores = new List<TopScore>();
	string _currentName;

	void Awake()
	{
		if (Current == null) // check to see if the instance has a reference
		{
			Current = this; // if not, give it a reference to this class...
			DontDestroyOnLoad(this.gameObject); // and make this object persistent as we load new scenes
		}
		else // if we already have a reference then remove the extra manager from the scene
		{
			Destroy(this.gameObject);
		}
	}

	public void AuthenticatePlayer(string name)
	{
		Debug.Log($"Attempting to authenticate {name}");
		new GameSparks.Api.Requests.DeviceAuthenticationRequest().SetDisplayName(name).Send((response) => {
			if (!response.HasErrors)
			{
				RegisterForMessages();
				_currentName = response.DisplayName;
				Debug.Log($"Device Authenticated... {response.DisplayName}");
			}
			else
			{
				Debug.Log("Error Authenticating Device...");
			}
		});
	}
	public void SetPlayerName(string name)
	{
		if (name == _currentName)
			return;
		new GameSparks.Api.Requests.ChangeUserDetailsRequest().SetDisplayName(name).Send((r) =>
		{
			if (!r.HasErrors)
				Debug.Log($"Name Changed To... {name}");
			else
				Debug.Log($"Unable to update display name.");
		});
	}
	private void RegisterForMessages()
	{
		AchievementEarnedMessage.Listener = ((AchievementEarnedMessage message) => { Debug.Log(message.Summary); AchievementEarned?.Invoke(message); });
	}
	public void PostHighScore(int newScore)
	{
		new GameSparks.Api.Requests.LogEventRequest().SetEventKey("SUBMIT_SCORE").SetEventAttribute("SCORE", newScore.ToString()).Send((response) => {
			if (!response.HasErrors)
			{
				Debug.Log("Score Posted Successfully...");
			}
			else
			{
				Debug.Log("Error Posting Score...");
				Debug.Log(response.Errors.JSON);
			}
		});
	}
	public List<TopScore> UpdateHighScores()
	{
		new GameSparks.Api.Requests.LeaderboardDataRequest().SetLeaderboardShortCode("SCORE_LEADERBOARD").SetEntryCount(10).Send((response) => {
			if (!response.HasErrors)
			{
				Debug.Log("Found Leaderboard Data...");
				_topScores.Clear();

				foreach (GameSparks.Api.Responses.LeaderboardDataResponse._LeaderboardData entry in response.Data)
				{
					TopScore topScore = new TopScore();

					int rank = (int)entry.Rank;
					string playerName = entry.UserName;
					topScore.name = playerName;
					string score = entry.JSONData["SCORE"].ToString();
					topScore.score = int.Parse(score);

					_topScores.Add(topScore);

					Debug.Log("Rank:" + rank + " Name:" + playerName + " \n Score:" + score);
				}
			}
			else
			{
				Debug.Log("Error Retrieving Leaderboard Data...");
				Debug.Log(response.Errors.JSON);
			}
		});

		return _topScores;
	}

	public void ResetHighScores()
	{
		StartCoroutine(clearHighScores());
	}

	System.Collections.IEnumerator clearHighScores()
	{
		bool set = false;
		float time = 1000;
		string[] userNames = { "Jeb", "Val", "Bill", "Bob", "Steve", "The Engineer", "Alex", "David", "Dan", "Jesse" };

		new GameSparks.Api.Requests.LogEventRequest().SetEventKey("CLEAR_LEADERBOARD").Send((response) => {
			if (!response.HasErrors)
			{
				Debug.Log("Leaderboard Cleared");
			}
			else
			{
				Debug.Log("Failed to clear Leaderboard");
				Debug.Log(response.Errors.JSON);
			}
			set = true;
		});

		while(set == false && time > 0)
		{
			time -= Time.deltaTime;
			yield return new WaitForSeconds(0.1f);
		}
		set = false;
		time = 1000;
		
		for (int i = 0; i < userNames.Length; i++)
		{
			var n = userNames[i];
			new GameSparks.Api.Requests.RegistrationRequest().SetUserName(n).SetPassword(n).SetDisplayName(n).Send((response) =>
			{
				if (response.HasErrors)
				{
					Debug.LogError($"Failed to create user {n}\n{response.Errors.JSON}");
				}
				set = true;
			});

			while (set == false && time > 0)
			{
				time -= Time.deltaTime;
				yield return new WaitForSeconds(0.1f);
			}
			set = false;
			time = 1000;

			new GameSparks.Api.Requests.AuthenticationRequest().SetUserName(n).SetPassword(n).Send((r2) =>
			{
				if (!r2.HasErrors)
				{
					PostHighScore(Mathf.Max(100, 10000 - (i * 1000)));
				}
				else
				{
					Debug.LogError($"Failed to log in to user {n}\n{r2.Errors.JSON}");
				}
				set = true;
			});

			while (set == false && time > 0)
			{
				time -= Time.deltaTime;
				yield return new WaitForSeconds(0.1f);
			}
			set = false;
			time = 1000;
		}
		AuthenticatePlayer(_currentName);
	}
}
