using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;


public class AttackPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    AttackDisplay _attackDisplay = null, _counterattackDisplay = null;

    public void SwapDisplays()
    {
        var a = _attackDisplay;
        _attackDisplay = _counterattackDisplay;
        _counterattackDisplay = a;
    }

    public bool interactable = false;

    public UnitInfo attacker;
    public UnitInfo defender;

    public AttackInfo attackInfo;
    public AttackInfo? counterattackInfo;

    [SerializeField]
    TextMeshProUGUI _rangeText = null;

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

    void SetBackground()
    {
        _mainPanel.sprite = (mouseover || highlight) ? _backgroundFocus : _backgroundNormal;
    }

    public AttackDialog dialog;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(interactable) {
            mouseover = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(interactable) {
            mouseover = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        dialog.HighlightAttack(this);

        if(eventData.clickCount == 2) {
            GameController.instance.OkayAttack();
        }
    }

    float _dieTime = -1f;
    public void FadeOut()
    {
        _dieTime = Time.time + 2f;
    }

    public void FadeIn()
    {
        var rt = GetComponent<RectTransform>();
        float ypos = rt.anchoredPosition.y;
        rt.anchoredPosition += new Vector2(0f, -64f);
        rt.DOAnchorPosY(ypos, 0.2f);
    }

    public void Init()
    {
        SetBackground();

        _rangeText.text = string.Format("- {0} -", attackInfo.rangeDescription);
        _attackDisplay.Init(attacker, attackInfo);
        _counterattackDisplay.Init(defender, counterattackInfo);
    }

    // Update is called once per frame
    void Update()
    {
        if(_dieTime > 0f && Time.time > _dieTime) {
            _dieTime = -1f;
            var rt = GetComponent<RectTransform>();
            rt.DOAnchorPosY(rt.anchoredPosition.y - 64f, 0.2f).OnComplete(() => GameObject.Destroy(gameObject));
        }
    }
}
