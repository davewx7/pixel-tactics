using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootItem : MonoBehaviour
{
    public LootInfo lootInfo = null;

    Loot _loot = null;

    public Loot loot {
        get { return _loot; }
    }

    public void SetLoot(Loot loot)
    {
        _loot = loot;
        _loot.InitLootItem(this);
    }

    public void AnimEnter(Unit unit)
    {
        if(_loot != null && unit.playerControlled) {
            _loot.OpenAnim(this);
        }
    }

    public void AnimClose()
    {
        _loot.CloseAnim(this);
    }


    public void Loot(Unit unit)
    {
        if(_loot != null && unit.playerControlled) {
            _loot.GetLoot(unit, this);

            if(lootInfo == null || lootInfo.equipment == null || lootInfo.equipment.Count == 0) {
                _loot = null;
            }
        }
    }

    public SpriteRenderer renderer;
}
