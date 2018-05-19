using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMosue : MonoBehaviour
{
	[SerializeField]
	Vector2 _offset;
	Camera _camera;
	Camera Camera
	{
		get
		{
			return _camera ?? (_camera = Camera.main);
		}
	}
	
	void Update()
	{
		var pos = Camera.ScreenToWorldPoint( Input.mousePosition);
		transform.position = new Vector2(pos.x, pos.y) + _offset;
	}
}
