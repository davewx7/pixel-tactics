using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;


public class ChooseTeamPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Team team;

    public bool interactable = true;

    [SerializeField]
    TMPro.TextMeshProUGUI _characterNameText = null, _characterTitleText = null, _descriptionText = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _unlockedText = null;

    [SerializeField]
    Image _avatarImage = null;

    [SerializeField]
    Image _image = null;

    [SerializeField]
    Sprite _normalSprite = null, _highlightSprite = null, _selectedSprite = null, _disabledSprite = null;

    [SerializeField]
    ChooseTeamScreen _chooseTeamScreen = null;

    DefeatBanner _defeatBanner = null;

    public void SetDefeatBanner(DefeatBanner banner)
    {
        _defeatBanner = banner;
    }

    [SerializeField]
    Transform _lock = null;

    public bool locked = true;

    public void CalculateSprite()
    {
        if(locked) {
            _image.sprite = _disabledSprite;
        } else if(selected) {
            _image.sprite = _selectedSprite;
        } else if(mouseover) {
            _image.sprite = _highlightSprite;
        } else {
            _image.sprite = _normalSprite;
        }
    }

    bool _selected = false;
    public bool selected {
        get { return _selected; }
        set {
            _selected = value;
            CalculateSprite();
        }
    }

    bool _mouseover = false;
    public bool mouseover {
        get { return _mouseover; }
        set {
            _mouseover = value;
            CalculateSprite();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(locked || interactable == false) {
            mouseover = false;
            return;
        }
        mouseover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseover = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(locked == false && interactable) {
            if(_chooseTeamScreen != null) {
                _chooseTeamScreen.ChoosePanel(this);
                if(eventData.clickCount == 2) {
                }
            }

            if(_defeatBanner != null) {
                _defeatBanner.RemoveChooseTeamPanel();
            }
        }
    }

    float _unlocking = -1f;

    public void UnlockAnim()
    {
        _unlockedText.gameObject.SetActive(true);
        _unlockedText.DOColor(new Color(1f, 1f, 1f, 0f), 3f);
        _unlockedText.transform.DOScale(4f, 3f);
        _unlocking = 2f;

        locked = false;
        CalculateSprite();
    }

    // Start is called before the first frame update
    void Start()
    {
        _characterNameText.text = team.rulerName;
        _avatarImage.sprite = team.rulerType.avatarImage;
        _characterTitleText.text = team.rulerTitle;
        _descriptionText.text = team.factionDescription;

        _lock.gameObject.SetActive(locked);

        CalculateSprite();
    }

    // Update is called once per frame
    void Update()
    {
        if(_unlocking > 0f) {
            _unlocking -= Time.deltaTime;
            _lock.gameObject.SetActive(!_lock.gameObject.activeSelf);

            if(_unlocking <= 0f) {
                _lock.gameObject.SetActive(false);
            }
        }
    }
}
