using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class InventorySlotDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    RectTransform rectTransform {
        get {
            return GetComponent<RectTransform>();
        }
    }

    [SerializeField]
    TMPro.TextMeshProUGUI _cooldownText = null;

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    [SerializeField]
    bool _allowInModal = false;

    [SerializeField]
    Sprite _blankIcon = null;

    [SerializeField]
    Image _image = null;

    Equipment _equipment = null;

    UnitSpell _spell = null;

    int _tier = 1;

    bool _canActivate = false;
    public virtual bool interactable {
        get {
            return _canActivate;
        }
    }

    public virtual bool inShop {
        get {
            return false;
        }
    }

    public void Clear()
    {
        _equipment = null;
        _spell = null;
        _image.sprite = _blankIcon;
        _image.material = null;
        _canActivate = false;
        _image.color = Color.white;
        UnitStatusPanel.ClearTooltip(_image);

        if(_cooldownText != null) {
            _cooldownText.gameObject.SetActive(false);
        }
    }
    
    public UnitSpell spell {
        get { return _spell; }
    }

    float _hueshift = 0f;
    void SetShaderHue()
    {
        if(_image.material != null) {
            _image.material.SetFloat("_hueshift", _hueshift);
        }
    }

    public void SetSpell(UnitSpell s)
    {
        _spell = s;
        _tier = 1;
        _equipment = null;
        _image.material = new Material(GameConfig.instance.GetMaterialForInventorySlot(_tier, false));
        _hueshift = _spell.hueshift;
        SetShaderHue();
        _image.sprite = _spell.icon;
        _canActivate = (_unitStatusPanel != null && _unitStatusPanel.displayedUnit.unitInfo.tired == false && _unitStatusPanel.displayedUnit.unitInfo.sleeping == false && _unitStatusPanel.displayedUnit.unitInfo.SpellOnCooldown(s) == false);

        int cooldownTime = _unitStatusPanel == null ? -1 : _unitStatusPanel.displayedUnit.unitInfo.SpellCooldownRemaining(s);

        if(_cooldownText != null) {
            _cooldownText.gameObject.SetActive(cooldownTime > 0);
            if(cooldownTime > 99) {
                _cooldownText.text = "X";
            } else {
                _cooldownText.text = cooldownTime.ToString();
            }
        }

        string tooltip = _spell.GetTooltip(unitInfo);

        if(_unitStatusPanel != null && _canActivate == false) {
            if(unitInfo != null && unitInfo.sleeping) {
                tooltip += "\n<color=#ff0000>Sleeping: This spell cannot be used while the unit is asleep.</color>";
            } else if(unitInfo != null && unitInfo.tired) {
                tooltip += "\n<color=#ff0000>Tired: This unit cannot cast any more spells this turn.</color>";
            } else if(cooldownTime > 0 && cooldownTime <= 99) {
                tooltip += string.Format("\n<color=#ff0000>Cooldown: Can be used again in {0} {1}. If the unit rests it will immediately reset this cooldown.</color>", cooldownTime, cooldownTime == 1 ? "Moon" : "Moons");
            } else {
                tooltip += "\n<color=#ff0000>Expended: This spell cannot be used again until after the unit rests.</color>";
            }

            _image.color = Color.gray;
        } else {
            _image.color = Color.white;
        }

        UnitStatusPanel.SetTooltip(_image, tooltip);

    }

    public Equipment equipment {
        get { return _equipment; }
    }

    UnitInfo unitInfo {
        get {
            if(_unitStatusPanel != null) {
                return _unitStatusPanel.displayedUnit.unitInfo;
            }

            return null;
        }
    }

    public void SetEquipment(Equipment equip)
    {
        _tier = equip.tier;
        _spell = null;
        _canActivate = false;
        _image.material = new Material(GameConfig.instance.GetMaterialForInventorySlot(_tier, false));
        _hueshift = equip.hueShift;
        SetShaderHue();

        _equipment = equip;
        _image.sprite = equip.icon;
        if(equip.activatedAbility != null && _unitStatusPanel != null && _unitStatusPanel.displayedUnit.unitInfo.sleeping == false && _unitStatusPanel.displayedUnit.unitInfo.tired == false && _unitStatusPanel.displayedUnit.unitInfo.SpellOnCooldown(equip.activatedAbility) == false) {
            _canActivate = true;
        }

        int cooldownTime = -1;
        if(equip.activatedAbility != null && _unitStatusPanel != null) {
            cooldownTime = _unitStatusPanel.displayedUnit.unitInfo.SpellCooldownRemaining(equip.activatedAbility);
        }

        if(_cooldownText != null) {
            _cooldownText.gameObject.SetActive(cooldownTime > 0);
            if(cooldownTime > 99) {
                _cooldownText.text = "X";
            } else {
                _cooldownText.text = cooldownTime.ToString();
            }
        }

        string expendedText = "";
        if(_canActivate == false && equip.activatedAbility != null) {
            if(inShop == false) {
                if(unitInfo != null && unitInfo.sleeping) {
                    expendedText = "\n<color=#ff0000>Sleeping: This equipment cannot be used while the unit is asleep.</color>";
                } else if(unitInfo != null && unitInfo.tired) {
                    expendedText = "\n<color=#ff0000>Tired: This unit is tired and cannot use any more items or cast any more spells this turn.</color>";
                } else if(cooldownTime > 0 && cooldownTime <= 99) {
                    expendedText = string.Format("\n<color=#ff0000>Cooldown: Can be used again in {0} {1}. If the unit rests it will immediately reset this cooldown.</color>", cooldownTime, cooldownTime == 1 ? "Moon" : "Moons");
                } else {
                    expendedText = "\n<color=#ff0000>Expended: This equipment cannot be used again until after the unit rests.</color>";
                }
            }
            _image.color = Color.gray;
        } else {
            _image.color = Color.white;
        }

        string cursedText = "";
        if(equip.cursed) {
            cursedText = "<color=#ff0000>Cursed: Cannot be sold or discarded.</color>\n";
        }

        UnitStatusPanel.SetTooltip(_image, string.Format("<color=#ffffff>{0}</color> ({1}):\n{2}<color=#aaaaaa>{3}</color>{4}", equip.description, equip.slot.description, cursedText, equip.GetToolTip(), expendedText));

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_allowInModal == false && GameConfig.modalDialog > 0) {
            return;
        }

        if(interactable) {
            _image.material = new Material(GameConfig.instance.GetMaterialForInventorySlot(_tier, true));
            SetShaderHue();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.material = new Material(GameConfig.instance.GetMaterialForInventorySlot(_tier, false));
        SetShaderHue();
    }

    public virtual void Clicked()
    {

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(_allowInModal == false && GameConfig.modalDialog > 0) {
            return;
        }

        if(eventData.button == PointerEventData.InputButton.Right) {
            if(_unitStatusPanel != null && GameController.instance.localPlayerTurn && inShop == false && equipment != null && equipment.cursed == false) {

                HashSet<Loc> validLocs = new HashSet<Loc>();
                Loc[] adj = Tile.AdjacentLocs(_unitStatusPanel.displayedUnit.loc);
                foreach(Loc a in adj) {
                    Tile t = GameController.instance.map.GetTile(a);
                    if(t == null || t.isvoid || (t.loot != null && t.loot.gameObject.activeSelf)) {
                        continue;
                    }

                    Unit targetUnit = GameController.instance.GetUnitAtLoc(a);
                    if(targetUnit == null || (targetUnit.team == _unitStatusPanel.displayedUnit.team && equipment.EquippableForUnit(targetUnit.unitInfo))) {
                        validLocs.Add(a);
                    }
                }

                List<GameContextMenu.Entry> entries = new List<GameContextMenu.Entry>() {
                    new GameContextMenu.Entry() {
                        text = "Trade",
                        tooltip = "Give this item to an adjacent unit or drop it in a vacant tile",
                        action = () => GameController.instance.SetDiscardingEquipment(_unitStatusPanel.displayedUnit, equipment, validLocs),
                    },
                    new GameContextMenu.Entry() {
                        text = "Discard",
                        tooltip = "Trash this item. It will disappear from the game.",
                        action = () => GameController.instance.TrashEquipmentCommand(_unitStatusPanel.displayedUnit, equipment),
                    },
                };

                if(equipment.tier < 0) {
                    entries[0].disabled = true;
                    entries[1].disabled = true;
                    foreach(var item in entries) {
                        item.disabled = true;
                        item.tooltip += "\n<color=#ffaaaa>This item is bound to this unit and cannot be traded or discarded.</color>";
                    }
                } else if(equipment.tier == 0) {
                    entries[0].disabled = true;
                    entries[0].tooltip += "\n<color=#ffaaaa>Consumables may not be traded. They can be consumed or discarded.</color>";
                }

                GameController.instance.ShowContextMenu(entries);
            }

            return;
        }

        Clicked();

        if(_unitStatusPanel != null && GameController.instance.localPlayerTurn) {
            if(_equipment != null && _equipment.activatedAbility != null && _canActivate) {
                _equipment.activatedAbility.StartCasting(_unitStatusPanel.displayedUnit);
            } else if(_spell != null && _canActivate) {
                _spell.StartCasting(_unitStatusPanel.displayedUnit);
            }

            if(eventData.clickCount == 2) {
            }
        }
    }

    public void AnimateGetEquipment()
    {
        rectTransform.SetAsLastSibling();

        Vector3 destPos = rectTransform.localPosition;

        rectTransform.localPosition += new Vector3(-rectTransform.sizeDelta.x*0.5f, rectTransform.sizeDelta.y*0.5f);
        rectTransform.localScale = Vector3.one*2f;

        rectTransform.DOScale(1f, 1f);
        rectTransform.DOLocalMove(destPos, 1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if(_equipment != null) {
            _hueshift = _equipment.hueShift;
        } else if(_spell != null) {
            _hueshift = _spell.hueshift;
        }
        SetShaderHue();
#endif
    }
}
