using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Kritter", menuName = "Kritter Data")]
public class KritterData : ScriptableObject
{

	[SerializeField] private KritterTypes _kritterType = KritterTypes.Blots;

	// Min and Max Cooldown values
	[SerializeField] private float _minCd = 15.0f;
	[SerializeField] private float _maxCd = 25.0f;
	[SerializeField] private float _needTimeout = 10.0f;

	// Percentage values for various needs
	[SerializeField] [Range(0.0f, 1.0f)] private float _nonePercentage = 0.40f;
	[SerializeField] [Range(0.0f, 1.0f)] private float _foodPercentage = 0.20f;
	[SerializeField] [Range(0.0f, 1.0f)] private float _waterPercentage = 0.20f;
	[SerializeField] [Range(0.0f, 1.0f)] private float _attentionPercentage = 0.20f;

	[SerializeField] private float _matingTimer = 10.0f;
	[SerializeField] private float _fightingTimer = 10.0f;

	[SerializeField] private float _movementSpeed = 5.0f;
	[SerializeField] private int _startingHealth = 3;
	[SerializeField] private float _aggroRadius = 0.5f;
	[SerializeField] private float _spawnPitch = 1.0f;
	[SerializeField] private Sprite[] _frontSprites;
	[SerializeField] private Sprite[] _backSprites;


	public KritterTypes KritterType { get { return _kritterType; } }

	public float MinCd { get { return _minCd; } }

	public float MaxCd { get { return _maxCd; } }

	public float NeedTimeout { get { return _needTimeout; } }

	public float FoodPercentage { get { return _foodPercentage; } }

	public float WaterPercentage { get { return _waterPercentage; } }

	public float AttentionPercentage { get { return _attentionPercentage; } }

	public float MatingTimer { get { return _matingTimer; } }

	public float FightingTimer { get { return _fightingTimer; } }

	public float MovementSpeed { get { return _movementSpeed; } }

	public int StartingHealth { get { return _startingHealth; } }

	public float AggroRadius { get { return _aggroRadius; } }

	public float NonePercentage { get { return _nonePercentage; } }

	public Sprite[] Sprites { get { return _frontSprites; } }

	public Sprite[] BackSprites { get { return _backSprites; } }

	public float Pitch { get { return _spawnPitch; } }
}
