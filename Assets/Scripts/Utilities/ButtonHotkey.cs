using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
	[SerializeField]
	private KeyCode _key;
	private Button _button;
	private void Start()
	{
		_button = GetComponent<Button>();
	}
	void Update()
	{
		if(Input.GetKeyDown(_key))
		{
			_button.Select();
			_button.onClick.Invoke();
		}
	}
}