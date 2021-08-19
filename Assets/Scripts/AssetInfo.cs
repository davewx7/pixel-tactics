using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetInfo : ScriptableObject
{
    public static AssetInfo instance {
        get {
#if UNITY_EDITOR
            if(UnityEditor.EditorApplication.isPlaying == false)
                return AssetDatabase.LoadAssetAtPath<AssetInfo>("Assets/GameScriptableObjects/AssetInfo.asset");
#endif
            return GameConfig.instance.assetInfo;

        }
    }

    public bool RecordScriptableObject(GWScriptableObject obj)
    {
        bool dirty = false;
        if(allSerializableObjects.Contains(obj) == false) {
            allSerializableObjects.Add(obj);
            dirty = true;
        }
        Equipment equip = obj as Equipment;
        if(equip != null) {
            if(allEquipment.Contains(equip) == false) {
                allEquipment.Add(equip);
                dirty = true;
            }
        }

        UnitSpell spell = obj as UnitSpell;
        if(spell != null) {
            if(allSpells.Contains(spell) == false) {
                allSpells.Add(spell);
                dirty = true;
            }
        }

        if(dirty) {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        return dirty;
    }

    public List<GWScriptableObject> allSerializableObjects = new List<GWScriptableObject>();
    public List<Equipment> allEquipment = new List<Equipment>();

    public List<UnitSpell> allSpells = new List<UnitSpell>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
