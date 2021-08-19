using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Item : GWScriptableObject
{
    [AssetSelector(Paths = "Assets/UITextures/icons/attacks|Assets/UITextures/icons/items", FlattenTreeView = true)]
    public Sprite icon;
    public float hueShift = 0f;
    public string description;
    
    //"a sword" instead of sword.
    public string descriptionAsArticle {
        get {
            string vowels = "AEIOUaeiou";
            if(vowels.Contains(description.Substring(0,1))) {
                return string.Format("an {0}", description);
            } else {
                return string.Format("a {0}", description);
            }
        }
    }

    public string summaryRules;
    public string tooltipRules;

    public InventorySlot slot;
    public bool consumable = false;

    //if true this can be consumed immediately upon purchase.
    public virtual bool canConsumeImmediately {
        get {
            return false; 
        }
    }

    public Material CreateMaterial()
    {
        Material equipMaterial = new Material(GameConfig.instance.GetMaterialForInventorySlot(tier, false));
        equipMaterial.SetFloat("_hueshift", hueShift);
        return equipMaterial;
    }

    public int tier = 1;
}

[CreateAssetMenu(menuName = "Wesnoth/Equipment")]
public class Equipment : Item
{
    //which races make this kind of equipment?
    [AssetList(Path = "/GameScriptableObjects/Teams")]
    public List<Team> teamAssociation;

    public TooltipText.Options CreateTooltip()
    {
        return new TooltipText.Options() {
            text = FullTooltip(),
            icon = icon,
            iconSize = new Vector2(64f, 64f),
            iconMaterial = CreateMaterial(),
            useMousePosition = true,
        };
    }

    public string FullTooltip()
    {
        string cursedText = "";
        if(cursed) {
            cursedText = "<color=#ff0000>Cursed: Cannot be sold or discarded.</color>\n";
        }

        string tooltipText = string.Format("<color=#ffffff>{0}</color> ({1}):\n{2}<color=#aaaaaa>{3}</color>", description, slot.description, cursedText, GetToolTip());
        return tooltipText;
    }

    public string GetToolTip()
    {
        string result = tooltipRules;
        if(activatedAbility) {
            if(activatedAbility.consumable) {
                result += "\n<color=#aaaaaa>Consumed when used</color>";
            } else if(activatedAbility.cooldown < 0) {
                result += "\n<color=#aaaaaa>Must rest after use to use again</color>";
            } else if(activatedAbility.cooldown <= 1) {
                result += "\n<color=#aaffaa>May use each round</color>";
            } else {
                result += string.Format("\n<color=#aaaaaa>Cooldown: {0} Moons. (Resting always refreshes cooldowns)</color>", activatedAbility.cooldown);
            }
        }

        return result;
    }

    public UnitSpell activatedAbility;

    public UnitSpell beginTurnEffect;

    //Whenever we cast a spell on a unit, this spell is also triggered on them.
    public UnitSpell castSpellEffect;

    public UnitMod mod = new UnitMod();

    public List<TerrainRules> advantagedTerrain = new List<TerrainRules>();
    public List<TerrainRules> disadvantagedTerrain = new List<TerrainRules>();

    public List<string> attackRequired;

    public override bool canConsumeImmediately {
        get {
            return consumable && activatedAbility != null && activatedAbility.targetTypes.Count == 1 && activatedAbility.targetTypes[0] == UnitSpell.TargetType.Self;
        }
    }


    public bool cursed {
        get {
            return tier == -1;
        }
    }

    [SerializeField]
    int _price = -1;

    public int price {
        get {
            if(_price > 0) {
                return _price;
            }
            return tier*10;
        }
    }

    public bool EquippableForUnit(UnitInfo unit, out string reason)
    {
        Equipment equip = this;
        reason = null;

        int maxSlots = 4;
        if(unit.equipment.Count >= maxSlots) {
            reason = "This unit cannot carry any more";
            return false;
        }

        bool unitCanCarryMultiple = unit.isAlchemist && (equip.slot == GameConfig.instance.inventorySlotConsumables);

        if(unitCanCarryMultiple == false) {
            foreach(var cur in unit.equipment) {
                if(cur.slot == equip.slot) {
                    reason = (cur == equip) ? "This unit already owns this item" : string.Format("This unit already owns {0}", equip.slot.describeAsArticle);
                    return false;
                }
            }
        }

        if(equip.slot.weapon) {
            //Can only equip a weapon if it actually does something.
            if(equip.mod.useMod && equip.mod.attackMods.Count > 0) {
                bool foundMod = false;
                foreach(var attackMod in equip.mod.attackMods) {
                    foreach(var atk in unit.attacks) {
                        if(attackMod.AppliesToAttack(atk)) {
                            foundMod = true;
                        }
                    }
                }

                if(foundMod == false) {
                    reason = "This unit cannot use " + equip.slot.describePlural;
                    return false;
                }
            }
        }

        return true;
    }


    public bool EquippableForUnit(UnitInfo unit)
    {
        string str;
        return EquippableForUnit(unit, out str);
    }

    static bool _checkedEquipment = false;
    static public List<Equipment> all {
        get {
            if(_checkedEquipment == false) {
                _checkedEquipment = true;
                AssetInfo.instance.allEquipment.RemoveAll(a => (a == null));
            }
            return AssetInfo.instance.allEquipment;
        }
    }
    public override void Init()
    {
        mod.description = description;
    }

}
