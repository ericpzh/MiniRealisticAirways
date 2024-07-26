using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class LinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text _textMeshPro;
    public string url;

    void Awake()
    {
        _textMeshPro = GetComponent<TMP_Text>();
        _textMeshPro.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Application.OpenURL(url);
    }
}