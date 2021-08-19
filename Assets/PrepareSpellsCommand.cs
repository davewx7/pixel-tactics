using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PrepareSpellsCommandInfo
{
    public string unitGuid;
    public List<UnitSpell> spells;
}


public class PrepareSpellsCommand : GameCommand
{
    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<PrepareSpellsCommandInfo>(data); }

    public PrepareSpellsCommandInfo info;

    // Start is called before the first frame update
    void Start()
    {
        Unit target = GameController.instance.GetUnitByGuid(info.unitGuid);
        if(target != null) {
            List<int> spellsExpended = new List<int>();
            for(int i = 0; i != target.unitInfo.spells.Count; ++i) {
                if(target.unitInfo.SpellOnCooldown(target.unitInfo.spells[i])) {
                    spellsExpended.Add(i);
                }
            }

            target.unitInfo.spellsChangedThisTurn = true;
            target.unitInfo.spells = new List<UnitSpell>(info.spells);
            for(int i = 0; i != target.unitInfo.spells.Count; ++i) {
                if(spellsExpended.Contains(i)) {
                    target.unitInfo.PutSpellOnCooldown(target.unitInfo.spells[i]);
                }
            }
        }
        finished = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
