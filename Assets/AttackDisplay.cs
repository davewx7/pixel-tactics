using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttackDisplay : MonoBehaviour
{
    public Image iconImage;

    [SerializeField]
    TextMeshProUGUI _nameText, _strikesText, _chanceText;

    public void Init(UnitInfo unitInfo, AttackInfo? attackInfoRef)
    {
        if(attackInfoRef == null) {
            _nameText.text = "";
            _strikesText.text = "(none)";
            _chanceText.text = "";
        } else {
            AttackInfo attackInfo = attackInfoRef.Value;

            AttackInfo baseAttack = new AttackInfo();
            foreach(AttackInfo attack in unitInfo.unitType.attacks) {
                if(attack.id == attackInfo.id) {
                    baseAttack = attack;
                    break;
                }
            }

            _nameText.text = attackInfo.description;
            _strikesText.text = string.Format("{0}x{1} <color=#66AA66>{2}</color>", attackInfo.damage, attackInfo.nstrikes, attackInfo.attackEffectsDescription);

            string damageTooltip = string.Format("Base DAM: {0}\n", baseAttack.damage);
            string accuracyTooltip = string.Format("Base HIT: 100%\nBase ACC: {0}{1}%\n", baseAttack.accuracy > 0 ? "+" : "", baseAttack.accuracy);
            string criticalTooltip = string.Format("Base CRIT: {0}%\n", baseAttack.critical);

            string strikesInfo = "";

            bool showCrit = baseAttack.critical != 0;

            if(attackInfo.modLog != null) {
                foreach(var mod in attackInfo.modLog) {
                    if(mod.field == "DAM") {
                        damageTooltip += string.Format("{0}: {1}{2}\n", mod.description, mod.delta > 0 ? "+" : "", mod.delta);
                    } else if(mod.field == "ACC") {
                        accuracyTooltip += string.Format("{0}: {1}{2}%\n", mod.description, mod.delta > 0 ? "+" : "", mod.delta);
                    } else if(mod.field == "CRIT") {
                        showCrit = true;
                        criticalTooltip += string.Format("{0}: {1}{2}%\n", mod.description, mod.delta > 0 ? "+" : "", mod.delta);
                    } else if(mod.field == "STRIKES") {
                        strikesInfo += string.Format("\n{0}: {1}{2} STRIKES", mod.description, mod.delta > 0 ? "+" : "", mod.delta);
                    } else {
                        Debug.LogError("Unknown attack field mod: " + mod.field);
                    }
                }
            }

            damageTooltip += string.Format("Total DAM: {0}{1}", attackInfo.damage, strikesInfo);
            accuracyTooltip += string.Format("Total HIT: {0}%", 100 + attackInfo.accuracy);
            criticalTooltip += string.Format("Total CRIT: {0}%\nCRIT DAM: {1}\n(+100% DAM, ignores armor)", attackInfo.critical, attackInfo.criticalDamage);

            if(showCrit) {
                accuracyTooltip += "\n\n" + criticalTooltip;
            }

            string critChance = "";
            if(attackInfo.critical > 0) {
                critChance = string.Format(" <color=#66AA66>({0}% crit)</color>", attackInfo.critical);
            }

            _chanceText.text = string.Format("{0}%{1}", Mathf.Max(0, Mathf.Min(100, 100 + attackInfo.accuracy)), critChance);

            if(attackInfo.icon != null) {
                iconImage.sprite = attackInfo.icon;
            }

            UnitStatusPanel.SetTooltip(_strikesText, damageTooltip);
            UnitStatusPanel.SetTooltip(_chanceText, accuracyTooltip);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
