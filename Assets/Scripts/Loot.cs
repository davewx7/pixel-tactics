using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootInfo
{
    public string description = "";
    public List<Equipment> equipment = null;
}

[CreateAssetMenu(menuName = "Wesnoth/Loot")]
public class Loot : GWScriptableObject
{
    [SerializeField]
    Sprite _image = null, _imageOpen = null;

    public int amount = 20;

    public void InitLootItem(LootItem lootItem)
    {
        lootItem.renderer.sprite = _image;
        lootItem.gameObject.SetActive(true);
    }

    public void CloseAnim(LootItem lootItem)
    {
        lootItem.renderer.sprite = _image;
    }

    public void OpenAnim(LootItem lootItem)
    {
        if(_imageOpen != null) {
            lootItem.renderer.sprite = _imageOpen;
        }
    }

    string DescribeEquipment(List<Equipment> equip)
    {
        string result = "";
        for(int i = 0; i != equip.Count; ++i) {
            if(equip.Count > 1 && i == equip.Count-1) {
                result += " and";
            }

            result += " a " + equip[i].description;
        }

        return result;
    }

    public virtual void GetLoot(Unit claimingUnit, LootItem lootItem)
    {
        List<Equipment> couldNotPickUp = new List<Equipment>();

        if(amount > 0) {
            GameController.instance.ShowDialogMessage("Gold", string.Format("You have found {0} gold", amount));
            claimingUnit.teamInfo.EarnGold(amount);
        } else if(lootItem.lootInfo != null && lootItem.lootInfo.equipment.Count > 0) {

            List<Equipment> pickUp = new List<Equipment>();

            foreach(var equip in lootItem.lootInfo.equipment) {
                if(equip.EquippableForUnit(claimingUnit.unitInfo)) {
                    GameController.instance.ExecuteGrantEquipment(claimingUnit, equip);
                    pickUp.Add(equip);
                } else {
                    couldNotPickUp.Add(equip);
                }
            }

            if(pickUp.Count == 0) {
                if(couldNotPickUp.Count > 1) {
                    GameController.instance.ShowDialogMessage("Cannot Equip", string.Format("You found {0} but they cannot be equipped by this unit.", DescribeEquipment(couldNotPickUp)));
                } else {
                    GameController.instance.ShowDialogMessage("Cannot Equip", string.Format("You found {0} but it cannot be equipped by this unit.", DescribeEquipment(couldNotPickUp)));
                }
            } else {
                string couldNotPickUpExplanation = "";
                if(couldNotPickUp.Count > 0) {
                    couldNotPickUpExplanation = string.Format(" You also found {0} but it could not be equipped.", DescribeEquipment(couldNotPickUp));
                }

                GameController.instance.ShowDialogMessage("Equipment Recovered", string.Format("You recovered {0}.{1}", DescribeEquipment(pickUp), couldNotPickUpExplanation));
            }
        }

        if(couldNotPickUp.Count == 0) {
            lootItem.lootInfo.equipment.Clear();
            lootItem.gameObject.SetActive(false);
        } else {
            if(lootItem.lootInfo != null) {
                lootItem.lootInfo.equipment = couldNotPickUp;
            }
            lootItem.AnimClose();
        }
    }
}
