using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitStatusPanel : MonoBehaviour
{
    public Unit displayedUnit = null;
    bool _mustHaveDisplayedUnit = false;

    //A 'baseline unit' is a unit to compare the stats of the current unit with.
    UnitInfo _baseLineUnit = null;

    [SerializeField]
    List<InventorySlotDisplay> _inventorySlots = new List<InventorySlotDisplay>();

    public InventorySlotDisplay GetInventorySlot(Equipment equipment)
    {
        InventorySlotDisplay result = null;
        foreach(InventorySlotDisplay slot in _inventorySlots) {
            if(slot.equipment == equipment) {
                result = slot;
            }
        }

        return result;
    }

    [SerializeField]
    UnitSpellStatus _spellStatus = null;

    [SerializeField]
    Image _unitStatusIconPrefab = null;

    [SerializeField]
    Transform _unitStatusParent = null;

    List<Image> _unitStatusIcons = new List<Image>();

    [SerializeField]
    UnitAttackStatusPanel _attackStatusPanel = null;

    List<UnitAttackStatusPanel> _attackStatusPanelInstances = new List<UnitAttackStatusPanel>();

    [SerializeField]
    UnitSpellStatusPanel _spellStatusPanel = null;

    List<UnitSpellStatusPanel> _spellStatusPanelInstances = new List<UnitSpellStatusPanel>();

    [SerializeField]
    UnitEquipmentStatusPanel _equipmentPanel = null;

    List<UnitEquipmentStatusPanel> _equipmentPanelInstances = new List<UnitEquipmentStatusPanel>();

    [SerializeField]
    Button _levelUpButton = null, _restButton = null, _cancelRestButton = null, _shopButton = null, _configureSpellsButton = null, _villageButton = null;

    [SerializeField]
    Image _unitAvatarImage = null;

    [SerializeField]
    Image _abilityIcon = null;

    [SerializeField]
    Vector3 _abilityOffset = new Vector3(16f, 0f);

    List<Image> _abilityIconInstances = new List<Image>();

    [SerializeField]
    TextMeshProUGUI _charName = null, _unitType = null, _unitLevel = null, _unitAlignment = null, _traits = null, _hp = null, _xp = null, _mv = null, _evasion = null, _armor = null, _res = null;

    [SerializeField]
    ScrollRect _abilityScrollRect = null;

    [SerializeField]
    Transform _abilityParentNoScroll = null;

    public string formatText = "{0}: {1}/{2}";
    public string formatEvasion = "{0}: {1}%";
    public string formatArmor = "{0}: {1}";

    bool _locked = false;
    public bool locked {
        get { return _locked; }
        set {
            if(value != _locked) {
                _locked = value;

                if(_locked == false) {
                    if(_queueUnit != null) {
                        Init(_queueUnit, _queueBaselineUnit);
                    } else if(_queueUnitInfo != null) {
                        Init(_queueUnitInfo, _queueBaselineUnit);
                    }
                }
            }
        }
    }

    Unit _queueUnit = null;
    UnitInfo _queueUnitInfo = null;
    UnitInfo _queueBaselineUnit = null;

    public void Refresh()
    {
        if(displayedUnit == null) {
            _baseLineUnit = null;
            gameObject.SetActive(false);
            return;
        }

        Init(displayedUnit, _baseLineUnit);
    }

    public void Init(Unit unit, UnitInfo baselineUnit=null)
    {
        if(locked) {
            _queueUnit = unit;
            _queueBaselineUnit = baselineUnit;
            _queueUnitInfo = null;
            return;
        }

        _queueUnit = null;
        _queueBaselineUnit = null;
        _queueUnitInfo = null;

        displayedUnit = unit;
        _mustHaveDisplayedUnit = true;

        Init(unit.unitInfo, baselineUnit);
        _unitAvatarImage.sprite = unit.avatarImage;

        if(_levelUpButton != null && unit.canLevelUp) {
            _levelUpButton.gameObject.SetActive(true);
            SetTooltip(_levelUpButton.image, "Level up this unit.");

        } else if(_restButton != null && unit.canRest) {
            _restButton.gameObject.SetActive(true);

            SetTooltip(_restButton.image, "Falls asleep until the end of next turn. If the unit is not interrupted from its sleep it awakens fully restored.");
        } else if(_cancelRestButton != null && unit.canCancelRest) {
            _cancelRestButton.gameObject.SetActive(true);
            SetTooltip(_cancelRestButton.image, "Undo the command for this unit to sleep. Sleeping units are very vulnerable to attack.");
        }

        if(_configureSpellsButton != null && unit.canChangeSpells) {
            _configureSpellsButton.gameObject.SetActive(true);
            SetTooltip(_configureSpellsButton.image, "Choose which spells to prepare");
        } else if(_configureSpellsButton != null) {
            _configureSpellsButton.gameObject.SetActive(false);
        }

        if(_shopButton != null && unit.teamInfo.GetMarketUnitHasAccessTo(unit) != null) {
            _shopButton.gameObject.SetActive(true);
            SetTooltip(_shopButton.image, "Shop in the market for items");
        } else if(_shopButton != null) {
            _shopButton.gameObject.SetActive(false);
        }

        if(_villageButton != null && unit.tile.terrain.rules.village && GameController.instance.gameState.GetOwnerOfLoc(unit.tile.loc) == unit.unitInfo.nteam) {
            _villageButton.gameObject.SetActive(true);
            SetTooltip(_villageButton.image, "Village information");
        } else if(_villageButton != null) {
            _villageButton.gameObject.SetActive(false);
        }
    }

    public static string GetAbilityTooltip(UnitInfo unitInfo, UnitAbility ability, string abilitySource)
    {
        int arg = 0;
        foreach(UnitAbilityArg abilityArg in unitInfo.abilityArgs) {
            if(abilityArg.ability == ability) {
                arg = abilityArg.arg;
            }
        }

        string tooltip = ability.tooltip;
        tooltip = tooltip.Replace("ARG", arg.ToString());
        return string.Format("<color=#FFFFFF>{0} ({1})</color><color=#AAAAAA>: {2}</color>", ability.description, abilitySource, tooltip);

    }

    public void Init(UnitType unitType)
    {
        Init(unitType.unitInfo);
    }




    public void Init(UnitInfo unitInfo, UnitInfo baselineUnit=null)
    {
        if(locked) {
            _queueUnit = null;
            _queueBaselineUnit = baselineUnit;
            _queueUnitInfo = unitInfo;
            return;
        }

        _queueUnit = null;
        _queueBaselineUnit = null;
        _queueUnitInfo = null;


        _baseLineUnit = baselineUnit;

        if(baselineUnit == null) {
            baselineUnit = unitInfo;
        }

        if(_levelUpButton != null) {
            _levelUpButton.gameObject.SetActive(false);
        }

        if(_restButton != null) {
            _restButton.gameObject.SetActive(false);
        }

        if(_cancelRestButton != null) {
            _cancelRestButton.gameObject.SetActive(false);
        }

        _unitAvatarImage.sprite = unitInfo.avatarImage;

        _charName.text = unitInfo.characterName;
        _unitType.text = unitInfo.unitType.description;
        _unitLevel.text = string.Format("Lvl {0}", unitInfo.level + unitInfo.amla);
        _unitAlignment.text = FormatUtil.Format(unitInfo.alignmentDescription, baselineUnit.alignmentDescription);

        string traitsTip = "";

        string traitsText = "";
        foreach(var trait in unitInfo.traits) {
            if(traitsText != "") {
                traitsText += ",";
                traitsTip += "\n";
            }

            traitsText += trait.traitName;

            traitsTip += string.Format("<color=#ffffff>{0}</color>: <color=#aaaaaa>{1}</color>", trait.traitName, trait.traitTooltip);
        }

        _traits.text = traitsText;

        SetTooltip(_traits, traitsTip);

        string hpText = FormatUtil.Cmp(unitInfo.hitpointsMax, baselineUnit.hitpointsMax, unitInfo.hitpointsMax, string.Format(formatText, "HP", unitInfo.hitpointsRemaining, unitInfo.hitpointsMax));
        string temporaryHitpointsText = "";
        if(unitInfo.temporaryHitpoints > 0) {
            temporaryHitpointsText = string.Format("<color=#FF79F0>Temporary: {0}</color>\n", unitInfo.temporaryHitpoints);
            hpText = string.Format("<color=#FF79F0>{0}+</color>{1}", unitInfo.temporaryHitpoints, hpText);
        }

        _hp.text = hpText;
        SetTooltip(_hp, "Hitpoints breakdown\n" + temporaryHitpointsText + unitInfo.hitpointsMaxCalc);

        _xp.text = FormatUtil.Cmp(baselineUnit.experienceMax, unitInfo.experienceMax, unitInfo.experienceMax, string.Format(formatText, "XP", unitInfo.experience, unitInfo.experienceMax));
        SetTooltip(_xp, "Experience breakdown\n" + unitInfo.experienceMaxCalc);

        _mv.text = FormatUtil.Cmp(unitInfo.movement, baselineUnit.movement, unitInfo.movementPermanent, string.Format(formatText, "MV", unitInfo.movementRemaining, unitInfo.movement));
        SetTooltip(_mv, "Movement breakdown\n" + unitInfo.movementCalc);

        if(_armor != null) {
            _armor.text = FormatUtil.Cmp(unitInfo.armor, baselineUnit.armor, unitInfo.armorPermanent, string.Format(formatArmor, "ARM", unitInfo.armor));
            SetTooltip(_armor, "Armor: Reduces physical damage\n" + unitInfo.armorCalc);
        }

        if(_res != null) {
            _res.text = FormatUtil.Cmp(unitInfo.resistance, baselineUnit.resistance, unitInfo.resistancePermanent, string.Format(formatArmor, "RES", unitInfo.resistance));
            SetTooltip(_res, "Resistance: Reduces magical damage\n" + unitInfo.resistanceCalc);
        }

        if(_evasion != null) {
            _evasion.text = FormatUtil.Cmp(unitInfo.evasion, baselineUnit.evasion, unitInfo.evasionPermanent, string.Format(formatEvasion, "EV", unitInfo.evasion));
            SetTooltip(_evasion, "Evasion: Chance of avoiding enemy attacks\n" + unitInfo.evasionCalc);
        }

        switch(unitInfo.alignment) {
            case UnitType.Alignment.Lawful:
                SetTooltip(_unitAlignment, "<color=#ffffff>Lawful</color>: +1 MV +1 Damage +5% ACC during Summer\n-1 MV -1 Damage, -5% ACC during Winter");
                break;
            case UnitType.Alignment.Chaotic:
                SetTooltip(_unitAlignment, "<color=#ffffff>Neutral</color>: -1 MV -1 Damage -5% ACC during Summer\n+1 MV +1 Damage, +5% ACC during Winter");
                break;
            case UnitType.Alignment.Neutral:
                SetTooltip(_unitAlignment, "<color=#ffffff>Neutral</color>: Unaffected by Seasons");
                break;
        }

        foreach(var item in _abilityIconInstances) {
            GameObject.Destroy(item.gameObject);
        }

        _abilityIconInstances.Clear();
        
        if(_abilityIcon != null) {
            _abilityIcon.gameObject.SetActive(false);

            List<UnitAbility> abilities = unitInfo.abilities;
            List<UnitAbilityArg> abilityArgs = unitInfo.abilityArgs;
            List<string> abilitySources = unitInfo.abilitySourceDescriptions;

            for(int i = 0; i != abilities.Count; ++i) {
                var icon = Instantiate(_abilityIcon, transform);
                icon.sprite = abilities[i].icon;
                icon.transform.localPosition += _abilityOffset*i;
                icon.gameObject.SetActive(true);

                if(baselineUnit.abilities.Contains(abilities[i]) == false) {
                    //highlight this icon to contrast from the baseline.
                    icon.material = GameConfig.instance.iconHighlightMaterial;
                }

                UnitStatusPanel.SetTooltip(icon, GetAbilityTooltip(unitInfo, abilities[i], abilitySources[i]));

                _abilityIconInstances.Add(icon);
            }
        }

        int numAbilityPanels = (_attackStatusPanel != null ? unitInfo.attacks.Count : 0) +
            (_spellStatusPanel != null ? unitInfo.spells.Count : 0);


        Transform abilityParent = numAbilityPanels <= 4 ?
            (_abilityParentNoScroll != null ? _abilityParentNoScroll : transform) :
            (_abilityScrollRect != null ? _abilityScrollRect.content.transform : transform);

        if(_abilityScrollRect != null) {
            _abilityScrollRect.gameObject.SetActive(numAbilityPanels > 4);
            _abilityScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64*numAbilityPanels);
        }

        foreach(var item in _attackStatusPanelInstances) {
            GameObject.Destroy(item.gameObject);
        }

        _attackStatusPanelInstances.Clear();

        float panelsPos = 0f;

        if(_attackStatusPanel != null) {
            var attacks = unitInfo.GetAttacks(true);
            var attacksPermanent = unitInfo.GetAttacks(true, true);

            for(int i = 0; i != attacks.Count; ++i) {
                var panel = Instantiate(_attackStatusPanel, abilityParent);
                panel.unitInfo = unitInfo;
                panel.attackInfo = attacks[i];
                panel.attackInfoPermanent = attacksPermanent[i];
                panel.baseline = panel.attackInfo;

                foreach(var attack in baselineUnit.attacks) {
                    if(attack.id == panel.attackInfo.id) {
                        panel.baseline = attack;
                        break;
                    }
                }

                panel.transform.localPosition -= new Vector3(0f, 1f, 0f)*panelsPos;
                panelsPos += _attackStatusPanel.height;
                panel.gameObject.SetActive(true);
                _attackStatusPanelInstances.Add(panel);
            }
        }

        foreach(var item in _spellStatusPanelInstances) {
            GameObject.Destroy(item.gameObject);
        }

        _spellStatusPanelInstances.Clear();

        if(_spellStatusPanel != null) {
            for(int i = 0; i != unitInfo.spells.Count; ++i) {
                var panel = Instantiate(_spellStatusPanel, abilityParent);
                panel.spell = unitInfo.spells[i];
                panel.expended = unitInfo.SpellOnCooldown(panel.spell);
                panel.transform.localPosition -= new Vector3(0f, 1f, 0f)*panelsPos;
                panelsPos += _attackStatusPanel.height;
                panel.gameObject.SetActive(true);
                _spellStatusPanelInstances.Add(panel);
            }
        }

        if(_spellStatus != null) {
            _spellStatus.Init(unitInfo);
        }

        foreach(var slot in _inventorySlots) {
            slot.Clear();
        }

        int inventorySlotIndex = 0;
        foreach(Equipment equip in unitInfo.equipment) {
            if(inventorySlotIndex < _inventorySlots.Count) {
                var slot = _inventorySlots[inventorySlotIndex];
                slot.SetEquipment(equip);
                ++inventorySlotIndex;
            }
        }

        foreach(var icon in _unitStatusIcons) {
            GameObject.Destroy(icon.gameObject);
        }

        _unitStatusIcons.Clear();

        if(_unitStatusIconPrefab != null) {

            float pos = 0f;
            foreach(UnitStatus status in unitInfo.status) {
                var icon = Instantiate(_unitStatusIconPrefab, _unitStatusParent);
                icon.sprite = status.icon;
                icon.transform.localPosition -= new Vector3(0f, 1f, 0f)*pos;
                pos += 24f;
                icon.gameObject.SetActive(true);
                _unitStatusIcons.Add(icon);

                SetTooltip(icon, status.GetTooltip(GameController.instance.GetUnitByGuid(unitInfo.guid)));
            }
        }
    }

    static public void SetTooltip(TextMeshProUGUI target, Dictionary<string,TooltipText.Options> options)
    {
        TooltipText tooltipText = target.GetComponent<TooltipText>();
        if(tooltipText == null) {
            tooltipText = target.gameObject.AddComponent<TooltipText>();
        }

        if(tooltipText != null) {
            tooltipText.linkOptions = options;
        }
    }

    static public void SetTooltip(TextMeshProUGUI target, string text, TooltipText.Options options=null)
    {
        TooltipText tooltipText = target.GetComponent<TooltipText>();
        if(tooltipText == null) {
            tooltipText = target.gameObject.AddComponent<TooltipText>();
        }

        if(tooltipText != null) {
            if(options != null) {
                tooltipText.options = options;
            }

            tooltipText.tooltipText = text;
        }
    }

    static public void SetTooltip(Image target, string text, TooltipText.Options options=null)
    {
        TooltipText tooltipText = target.GetComponent<TooltipText>();
        if(tooltipText == null) {
            tooltipText = target.gameObject.AddComponent<TooltipText>();
        }

        if(tooltipText != null) {
            if(options != null) {
                tooltipText.options = options;
            }

            tooltipText.tooltipText = text;
        }
    }

    static public void ClearTooltip(TextMeshProUGUI target)
    {
        SetTooltip(target, null);
    }


    static public void ClearTooltip(Image target)
    {
        SetTooltip(target, null);
    }

    static public void MinimizeTextWidth(TextMeshProUGUI target)
    {
        RectTransform statsRT = target.GetComponent<RectTransform>();

        Vector2 statsDim = target.GetPreferredValues(target.text, statsRT.sizeDelta.x, statsRT.sizeDelta.y);
        target.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, statsDim.x);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_mustHaveDisplayedUnit && displayedUnit == null) {
            gameObject.SetActive(false);
        }
    }
}
