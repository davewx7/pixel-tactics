using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelectablePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    Image _cursor = null, _mainPanel = null;

    [SerializeField]
    Sprite _backgroundNormal = null, _backgroundFocus = null;

    bool _highlight = false;
    public bool highlight {
        get { return _highlight; }
        set {
            _highlight = value;
            Color color = _cursor.color;
            color.a = value ? 1f : 0f;
            _cursor.color = color;
            SetBackground();
        }
    }

    bool _mouseover = false;
    public bool mouseover {
        get { return _mouseover; }
        set {
            _mouseover = value;
            SetBackground();
        }
    }

    public virtual void OnClicked()
    {

    }


    public virtual void OnDoubleClicked()
    {

    }

    public void Init()
    {
        SetBackground();
    }

    void SetBackground()
    {
        _mainPanel.sprite = (mouseover || highlight) ? _backgroundFocus : _backgroundNormal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("POINTER ENTER");
        mouseover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseover = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked();

        if(eventData.clickCount == 2) {
            OnDoubleClicked();
        }
    }
}
