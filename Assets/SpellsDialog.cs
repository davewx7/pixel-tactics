using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellsDialog : MonoBehaviour
{
    public Unit unit;

    [SerializeField]
    Transform _panel = null;

    int _selectedIndex = 0;
    public void SetSelectedSlot(PrepareSpellSlot slot)
    {
        for(int i = 0; i != _slots.Count; ++i) {
            bool selected = _slots[i] == slot;
            if(selected) {
                _selectedIndex = i;
            }

            _slots[i].selected = selected;
        }

        CalculateInteractable();
    }

    [SerializeField]
    List<PrepareSpellSlot> _slots = new List<PrepareSpellSlot>();

    public List<UnitSpell> preparedSpells {
        get {
            List<UnitSpell> result = new List<UnitSpell>();
            foreach(var slot in _slots) {
                if(slot.gameObject.activeSelf && slot.spell != null) {
                    result.Add(slot.spell);
                }
            }

            return result;
        }
    }

    [SerializeField]
    SpellItemEntry _entryProto = null;

    List<SpellItemEntry> _entries = new List<SpellItemEntry>();

    public void SpellItemEntryClicked(SpellItemEntry entry)
    {
        _slots[_selectedIndex].SetSpell(entry.spell);
        CalculateInteractable();
    }

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    public void Confirm()
    {
        unit.teamInfo.newSpells.Clear();
        GameController.instance.ExecutePrepareSpells(unit, preparedSpells);
        GameController.instance.ClosePrepareSpells();
    }


    public void Cancel()
    {
        unit.teamInfo.newSpells.Clear();
        GameController.instance.ClosePrepareSpells();
    }

    List<UnitSpell> availableSpells {
        get {
            return unit.teamInfo.GetKnownSpells(unit.unitInfo);
        }
    }

    public void CalculateInteractable()
    {
        var spells = preparedSpells;
        foreach(var entry in _entries) {
            bool interactable = spells.Contains(entry.spell) == false && unit.unitInfo.unitType.unitInfo.spells[_selectedIndex].spellLevel >= entry.spell.spellLevel;
            entry.interactable = interactable;
        }
    }

    public void Populate()
    {
        int index = 0;
        foreach(UnitSpell spell in unit.unitInfo.spells) {
            var baseSpell = unit.unitInfo.unitType.unitInfo.spells[index];

            var slot = _slots[index];
            slot.slotLevel = baseSpell.spellLevel;
            slot.gameObject.SetActive(true);
            slot.SetSpell(spell);
            ++index;
        }

        for(; index < _slots.Count; ++index) {
            _slots[index].gameObject.SetActive(false);
        }


        float xpos = 0f, ypos = 0f;

        foreach(UnitSpell spell in availableSpells) {

            SpellItemEntry entry = Instantiate(_entryProto, _panel);

            if(unit.teamInfo.newSpells.Contains(spell)) {
                entry.SetStatus("<color=#ffff99><i>New!</i></color>");
            }

            entry.GetComponent<RectTransform>().anchoredPosition += new Vector2(xpos, ypos);
            entry.spell = spell;
            entry.gameObject.SetActive(true);

            _entries.Add(entry);

            ypos -= 80f;
            if(ypos < -450f) {
                xpos += 200f;
                ypos = 0f;
            }
        }

        CalculateInteractable();
    }

    // Start is called before the first frame update
    void Start()
    {
        Populate();
        SetSelectedSlot(_slots[0]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
