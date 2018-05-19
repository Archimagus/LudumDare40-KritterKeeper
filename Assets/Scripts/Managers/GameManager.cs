using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
 * Manager class for referencing global objects
 **/
public class GameManager : MonoBehaviour
{
	public static GameManager Current { get; private set; }

	public PolygonCollider2D KorralCollider;
	public PlayerManager PlayerManager;
	public UIManager UIManager;
	public KritterSpawner KritterManager;

	private void Awake()
	{
		Current = this;

		PlayerManager = FindObjectOfType<PlayerManager>();
		UIManager = FindObjectOfType<UIManager>();
	}

	// Returns a position within the bounds of the korral collider
	public Vector3 GetLegalKritterPosition()
	{		
		Vector3 newVector = new Vector3(Random.Range(KorralCollider.bounds.min.x, KorralCollider.bounds.max.x), 
										Random.Range(KorralCollider.bounds.min.y, KorralCollider.bounds.max.y), 0);

		return newVector;
	}

	public Kritter GetNearestKritterToFight(Kritter instigator)
	{
		return KritterManager.GetNearestKritterToFight(instigator);
	}
}
