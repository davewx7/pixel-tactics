using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RecruitDialogUnitEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public UnitInfo unitInfo;

    [SerializeField]
    RecruitDialog _dialog = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _unitNameText = null, _unitTypeText = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _goldText = null;

    [SerializeField]
    Image _avatarImage = null;

    [SerializeField]
    Image _cursor = null, _mainPanel = null;

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
    Sprite _backgroundNormal = null, _backgroundFocus = null;

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
        if(eventData.clickCount == 2 && optionEnabled) {
            GameController.instance.OkayRecruit();
        }
    }

    bool _optionEnabled = true;
    public bool optionEnabled {
        get { return _optionEnabled; }
        set {
            if(_optionEnabled != value) {
                _optionEnabled = value;
                _unitNameText.color = value ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                _unitTypeText.color = value ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
                _mainPanel.color = value ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }

    bool _tooExpensive = false;
    public bool tooExpensive {
        get { return _tooExpensive; }
        set {
            if(_tooExpensive != value) {
                _tooExpensive = value;
                _goldText.color = value ? new Color(1f, 0f, 0f, 1f) : new Color(1f, 1f, 1f, 1f);
            }
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        _avatarImage.sprite = unitInfo.avatarImage;
        _goldText.text = unitInfo.unitType.cost.ToString();
        _unitNameText.text = unitInfo.characterName;
        _unitTypeText.text = unitInfo.unitType.description;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
