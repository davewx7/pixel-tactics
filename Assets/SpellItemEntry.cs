using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.EventSystems;

public class SpellItemEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public UnitSpell spell;

    [SerializeField]
    SpellsDialog _spellsDialog = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _nameText = null, _levelText = null, _statusText = null;

    [SerializeField]
    Sprite _bg = null, _bgHighlight = null;

    [SerializeField]
    Image _image = null, _slotImage = null;

    bool _interactable = true;
    public bool interactable {
        get {
            return _interactable;
        }
        set {
            _interactable = value;
            _image.color = value ? Color.white : Color.gray;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(interactable) {
            _image.sprite = _bgHighlight;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.sprite = _bg;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(interactable) {
            _spellsDialog.SpellItemEntryClicked(this);
        }
    }

    public void SetStatus(string statusText)
    {
        _statusText.text = statusText;
    }

    // Start is called before the first frame update
    void Start()
    {
        _slotImage.sprite = spell.icon;
        _slotImage.material = new Material(_slotImage.material);
        _slotImage.material.SetFloat("_hueshift", spell.hueshift);
        _nameText.text = spell.description;
        _levelText.text = string.Format("lvl{0}", spell.spellLevel);
        UnitStatusPanel.SetTooltip(_image, spell.GetTooltip());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
