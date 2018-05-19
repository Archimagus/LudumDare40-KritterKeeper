using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
	// In game canvases (assigned in inspector)
	[SerializeField] private GameObject _hud;
	[SerializeField] private GameObject _pauseMenu;
	[SerializeField] private GameObject _gameOverCanvas;
	[SerializeField] private GameObject _transitionCanvas;
	[SerializeField] private NotificationArea _notificationArea;

	[SerializeField] private GameObject _failuresIconPanel;
	[SerializeField] private TextMeshProUGUI _scoreUI;
	[SerializeField] private TextMeshProUGUI _finalScore;

	[SerializeField] private GameObject _failureUIPrefab;

	[SerializeField] private Image _transitionScreen;

	private PlayerManager _playerManager;

	public float TransitionTime = 3;

	private string _sceneName;

	private Image _transition;

	private AudioSource _sfxSource;

	[SerializeField] private AudioClip _play;
	[SerializeField] private AudioClip _exit;
	[SerializeField] private AudioClip _hover;
	[SerializeField] private List<AudioClip> _clickSounds;

	private List<TopScore> _topScoresData;
	private List<TopScore> _globalTopScoresData;

	// Assigned in inspector
	[SerializeField] private List<ScoreUI> _topScores;
	[SerializeField] private List<ScoreUI> _globalTopScores;

	private bool _checkAgainstHighScores = false;
	private bool _newHighScore = false;
	private string _playerName;

	// Assigned in inspector
	[SerializeField] private List<ScoreTabs> _scoreTabs;
	[SerializeField] private TextMeshProUGUI _currentScoreTabText;
	private int _currentScoreTab = 0;

	// Assigned in inspector
	[SerializeField] private Color _highlightedRowColor;

    public bool paused = false;

	void Start ()
	{
		_sfxSource = GetComponent<AudioSource>();

		_playerManager = GameManager.Current.PlayerManager;

		_sceneName = SceneManager.GetActiveScene().name;

		_transition = _transitionCanvas.GetComponentInChildren<Image>();

		_playerName = PlayerPrefs.GetString("PlayerName", "PlayerName");

		_globalTopScoresData = new List<TopScore>();

		GameSparksManager.Current.AchievementEarned += AchievementEarned;
	}

	private void AchievementEarned(GameSparks.Api.Messages.AchievementEarnedMessage obj)
	{
		NotificationArea.Show(obj.AchievementName);
	}

	void Update()
	{
		if (_hud.activeInHierarchy)
		{
			_scoreUI.text = _playerManager.Score.ToString();
		}
		if (Input.GetKeyDown(KeyCode.Escape) && !_playerManager.GameOver)
		{
            paused = true;
            Time.timeScale = 0;
			_pauseMenu.SetActive(true);
		}

		if (_playerManager.GameOver)
		{
			if (!_checkAgainstHighScores)
			{
				_globalTopScoresData = GameSparksManager.Current.UpdateHighScores();
				_checkAgainstHighScores = true;
				CheckHighScores(_playerManager.Score);
			}
		}


	}

	public PostProcessProfile _gameOverProfile;
	private PostProcessVolume _volume;

	public NotificationArea NotificationArea { get { return _notificationArea; } }

	public void ApplyGameOverEffect()
	{
		_volume = PostProcessManager.instance.QuickVolume(LayerMask.NameToLayer("CameraEffects"), 100f, _gameOverProfile.settings.ToArray());
		_volume.weight = 0;
		StartCoroutine(fadeGameOverEffect());
	}
	public void EndGameOverEffect()
	{
		RuntimeUtilities.DestroyVolume(_volume, false);
	}

	IEnumerator fadeGameOverEffect()
	{
		while ((_volume?.weight ?? 1) < 1)
		{
			_volume.weight += Time.unscaledDeltaTime * 2;
			yield return null;
		}
	}

	IEnumerator Transition(string action)
	{
		_transitionCanvas.SetActive(true);
		//_transition.gameObject.SetActive(true);

		while (_transition.fillAmount != 1)
		{
			yield return null;
			_transition.fillAmount += 0.1f;
		}

		if (action.Equals("Play"))
		{
			SceneManager.LoadScene(_sceneName);
		}
		else
		{
            MusicManager.Current.FadeTo(0);
            SceneManager.LoadScene("MainMenu");
		}

	}

	public void FadeToBlack()
	{
		_transitionScreen.CrossFadeAlpha(255, TransitionTime, true);
	}
	public void FadeIn()
	{
		_transitionScreen.CrossFadeAlpha(0, TransitionTime, true);
	}
	public void AddFailureTick()
	{
		Instantiate(_failureUIPrefab, _failuresIconPanel.transform);
	}

	public void GameOver()
	{
		_finalScore.text = _playerManager.Score.ToString();
		_gameOverCanvas.SetActive(true);
	}

	public void PlayAgain()
	{
        Time.timeScale = 1;
		_sfxSource.PlayOneShot(_play);
		StartCoroutine(Transition("Play"));
	}

	public void MainMenu()
	{
		Time.timeScale = 1;
		_sfxSource.PlayOneShot(_exit);
		StartCoroutine(Transition("MainMenu"));
	}

	public void ButtonHover()
	{
		_sfxSource.PlayOneShot(_hover);
	}

	public void Resume()
	{
		_sfxSource.PlayOneShot(_play);
		Time.timeScale = 1;
		_pauseMenu.SetActive(false);
        paused = false;
	}

	public void ChangeScoreTab(int direction)
	{
		if (direction > 0)
		{
			_sfxSource.PlayOneShot(_clickSounds[1]);
			if (_currentScoreTab < _scoreTabs.Count - 1)
			{
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(false);
				_currentScoreTab++;
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(true);
			}
			else
			{
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(false);
				_currentScoreTab = 0;
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(true);
			}
		}
		else
		{
			_sfxSource.PlayOneShot(_clickSounds[0]);
			if (_currentScoreTab > 0)
			{
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(false);
				_currentScoreTab--;
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(true);
			}
			else
			{
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(false);
				_currentScoreTab = _scoreTabs.Count - 1;
				_scoreTabs[_currentScoreTab].tabPanel.SetActive(true);
			}
		}

		_currentScoreTabText.text = _scoreTabs[_currentScoreTab].tabName;
		EventSystem.current.SetSelectedGameObject(null);
	}


	private void LoadTopScores()
	{
		if (File.Exists(Application.persistentDataPath + "/topScoresData.dat"))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/topScoresData.dat", FileMode.Open);

			ScoreData savedScores = (ScoreData)bf.Deserialize(file);
			file.Close();

			// Deletes legacy top 5 file and creates new top 10 file with default values
			if (savedScores.topScores.Count < 6)
			{
				File.Delete(Application.persistentDataPath + "/topScores.dat");
				CreateTopScoresFile();
			}

			_topScoresData = new List<TopScore>();
			for (int i = 0; i < savedScores.topScores.Count; i++)
			{
				TopScore topScore = savedScores.topScores[i];
				_topScoresData.Add(topScore);
			}
		}
		else
		{
			CreateTopScoresFile();
		}

	}


	private void SaveTopScores()
	{
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Open(Application.persistentDataPath + "/topScoresData.dat", FileMode.Open);

		ScoreData savedScores = new ScoreData();

		for (int i = 0; i < _topScoresData.Count; i++)
		{
			TopScore topScore = new TopScore();
			topScore.name = _topScoresData[i].name;
			topScore.score = _topScoresData[i].score;

			savedScores.topScores.Add(topScore);
		}

		bf.Serialize(file, savedScores);
		file.Close();
	}

	private void CreateTopScoresFile()
	{
		_topScoresData = new List<TopScore>();

		DefaultHighScores();

		BinaryFormatter bf = new BinaryFormatter();
		FileStream file;

		file = File.Create(Application.persistentDataPath + "/topScoresData.dat");
		file.Close();

		file = File.Open(Application.persistentDataPath + "/topScoresData.dat", FileMode.Open);

		ScoreData savedScores = new ScoreData();

		for (int i = 0; i < _topScoresData.Count; i++)
		{
			TopScore topScore = new TopScore();
			topScore.name = _topScoresData[i].name;
			topScore.score = _topScoresData[i].score;

			savedScores.topScores.Add(topScore);
		}

		bf.Serialize(file, savedScores);
		file.Close();
	}

	private void CheckHighScores(int finalScore)
	{
		LoadTopScores();

		for (int i = 0; i < _topScoresData.Count; i++)
		{
			if (finalScore > _topScoresData[i].score)
			{
				_topScoresData[i].name = _playerName;
				_topScoresData[i].score = finalScore;
				_newHighScore = true;

				break;
			}
		}

		PopulateTopScoreUI(finalScore);
	}

	private void PopulateTopScoreUI(int finalScore)
	{
		for (int i = 0; i < _topScoresData.Count; i++)
		{
			_topScores[i].name.text = _topScoresData[i].name;
			_topScores[i].score.text = _topScoresData[i].score.ToString();

			if (_newHighScore && int.Parse(_topScores[i].score.text) == finalScore)
			{
				_topScores[i].name.color = _highlightedRowColor;
				_topScores[i].score.color = _highlightedRowColor;
			}
		}

		for (int i = 0; i < _globalTopScoresData.Count; i++)
		{
			if (i < _globalTopScores.Count)
			{
				_globalTopScores[i].name.text = _globalTopScoresData[i].name;
				_globalTopScores[i].score.text = _globalTopScoresData[i].score.ToString();

				if (_globalTopScores[i].name.text == _playerName)
				{
					_globalTopScores[i].name.color = _highlightedRowColor;
					_globalTopScores[i].score.color = _highlightedRowColor;
				}
			}
		}

		SaveTopScores();
	}

	[System.Serializable]
	public class ScoreUI
	{
		public TextMeshProUGUI name;
		public TextMeshProUGUI score;
	}

	[System.Serializable]
	public class ScoreTabs
	{
		public string tabName;
		public GameObject tabPanel;
	}

	private void DefaultHighScores()
	{
		TopScore topScore1 = new TopScore();
		topScore1.name = "Bob";
		topScore1.score = 15000;
		_topScoresData.Add(topScore1);

		TopScore topScore2 = new TopScore();
		topScore2.name = "Susie";
		topScore2.score = 13000;
		_topScoresData.Add(topScore2);

		TopScore topScore3 = new TopScore();
		topScore3.name = "Carlos";
		topScore3.score = 11000;
		_topScoresData.Add(topScore3);

		TopScore topScore4 = new TopScore();
		topScore4.name = "Jess";
		topScore4.score = 10000;
		_topScoresData.Add(topScore4);

		TopScore topScore5 = new TopScore();
		topScore5.name = "George";
		topScore5.score = 8000;
		_topScoresData.Add(topScore5);

		TopScore topScore6 = new TopScore();
		topScore6.name = "Elaine";
		topScore6.score = 6000;
		_topScoresData.Add(topScore6);

		TopScore topScore7 = new TopScore();
		topScore7.name = "Kramer";
		topScore7.score = 4000;
		_topScoresData.Add(topScore7);

		TopScore topScore8 = new TopScore();
		topScore8.name = "Newman";
		topScore8.score = 3000;
		_topScoresData.Add(topScore8);

		TopScore topScore9 = new TopScore();
		topScore9.name = "Barbara";
		topScore9.score = 2000;
		_topScoresData.Add(topScore9);

		TopScore topScore10 = new TopScore();
		topScore10.name = "Frank";
		topScore10.score = 1000;
		_topScoresData.Add(topScore10);
	}
}
