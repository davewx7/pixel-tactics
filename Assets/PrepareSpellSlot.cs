using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PrepareSpellSlot : InventorySlotDisplay
{
    [SerializeField]
    SpellsDialog _dialog = null;

    [SerializeField]
    Image _cursor = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _levelText = null;

    public int slotLevel = 1;


    bool _selected = false;
    public bool selected {
        get {
            return _selected;
        }
        set {
            _selected = value;
            _cursor.gameObject.SetActive(_selected);
        }
    }

    public override void Clicked()
    {
        _dialog.SetSelectedSlot(this);
    }

    public override bool interactable {
        get {
            return true;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        _levelText.text = string.Format("lvl{0}", slotLevel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
