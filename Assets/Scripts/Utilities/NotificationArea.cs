using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationArea : MonoBehaviour 
{
	[SerializeField] TextMeshProUGUI _messageText;

	public void Show(string text)
	{
		gameObject.SetActive(true);
		transform.localScale = new Vector3(0, 1, 1);
		_messageText.text = text;
		StartCoroutine(ShowNotification());
	}

	private IEnumerator ShowNotification()
	{
		Vector3 velocity = Vector3.zero;
		while (transform.localScale.x < 0.9f)
		{
			transform.localScale = Vector3.SmoothDamp(transform.localScale, Vector3.one, ref velocity, 0.25f, float.MaxValue, Time.unscaledDeltaTime);
			yield return null;
		}
		transform.localScale = Vector3.one;
		yield return new WaitForSeconds(3);
		while (transform.localScale.x > 0.1f)
		{
			transform.localScale = Vector3.SmoothDamp(transform.localScale, new Vector3(0,1,1), ref velocity, 0.25f, float.MaxValue, Time.unscaledDeltaTime);
			yield return null;
		}
		transform.localScale = new Vector3(0, 1, 1);
		gameObject.SetActive(false);
	}
	
}
