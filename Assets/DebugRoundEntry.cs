using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DebugRoundEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    DebugGameScreen _mainDialog = null;

    public int nround;

    [SerializeField]
    Image _image = null;

    public TMPro.TextMeshProUGUI descriptionText;

    Color _normalColor;
    Color _highlightColor = Color.white;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _image.color = _highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.color = _normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _mainDialog.RoundClicked(nround);
    }

    // Start is called before the first frame update
    void Start()
    {
        _normalColor = _image.color;   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
