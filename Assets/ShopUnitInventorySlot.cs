using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUnitInventorySlot : InventorySlotDisplay
{
    public bool selling = false;

    [SerializeField]
    ShopDialog _dialog = null;

    public override bool interactable {
        get {
            return true;
        }
    }

    public override bool inShop {
        get {
            return true;
        }
    }


    public override void Clicked()
    {
        if(equipment != null) {
            if(selling) {
                _dialog.BuyEquipment(equipment);
            } else {
                _dialog.SellEquipment(equipment);
            }
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
