using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Wesnoth/Boon/LearnSpells")]
public class BoonLearnSpells : Boon
{
    [SerializeField]
    [AssetList(Path = "/GameScriptableObjects/UnitSpells/Schools")]
    List<SpellSchool> _schools = null;

    [SerializeField]
    [TextArea(3,3)]
    string _acceptMessage = null;

    public int numSpells = 2;

    List<UnitSpell> CandidateSpells(Unit unit)
    {
        List<UnitSpell> candidateSpells = new List<UnitSpell>();
        var knownSpells = unit.teamInfo.GetKnownSpells(unit.unitInfo);
        foreach(UnitSpell spell in UnitSpell.all) {

            bool matchesSchool = false;
            foreach(var school in spell.schools) {
                if(_schools.Contains(school)) {
                    matchesSchool = true;
                }
            }

            if(matchesSchool == false) {
                continue;
            }

            if(knownSpells.Contains(spell) == false && unit.unitInfo.CanCastSpell(spell)) {
                candidateSpells.Add(spell);
            }
        }

        return candidateSpells;
    }

    public override bool IsEligible(Unit unit)
    {
        bool schoolOverlap = false;
        List<SpellSchool> unitSchools = unit.unitInfo.unitType.unitInfo.spellSchoolsKnown;
        foreach(var s in unitSchools) {
            if(_schools.Contains(s)) {
                schoolOverlap = true;
                break;
            }
         }

        if(schoolOverlap == false) {
            return false;
        }

        var candidates = CandidateSpells(unit);
        return candidates.Count >= numSpells;
    }

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        base.Award(info, unit);

        if(info.interactable == false) {
            return;
        }

        ConsistentRandom rng = new ConsistentRandom(info.seed);
        var spells = CandidateSpells(unit);

        for(int i = 0; i < numSpells && spells.Count > 0; ++i) {
            int index = rng.Next(spells.Count);

            unit.teamInfo.LearnSpell(spells[index]);
            spells.RemoveAt(index);
        }

        GenericCommandInfo cmd = GameController.instance.QueueGenericCommand();
        AIDiplomacyManager.instance.StartCoroutine(AIDiplomacyManager.instance.LearnSpells(unit, cmd, _acceptMessage));
    }
}
