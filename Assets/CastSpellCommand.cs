using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CastSpellCommandInfo
{
    public string unitGuid;
    public UnitSpell spell;
    public Loc target;
}

public class CastSpellCommand : GameCommand
{
    public CastSpellCommandInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<CastSpellCommandInfo>(data); }


    // Start is called before the first frame update
    void Start()
    {
        Unit caster = GameController.instance.GetUnitByGuid(info.unitGuid);
        bool canCast = false;

        for(int i = 0; i != caster.unitInfo.spells.Count; ++i) {
            var spell = caster.unitInfo.spells[i];

            if(spell == info.spell && caster.unitInfo.SpellOnCooldown(spell) == false) {
                caster.unitInfo.ExpendSpell(spell);
                canCast = true;
                caster.AwardExperience(4);
                break;
            }
        }

        for(int i = 0; i != caster.unitInfo.equipment.Count; ++i) {
            var spell = caster.unitInfo.equipment[i].activatedAbility;

            if(spell == info.spell && caster.unitInfo.SpellOnCooldown(spell) == false) {
                if(caster.unitInfo.equipment[i].consumable) {
                    caster.unitInfo.equipment.RemoveAt(i);
                    caster.unitInfo.tired = true;
                } else {
                    caster.unitInfo.ExpendSpell(spell);
                }
                canCast = true;
                break;
            }
        }
        
        if(canCast) {
            caster.PlayCastAnim();

            if(info.spell.usesAttack) {
                caster.unitInfo.hasAttacked = true;
            }

            //caster.unitInfo.movementRemaining = 0;
            info.spell.CompleteCasting(caster, info.target);
        }

        GameController.instance.RefreshUnitDisplayed();
        finished = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
