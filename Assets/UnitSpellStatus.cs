using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpellStatus : MonoBehaviour
{
    [SerializeField]
    List<InventorySlotDisplay> _slots = null;

    public void Init(UnitInfo unitInfo)
    {
        if(unitInfo.spells.Count == 0) {
            gameObject.SetActive(false);
        } else {
            gameObject.SetActive(true);
            
            for(int i = 0; i != _slots.Count; ++i) {
                var slot = _slots[i];
                if(i < unitInfo.spells.Count) {
                    slot.gameObject.SetActive(true);
                    slot.SetSpell(unitInfo.spells[i]);
                } else {
                    slot.gameObject.SetActive(false);
                }
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
