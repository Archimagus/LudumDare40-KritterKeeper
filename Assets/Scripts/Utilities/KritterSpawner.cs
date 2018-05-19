using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Spawner class used to create pools of kritter prefabs to spawn in the scene
 **/ 
public class KritterSpawner : MonoBehaviour
{
	// Build this list within the inspector
	[SerializeField] private Kritter _kritterPrefab;
	[SerializeField] private List<KritterData> _kritterDatas;
	[SerializeField] private float _startingRespawnRate = 30.0f;
	private float _spawnTimer;

	private List<Kritter> _kritters = new List<Kritter>();

	public int Count { get { return _kritters.Count; } }
	
	private PlayerManager _playerManager;

	private void Start()
	{
		_playerManager = GameManager.Current.PlayerManager;
	}

	private void Update()
	{
		float rateMult = Mathf.Max(1.0f, _playerManager.Score / 1000);

		if(!_playerManager.GameOver)
		{
			_spawnTimer -= Time.deltaTime;
			if (_spawnTimer < 0)
			{
				_spawnTimer = _startingRespawnRate / rateMult;
				SpawnKritter();
			}
		}
	}

	// Spawns a kritter
	// Default is one random kritter prefab from kritter prefabs list (index of 0 gets overridden with random value)
	public void SpawnKritter(bool spawnRandom = true, int spawnAmount = 1, int index = 0)
	{
		if(_kritterDatas != null)
		{
			if(spawnRandom)
			{
				for (int i = 0; i < spawnAmount; i++)
				{
					index = Random.Range(0, _kritterDatas.Count);
					Spawn(_kritterDatas[index]);
				}                   
			}
			else
			{
				if(index < 0 || index >= _kritterDatas.Count)
				{
					Debug.LogError("Failed to spawn kritter at Index: " + index + ". Index must be between 0 and " + _kritterDatas.Count + ".");
				}
				else
				{
					for (int i = 0; i < spawnAmount; i++)
					{
						Spawn(_kritterDatas[index]);
					}
				}                
			}
			
		}
		
	}

	private void Spawn(KritterData kritter)
	{
		Kritter newKritter = Instantiate(_kritterPrefab, GameManager.Current.KorralCollider.transform);
		newKritter.name = kritter.name;
		newKritter.Data = kritter;
		newKritter.transform.position = GameManager.Current.GetLegalKritterPosition();

		_kritters.Add(newKritter);
	}

	public Kritter GetNearestKritterToFight(Kritter instigator)
	{
		float shortestDistance = float.MaxValue;
		Kritter nearest = null;

		foreach (Kritter k in _kritters)
		{
			if ((instigator.IsStarving || instigator.Type != k.Type) && instigator != k)
			{
				float dist = Vector3.Distance(instigator.transform.position, k.transform.position);

				if (dist < shortestDistance)
				{
					shortestDistance = dist;
					nearest = k;
				}
			}
		}

		return nearest;
	}
}
