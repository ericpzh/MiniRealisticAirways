using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LinkHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private TMP_Text _textMeshPro;

	public string url;

	private void Awake()
	{
		_textMeshPro = GetComponent<TMP_Text>();
		if (_textMeshPro != null)
		{
			_textMeshPro.raycastTarget = true;
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!string.IsNullOrWhiteSpace(url))
		{
			Application.OpenURL(url);
		}
	}
}
