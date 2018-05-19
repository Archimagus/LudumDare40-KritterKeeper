using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Kritter : MonoBehaviour
{
	[HideInInspector] public KritterData Data;
	[SerializeField] private SpriteRenderer _graphics;
	[SerializeField] private Canvas _needIcon;
	[SerializeField] private Sprite[] _needIcons;
	[SerializeField] private SpriteRenderer []_damageTicks;

	[SerializeField] private bool _isStarving = false;
	[SerializeField] private AudioClip _spawn;
	[SerializeField] private AudioClip _hurt;
	[SerializeField] private AudioClip[] _death;
	[SerializeField] private AudioClip _newNeed;
	[SerializeField] private AudioClip _grab;

	private Image _needImage;
	private Image _needProgress;
	public KritterTypes Type { get { return Data.KritterType; } }
	private float _timer = 0.0f;
	private int _health;
	public bool IsStarving { get { return _isStarving; } }
	private KritterNeeds _currentNeed = KritterNeeds.None;

	private FollowMosue _followMouse;
	private Animator _animator;
	private State _state;
	private ParticleSystem _particles;
	private AudioSource _audioSource;
	float _speed = 0;

	enum State
	{
		Idle,
		Walk,
		Dragging,
		Fighting,
		Mating,
		Death
	}

	void Start()
	{
		_state = State.Idle;
		_graphics.sprite = Data.Sprites[0];
		_needImage = _needIcon.transform.Find("NeedImage").GetComponent<Image>();
		_needProgress = _needIcon.transform.Find("FillImage").GetComponent<Image>();
		_needIcon.gameObject.SetActive(false);
		_animator = GetComponent<Animator>();
		_followMouse = GetComponent<FollowMosue>();
		_audioSource = GetComponent<AudioSource>();
		_particles = GetComponentInChildren<ParticleSystem>();
		_particles.gameObject.SetActive(false);
		_health = Data.StartingHealth;
		ResetCDTimer();
		NextState();
	}

	private void ResetCDTimer()
	{
		float scalar = Mathf.Min(1.0f, GameManager.Current.KritterManager.Count / 5.0f);
		_timer = Random.Range(Data.MinCd, Data.MaxCd) * scalar;
	}

	public void PlayDropParticles()
	{
		_particles.gameObject.SetActive(true);
		PlaySound(_spawn);
	}
	internal void StartDrag()
	{
		_state = State.Dragging;
	}
	internal void EndDrag()
	{
		_state = State.Idle;
	}

	internal bool FillNeed(KritterNeeds needAction)
	{
		if (_currentNeed == needAction)
		{
			GameManager.Current.PlayerManager.IncreaseScore(GetNeedScore(needAction));
			ClearNeed();
			return true;
		}

		if (needAction == KritterNeeds.Health && (_health < Data.StartingHealth))
		{
			GameManager.Current.PlayerManager.IncreaseScore(GetNeedScore(needAction));
			Heal();
			return true;
		}

		return false;
	}


	private int GetNeedScore(KritterNeeds need)
	{
		float score = _timer * 100;

		switch (need)
		{
			case KritterNeeds.Food:
				score *= Data.FoodPercentage;
				break;
			case KritterNeeds.Water:
				score *= Data.WaterPercentage;
				break;
			case KritterNeeds.Care:
				score *= Data.AttentionPercentage;
				break;
			case KritterNeeds.Health:
				score = 250.0f;
				break;
		}

		return (int)score;
	}

	private void Update()
	{
		_timer -= Time.deltaTime;

		if (_timer <= 0)
		{
			if (_currentNeed == KritterNeeds.None)
			{
				PickNeed();
			}
			else
			{
				Hurt();
				ClearNeed();
			}
		}
		else if(_currentNeed != KritterNeeds.None)
		{
			_needProgress.fillAmount = _timer / Data.NeedTimeout;
		}
		_animator.SetFloat("Speed", _speed);
	}

	private void ClearNeed()
	{
		_needIcon.gameObject.SetActive(false);
		_currentNeed = KritterNeeds.None;
		ResetCDTimer();
	}
	private void PickNeed()
	{
		float roll = Random.Range(0.0f, 1.0f);
		_timer = Data.NeedTimeout;

		if (roll < Data.NonePercentage)
		{
			_currentNeed = KritterNeeds.None;
			ResetCDTimer();
		}
		else if (roll <= Data.NonePercentage + Data.FoodPercentage)
		{
			_currentNeed = KritterNeeds.Food;
		}
		else if (roll <= Data.NonePercentage + Data.FoodPercentage + Data.WaterPercentage)
		{
			_currentNeed = KritterNeeds.Water;
		}
		else
		{
			_currentNeed = KritterNeeds.Care;
		}
		if (_currentNeed != KritterNeeds.None)
		{
			_needIcon.gameObject.SetActive(true);
			_needImage.sprite = _needIcons[(int)_currentNeed];
			_needProgress.fillAmount = 1;
			PlaySound(_newNeed);
		}
	}

	IEnumerator IdleState()
	{
		float time = Time.time + Random.Range(3f, 10f);
		while (_state == State.Idle)
		{
			_speed = 0;
			if (time > Time.time)
				yield return null;
			else
				_state = State.Walk;
		}
		NextState();
	}
	IEnumerator DeathState()
	{
		while (_state == State.Death)
		{
			_speed = 0;
			yield return null;
		}
		NextState();
	}
	IEnumerator DraggingState()
	{
		_animator.SetTrigger("Grab");
		_followMouse.enabled = true;
		_followMouse.SendMessage("Update");
		PlaySound(_grab);
		while (_state == State.Dragging)
		{
			_speed = 0;
			yield return null;
		}
		_animator.SetTrigger("Drop");
		_followMouse.enabled = false;
		NextState();
	}

	IEnumerator WalkState()
	{
		float acceleration = 0;
		var targetPosition = GameManager.Current.GetLegalKritterPosition();
		var direction = targetPosition - transform.position;
		if(direction.y > 0)
		{
			SetSprite(isBackSprite:true);
		}
		while (_state == State.Walk)
		{
			yield return 0;
			_speed = Mathf.SmoothDamp(_speed, Data.MovementSpeed, ref acceleration, 1f);
			transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
			direction = targetPosition - transform.position;
			if (Vector3.SqrMagnitude(direction) < 0.1f)
			{
				_speed = 0;
				_state = State.Idle;
			}
		}
		SetSprite();
		NextState();
	}

	void NextState()
	{
		string methodName = _state.ToString() + "State";
		System.Reflection.MethodInfo info =
			GetType().GetMethod(methodName,
								System.Reflection.BindingFlags.NonPublic |
								System.Reflection.BindingFlags.Instance);
		StartCoroutine((IEnumerator)info.Invoke(this, null));
	}

	void Hurt(int amount = 1)
	{
		_health -= amount;
		SetHealthTicks();

		if (_health <= 0)
		{
			// Kritter is dead
			GameManager.Current.PlayerManager.AddFailure();
			PlaySound(_death[Random.Range(0, _death.Length)]);
			_state = State.Death;
			Destroy(gameObject, 1.0f);
		}
		else
		{
			PlaySound(_hurt);
		}
	}


	void Heal(int amount = 1)
	{
		_health += amount;
		SetHealthTicks();
		if (_health > Data.StartingHealth)
			_health = Data.StartingHealth;
	}

	void PlaySound(AudioClip clip)
	{
		_audioSource.Stop();
		_audioSource.clip = clip;
		_audioSource.pitch = Data.Pitch;
		_audioSource.Play();
	}
	private void SetHealthTicks()
	{
		//_damageTicks[1].enabled = _health < 2;
		//_damageTicks[0].enabled = _health < 3;
		SetSprite();
	}

	private void SetSprite(bool isBackSprite = false)
	{
		if (isBackSprite)
		{
			if (Data.BackSprites.Length - 1 >= Data.StartingHealth - _health)
			{
				_graphics.sprite = Data.BackSprites[Data.StartingHealth - _health];
			}
		}
		else
		{
			if (Data.Sprites.Length - 1 >= Data.StartingHealth - _health)
			{
				_graphics.sprite = Data.Sprites[Data.StartingHealth - _health];
			}
		}
		
	}
}
