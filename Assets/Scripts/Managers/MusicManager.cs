using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
	public static MusicManager Current { get; private set; }
	[SerializeField]
	AudioMixerGroup _mixerGroup;

	[SerializeField]
	float _fadeTime = 1;

	public int NumClips{get{ return _musicClips?.Length ?? 0; }}

	[SerializeField]
	AudioClip[] _musicClips;
	AudioSource [] _audioSources = new AudioSource[2];
	int _currentAudioSource = -1;

	private void Start()
	{
		if (Current != null && Current != this)
		{
			Destroy(gameObject);
			return;
		}
		Current = this;
		DontDestroyOnLoad(gameObject);

		if ((_musicClips?.Length ?? 0) == 0)
		{
			Debug.LogError("No Music");
			enabled = false;
		}
		for (int i = 0; i < _audioSources.Length; i++)
		{
			var go = new GameObject($"MusicSource{i}");
			go.transform.SetParent(transform);
			_audioSources[i] = go.AddComponent<AudioSource>();
			_audioSources[i].volume = 0;
			_audioSources[i].loop = true;
			_audioSources[i].outputAudioMixerGroup = _mixerGroup;
		}
		FadeTo(0);
	}

	public void FadeTo(int index)
	{
		if(_currentAudioSource >=0)
			StartCoroutine(doFade(_currentAudioSource, 0));
		_currentAudioSource = (++_currentAudioSource) % _audioSources.Length;
		_audioSources[_currentAudioSource].volume = 0;
		_audioSources[_currentAudioSource].clip = _musicClips[index];
		_audioSources[_currentAudioSource].Play();
		StartCoroutine(doFade(_currentAudioSource, 1));
	}
	IEnumerator doFade(int source, float volume)
	{
		float speed = 0;
		while(Mathf.Abs(_audioSources[source].volume - volume) > 0.01f)
		{
			_audioSources[source].volume = Mathf.SmoothDamp(_audioSources[source].volume, volume, ref speed, _fadeTime, float.MaxValue, Time.unscaledDeltaTime);
			yield return null;
		}
		_audioSources[source].volume = volume;
		if(volume < 0.01f)
			_audioSources[source].Stop();
	}
}
