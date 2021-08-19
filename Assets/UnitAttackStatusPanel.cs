using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitAttackStatusPanel : MonoBehaviour
{
    public UnitInfo unitInfo;
    public AttackInfo attackInfo;
    public AttackInfo attackInfoPermanent;

    public AttackInfo baseline;

    public int height = 64;

    [SerializeField]
    Image _image = null;

    [SerializeField]
    Image _rangeIcon = null;

    [SerializeField]
    TextMeshProUGUI _nameText = null, _statsText = null, _rangeText = null;

    [SerializeField]
    RectTransform _abilitiesTransform = null;

    [SerializeField]
    Image[] _abilityIcons = null;

    [SerializeField]
    Sprite _rangeMelee = null, _rangeRanged = null;

    [SerializeField]
    Image[] _chargeIcons = null;

    [SerializeField]
    Sprite _iconChargeAvailable = null, _iconChargeExpended = null;

    // Start is called before the first frame update
    void Start()
    {
        _image.sprite = attackInfo.icon;
        _nameText.text = attackInfo.description;

        if(_rangeText != null) {
            _rangeText.text = attackInfo.rangeDescription;
        }

        if(_rangeIcon != null) {
            _rangeIcon.sprite = attackInfo.range == AttackInfo.Range.Melee ? _rangeMelee : _rangeRanged;
            UnitStatusPanel.SetTooltip(_rangeIcon, attackInfo.rangeDescription);
        }

        int accuracyMod = attackInfo.accuracy;
        string accuracyDesc = accuracyMod < 0 ? string.Format("{0}", accuracyMod) : string.Format("+{0}", accuracyMod);
        string accuracyFormatted = "";
        if(accuracyMod > 0) {
            accuracyFormatted = string.Format("<color=#66AA66>{0}%</color>", accuracyDesc);
        } else if(accuracyMod < 0) {
            accuracyFormatted = string.Format("<color=#AA6666>{0}%</color>", accuracyDesc);
        }

        accuracyFormatted = FormatUtil.Cmp(attackInfo.accuracy, baseline.accuracy, attackInfoPermanent.accuracy, accuracyFormatted);

        string critTooltip = "";
        string critFormatted = "";
        if(attackInfo.critical > 0) {
            critFormatted = string.Format("\n<color=#66AA66>{0}% crit</color>", attackInfo.critical);
            critTooltip = string.Format("\n{0}% chance of critical hit", attackInfo.critical);

            critFormatted = FormatUtil.Cmp(attackInfo.critical, baseline.critical, attackInfoPermanent.critical, critFormatted);
        }

        string damageFormatted = FormatUtil.Cmp(attackInfo.damage, baseline.damage, attackInfoPermanent.damage, attackInfo.damage.ToString());
        string nstrikesFormatted = FormatUtil.Cmp(attackInfo.nstrikes, baseline.nstrikes, attackInfoPermanent.nstrikes, attackInfo.nstrikes.ToString());

        _statsText.text = string.Format("{0}x{1}{2}{3}", damageFormatted, nstrikesFormatted, accuracyFormatted, critFormatted);

        string mods = "";
        foreach(AttackModLog logInfo in attackInfo.modLog) {
            if(string.IsNullOrEmpty(mods) == false) {
                mods += "\n";
            }

            string value = (logInfo.delta > 0 ? "+" : "") + logInfo.delta + (logInfo.field == "ACC" || logInfo.field == "CRIT" ? "%" : "");
            mods += string.Format("{0}: {1} {2}", logInfo.description, logInfo.field, value);
        }

        if(string.IsNullOrEmpty(mods) == false) {
            mods += "\n---\n";
        }
        UnitStatusPanel.SetTooltip(_statsText, string.Format(mods + "{0} damage\n{1} attacks\n{2}% accuracy{3}", attackInfo.damage, attackInfo.nstrikes, accuracyDesc, critTooltip));
        UnitStatusPanel.MinimizeTextWidth(_statsText);

        List<AttackAbility> attackAbilities = new List<AttackAbility>(attackInfo.abilities);

        if(attackInfo.attackType == AttackInfo.AttackType.Magical) {
            attackAbilities.Add(GameConfig.instance.magicAttackDummyAbility);
        }

        for(int i = 0; i < _abilityIcons.Length; ++i) {
            if(i < attackAbilities.Count) {
                var ability = attackAbilities[i];
                _abilityIcons[i].sprite = ability.icon;
                string tipDescription = string.Format(ability.tooltip, attackInfo.GetAbilityParam(ability));
                UnitStatusPanel.SetTooltip(_abilityIcons[i], string.Format("{0}: {1}", ability.description, tipDescription));
                _abilityIcons[i].gameObject.SetActive(true);
            }
        }

        int chargesExpended = unitInfo.GetAttacksExpended(attackInfo.id);
        for(int i = 0; i < attackInfo.numCharges; ++i) {
            if(_chargeIcons != null && i < _chargeIcons.Length) {
                _chargeIcons[i].gameObject.SetActive(true);
                if(i < chargesExpended) {
                    _chargeIcons[i].sprite = _iconChargeExpended;
                } else {
                    _chargeIcons[i].sprite = _iconChargeAvailable;
                }

                UnitStatusPanel.SetTooltip(_chargeIcons[i], string.Format("Charges: {0}/{1}. Charges are refreshed by resting in a village.", attackInfo.numCharges-chargesExpended, attackInfo.numCharges));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
