using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelUpUnitEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public UnitInfo unitInfo;

    [SerializeField]
    LevelUpDialog _dialog;

    [SerializeField]
    TMPro.TextMeshProUGUI _unitTypeText;

    [SerializeField]
    Image _avatarImage;

    [SerializeField]
    Image _cursor, _mainPanel;

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

    [SerializeField]
    Sprite _backgroundNormal, _backgroundFocus;

    void SetBackground()
    {
        _mainPanel.sprite = (mouseover || highlight) ? _backgroundFocus : _backgroundNormal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseover = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _dialog.SelectUnitEntry(this);
        if(eventData.clickCount == 2) {
            GameController.instance.OkayLevelUp();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _avatarImage.sprite = unitInfo.avatarImage;
        _unitTypeText.text = unitInfo.unitType.description;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
