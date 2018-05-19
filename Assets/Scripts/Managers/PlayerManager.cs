using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public enum KritterNeeds
{
	None,
	Food,
	Water,
	Care,
	Health
}
public class PlayerManager : MonoBehaviour
{
	public int Failures = 0;
	public int Score = 0;
	// Used in other classes to stop progression when true
	public bool GameOver = false;

	[SerializeField]
	private int[] _scoreMusicThresholds;

	[Space]
	[SerializeField]
	private Texture2D[] _cursorTextures;
	[SerializeField]
	private Vector2[] _cursorHotspots;

	private KritterNeeds _currentNeedMode;
	private UIManager _uiManager;
	private Kritter _kritter;

	void Start()
	{
		_uiManager = GameManager.Current.UIManager;
		MusicManager.Current.FadeTo(1);
	}

	public void SetNeedMode(KritterNeeds need)
	{
		SetNeedMode((int)need);
	}
	public void SetNeedMode(int need)
	{
		if (!GameOver)
		{
			_currentNeedMode = (KritterNeeds)need;
			if(Application.platform == RuntimePlatform.WebGLPlayer)
				Cursor.SetCursor(_cursorTextures[need], _cursorHotspots[need], CursorMode.ForceSoftware);
			else
				Cursor.SetCursor(_cursorTextures[need], _cursorHotspots[need], CursorMode.Auto);
		}
	}

	void Update()
	{
		if (!GameOver)
		{
#if UNITY_EDITOR
			if (Input.GetKeyDown(KeyCode.Space))
				AddFailure();
#endif
			if (Input.GetMouseButtonDown(1))
			{
				SetNeedMode(KritterNeeds.None);
			}
			if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0))
			{
				var hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

				foreach (var hit in hits)
				{
					if (hit.transform.CompareTag("Kritter"))
					{
						_kritter = hit.transform.GetComponent<Kritter>();
						if (_currentNeedMode == KritterNeeds.None)
						{
							_kritter.StartDrag();
							break;
						}
						else
						{
							if (_kritter.FillNeed(_currentNeedMode))
							{
								SetNeedMode(KritterNeeds.None);
								break;
							}
						}
					}
				}
			}
			if (Input.GetMouseButtonUp(0))
			{
				if (_kritter != null)
				{
					_kritter.EndDrag();
					_kritter = null;
				}
			}
		}
	}

	// Call this when we want to increase the player's score by an amount
	public void IncreaseScore(int amount)
	{
		for (int i = 0; i < _scoreMusicThresholds.Length; i++)
		{
			if (Score < _scoreMusicThresholds[i] && Score + amount >= _scoreMusicThresholds[i])
			{
				MusicManager.Current.FadeTo(i + 2);
				break;
			}
		}

		Score += amount;
		GameSparksManager.Current.PostHighScore(Score);
	}

	// Call this when a kritter dies
	public void AddFailure()
	{
		Failures += 1;

		if (_uiManager != null)
		{
			_uiManager.AddFailureTick();
		}
		if (Failures > 2)
		{
			StartCoroutine(GameOverTransition());
		}
	}


	IEnumerator GameOverTransition()
	{
		SetNeedMode(KritterNeeds.None);
		Time.timeScale = float.Epsilon;
		GameOver = true;
		MusicManager.Current.FadeTo(MusicManager.Current.NumClips - 1);
		_uiManager.ApplyGameOverEffect();
		
		yield return new WaitForSecondsRealtime(2);

		_uiManager.FadeToBlack();

		yield return new WaitForSecondsRealtime(_uiManager.TransitionTime);
		_uiManager.EndGameOverEffect();
		_uiManager.GameOver();
	}

}
