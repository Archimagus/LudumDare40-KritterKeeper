using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Audio;

public class MainMenuManager : MonoBehaviour
{
	[SerializeField] private GameObject _mainPanel;
	[SerializeField] private GameObject _scoresPanel;
	[SerializeField] private GameObject _instructionsPanel;
	[SerializeField] private GameObject _optionsPanel;
	[SerializeField] private GameObject _namePanel;

	[SerializeField] private GameObject _audioPanel;
	[SerializeField] private GameObject _videoPanel;

	private GameObject _previousPanel;
	private GameObject _currentPanel;

	[SerializeField] private Slider _musicSlider;
	[SerializeField] private Slider _sfxSlider;
	[SerializeField] private AudioMixer _mixer;

	[SerializeField] private TextMeshProUGUI _currentResolutionText;
	[SerializeField] private TextMeshProUGUI _currentQualitySettingText;

	[SerializeField] private Image _transition;

	private float _musicVolume;
	private float _sfxVolume;

	private int _currentResolutionIndex = 0;
	private int _currentQualityIndex = 0;

	private UnityEngine.Resolution[] supportedResolutions;
	private List<UnityEngine.Resolution> supportedResolutionsList = new List<UnityEngine.Resolution>();

	public List<ResolutionOptions> resolutionOptionsList;
	public List<GraphicsSettings> graphicsQualityList;

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

	[SerializeField] private Text _nameText;
	[SerializeField] private InputField _inputField;
	[SerializeField] private Button _beginButton;

	// Assigned in inspector
	[SerializeField] private List<ScoreTabs> _scoreTabs;
	[SerializeField] private TextMeshProUGUI _currentScoreTabText;
	private int _currentScoreTab = 0;

	[SerializeField] private List<InstructionTabs> _instructionTabs;
	[SerializeField] private TextMeshProUGUI _currentInstructionTabText;
	private int _currentInstructionTab = 0;

	// Assigned in inspector
	[SerializeField] private Color _highlightedRowColor;

	[SerializeField] private List<TextMeshProUGUI> _resolutionUIItems;

	[SerializeField] private GameObject _exitButton;

	public float MusicVolume
	{
		get
		{
			return _musicVolume;
		}

		set
		{
			if (Mathf.Abs(_musicVolume - value) > 0.01f)
				_mixer.SetFloat("MusicVolume", LinearToDecibel(value));
			_musicVolume = value;
		}
	}

	public float SfxVolume
	{
		get
		{
			return _sfxVolume;
		}

		set
		{
			if (Mathf.Abs(_sfxVolume - value) > 0.01f)
			{
				_mixer.SetFloat("SfxVolume", LinearToDecibel(value));
				if(_audioPanel.activeInHierarchy)
					_sfxSource.PlayOneShot(_hover);
			}
			_sfxVolume = value;
		}
	}

	private static float LinearToDecibel(float lin)
	{
		if (lin <= float.Epsilon)
			return -80;
		return Mathf.Log(lin, 3) * 20;
	}

	private static float DecibelToLinear(float db)
	{
		return Mathf.Pow(3, db / 20);
	}
	private void Awake()
	{
		
		resolutionOptionsList = new List<ResolutionOptions>();
		_sfxSource = GetComponent<AudioSource>();
	}
	private void Start()
	{
		LoadSettings();

		if (Application.platform == RuntimePlatform.WebGLPlayer)
		{
			for(int i = 0; i < _resolutionUIItems.Count; i++)
			{
				_resolutionUIItems[i].gameObject.SetActive(false);
				_exitButton.SetActive(false);
			}
		}
		else
		{
			CreateSupportedResolutionOptions();
		}
			

		LoadTopScores();
		Invoke("AuthenticatePlayer", 0.1f);

		StartCoroutine(OpenScreen());
	}
	void AuthenticatePlayer()
	{
		GameSparksManager.Current.AuthenticatePlayer(_nameText.text);
		_globalTopScoresData = GameSparksManager.Current.UpdateHighScores();
	}
	void Update ()
	{
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.X))
			if (Input.GetKey(KeyCode.LeftControl))
				if (Input.GetKey(KeyCode.LeftAlt))
					GameSparksManager.Current.ResetHighScores();
#endif
		if(_audioPanel.activeInHierarchy)
		{
			MusicVolume = _musicSlider.value;
			SfxVolume = _sfxSlider.value;
		}
		

		if(_namePanel.activeInHierarchy)
		{
			if (_nameText.text != null & _nameText.text != "")
			{
				_beginButton.interactable = true;
			}
			else
			{
				_beginButton.interactable = false;
			}
		}
	}

	private void CreateSupportedResolutionOptions()
	{
		supportedResolutions = Screen.resolutions;
		for (int i = 0; i < supportedResolutions.Length; i++)
		{
			if (supportedResolutions[i].refreshRate >= 29)
			{
				ResolutionOptions resolutionOption = new ResolutionOptions();
				resolutionOption.width = supportedResolutions[i].width;
				resolutionOption.height = supportedResolutions[i].height;
				resolutionOption.refreshRate = supportedResolutions[i].refreshRate;

				resolutionOptionsList.Add(resolutionOption);
			}
		}
	}

	public void EnterName()
	{
		_sfxSource.PlayOneShot(_clickSounds[1]);
		_mainPanel.SetActive(false);

		_currentPanel = _namePanel;
		_currentPanel.SetActive(true);
	}

	public void PlayGame()
	{
		PlayerPrefs.SetString("PlayerName", _nameText.text);
		GameSparksManager.Current.SetPlayerName(_nameText.text);
		_sfxSource.PlayOneShot(_play);
		StartCoroutine(Play());
	}

	public void Scores()
	{
		PopulateTopScoreUI();
		_sfxSource.PlayOneShot(_clickSounds[1]);
		_mainPanel.SetActive(false);
		_currentPanel = _scoresPanel;

		_currentPanel.SetActive(true);
	}

	public void Instructions()
	{
		_sfxSource.PlayOneShot(_clickSounds[1]);
		_mainPanel.SetActive(false);
		_currentPanel = _instructionsPanel;

		_currentPanel.SetActive(true);
	}

	public void Options()
	{
		_sfxSource.PlayOneShot(_clickSounds[1]);
		_mainPanel.SetActive(false);
		_currentPanel = _optionsPanel;

		_currentPanel.SetActive(true);
	}

	public void AudioOptions()
	{
		_sfxSource.PlayOneShot(_clickSounds[1]);
		_previousPanel = _optionsPanel;
		_currentPanel = _audioPanel;

		DeactivePreviousPanel(_previousPanel);
		_currentPanel.SetActive(true);
	}

	public void VideoOptions()
	{
		_sfxSource.PlayOneShot(_clickSounds[1]);
		_previousPanel = _optionsPanel;
		_currentPanel = _videoPanel;

		DeactivePreviousPanel(_previousPanel);
		_currentPanel.SetActive(true);
	}

	public void GoBack(GameObject returnToPanel)
	{
		_sfxSource.PlayOneShot(_clickSounds[0]);
		SaveSettings();
		ActivatePreviousPanel(returnToPanel);
	}

	public void BackToMain()
	{
		_sfxSource.PlayOneShot(_clickSounds[0]);
		_currentPanel.SetActive(false);
		_mainPanel.SetActive(true);

		_previousPanel = null;
		_currentPanel = null;
	}

	public void Exit()
	{
		_sfxSource.PlayOneShot(_exit);
		StartCoroutine(ExitGame());
	}

	private void DeactivePreviousPanel(GameObject panelToDeactivate)
	{
		if(panelToDeactivate.Equals(_audioPanel) || panelToDeactivate.Equals(_videoPanel))
		{
			SaveSettings();
		}

		panelToDeactivate.SetActive(false);
	}

	private void ActivatePreviousPanel(GameObject returnToPanel)
	{
		_currentPanel.SetActive(false);
		_previousPanel.SetActive(true);

		_currentPanel = returnToPanel;
	}

	private void LoadSettings()
	{
		MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
		SfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
		_inputField.text = PlayerPrefs.GetString("PlayerName", "");

		_musicSlider.value = MusicVolume;
		_sfxSlider.value = SfxVolume;

		for (int i = 0; i < resolutionOptionsList.Count; i++)
		{
			if (resolutionOptionsList[i].width.Equals(Screen.currentResolution.width) &&
				resolutionOptionsList[i].height.Equals(Screen.currentResolution.height) && Screen.currentResolution.refreshRate >= 29)
			{
				_currentResolutionIndex = i;
				_currentResolutionText.text = resolutionOptionsList[_currentResolutionIndex].width + " x " + resolutionOptionsList[_currentResolutionIndex].height + " " + resolutionOptionsList[_currentResolutionIndex].refreshRate + "Hz";
			}
		}

		for (int i = 0; i < graphicsQualityList.Count; i++)
		{
			if (i.Equals(QualitySettings.GetQualityLevel()))
			{
				_currentQualityIndex = i;
				_currentQualitySettingText.text = graphicsQualityList[i].qualityText;
			}
		}
	}

	public void ChangeResolution(int direction)
	{
		if(direction > 0)
		{
			_sfxSource.PlayOneShot(_clickSounds[1]);
			if (_currentResolutionIndex < resolutionOptionsList.Count - 1)
			{
				_currentResolutionIndex++;
			}
			else
			{
				_currentResolutionIndex = 0;
			}
		}
		else
		{
			_sfxSource.PlayOneShot(_clickSounds[0]);
			if (_currentResolutionIndex > 0)
			{
				_currentResolutionIndex--;
			}
			else
			{
				_currentResolutionIndex = resolutionOptionsList.Count - 1;
			}
		}

		_currentResolutionText.text = resolutionOptionsList[_currentResolutionIndex].width + " x " + resolutionOptionsList[_currentResolutionIndex].height + " " + resolutionOptionsList[_currentResolutionIndex].refreshRate + "Hz";
		Screen.SetResolution(resolutionOptionsList[_currentResolutionIndex].width, resolutionOptionsList[_currentResolutionIndex].width, true);
		EventSystem.current.SetSelectedGameObject(null);
	}

	public void ChangeQuality(int direction)
	{
		if (direction > 0)
		{
			_sfxSource.PlayOneShot(_clickSounds[1]);
			if (_currentQualityIndex < graphicsQualityList.Count - 1)
			{
				_currentQualityIndex++;
			}
			else
			{
				_currentQualityIndex = 0;
			}
		}
		else
		{
			_sfxSource.PlayOneShot(_clickSounds[0]);
			if (_currentQualityIndex > 0)
			{
				_currentQualityIndex--;
			}
			else
			{
				_currentQualityIndex = graphicsQualityList.Count - 1;
			}
		}

		_currentQualitySettingText.text = graphicsQualityList[_currentQualityIndex].qualityText;
		QualitySettings.SetQualityLevel(_currentQualityIndex);
		EventSystem.current.SetSelectedGameObject(null);
	}

	private void SaveSettings()
	{
		PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
		PlayerPrefs.SetFloat("SFXVolume", SfxVolume);     
	}

	public void ButtonHover()
	{
		_sfxSource.PlayOneShot(_hover);
	}

	private void LoadTopScores()
	{
		if(File.Exists(Application.persistentDataPath + "/topScoresData.dat"))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/topScoresData.dat", FileMode.Open);

			ScoreData savedScores = (ScoreData)bf.Deserialize(file);
			file.Close();

			// Deletes legacy top 5 file and creates new top 10 file with default values
			if(savedScores.topScores.Count < 6)
			{
				File.Delete(Application.persistentDataPath + "/topScoresData.dat");
				CreateTopScoresFile();
			}

			_topScoresData = new List<TopScore>();
			for (int i = 0; i < savedScores.topScores.Count; i++)
			{
				TopScore topScore = savedScores.topScores[i];
				_topScoresData.Add(topScore);
			}

			//PopulateTopScoreUI();
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

		for(int i = 0; i < _topScoresData.Count; i++)
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

		PopulateTopScoreUI();
	}

	private void UpdateExistingFileToTop10(ScoreData savedScores, BinaryFormatter bf, FileStream file)
	{
		for (int i = 0; i < _topScoresData.Count; i++)
		{
			TopScore topScore = new TopScore();
			topScore.name = _topScoresData[i].name;
			topScore.score = _topScoresData[i].score;

			savedScores.topScores.Add(topScore);
		}

		bf.Serialize(file, savedScores);
	}

	private void PopulateTopScoreUI()
	{
		for (int i = 0; i < _topScoresData.Count; i++)
		{
			_topScores[i].name.text = _topScoresData[i].name;
			_topScores[i].score.text = _topScoresData[i].score.ToString();
		}

		if (_globalTopScoresData != null)
		{
			for (int i = 0; i < _globalTopScoresData.Count; i++)
			{
				_globalTopScores[i].name.text = _globalTopScoresData[i].name;
				_globalTopScores[i].score.text = _globalTopScoresData[i].score.ToString();

				if (_globalTopScores[i].name.text == PlayerPrefs.GetString("PlayerName"))
				{
					_globalTopScores[i].name.color = _highlightedRowColor;
					_globalTopScores[i].score.color = _highlightedRowColor;
				}
			}
		}
	}

	public void ChangeScoreTab(int direction)
	{
		if(direction > 0)
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

	public void ChangeInstructionTab(int direction)
	{
		if (direction > 0)
		{
			_sfxSource.PlayOneShot(_clickSounds[1]);
			if (_currentInstructionTab < _instructionTabs.Count - 1)
			{
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(false);
				_currentInstructionTab++;
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(true);
			}
			else
			{
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(false);
				_currentInstructionTab = 0;
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(true);
			}
		}
		else
		{
			_sfxSource.PlayOneShot(_clickSounds[0]);
			if (_currentInstructionTab > 0)
			{
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(false);
				_currentInstructionTab--;
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(true);
			}
			else
			{
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(false);
				_currentInstructionTab = _instructionTabs.Count - 1;
				_instructionTabs[_currentInstructionTab].tabPanel.SetActive(true);
			}
		}

		_currentInstructionTabText.text = _instructionTabs[_currentInstructionTab].tabName;
		EventSystem.current.SetSelectedGameObject(null);
	}

	IEnumerator OpenScreen()
	{
		_transition.gameObject.SetActive(true);

		yield return new WaitForSeconds(1);

		while (_transition.fillAmount > 0)
		{
			yield return null;
			_transition.fillAmount -= 0.05f;
		}

		_transition.gameObject.SetActive(false);
	}

	IEnumerator Play()
	{
		_transition.gameObject.SetActive(true);

		while(_transition.fillAmount != 1)
		{
			yield return null;
			_transition.fillAmount += 0.05f;
		}
		SceneManager.LoadScene("Stage");
	}

	IEnumerator ExitGame()
	{
		_transition.gameObject.SetActive(true);

		while (_transition.fillAmount != 1)
		{
			yield return null;
			_transition.fillAmount += 0.05f;
		}
		Application.Quit();
	}

	[System.Serializable]
	public class GraphicsSettings
	{
		public string qualityText;
		public int qualitySettingIndex;
	}

	[System.Serializable]
	public class ResolutionOptions
	{
		public int width;
		public int height;
		public int refreshRate;
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

	[System.Serializable]
	public class InstructionTabs
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
