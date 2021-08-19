using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/CalendarSeason")]
public class CalendarSeason : GWScriptableObject
{
    public Season season;
    public string description;

    [System.Serializable]
    public struct AlignmentMod
    {
        public UnitType.Alignment alignment;
        public UnitMod mod;
    }

    public AlignmentMod[] alignmentMods;

    public UnitMod GetAlignmentMod(UnitType.Alignment alignment)
    {
        for(int i = 0; i < alignmentMods.Length; ++i) {
            if(alignmentMods[i].alignment == alignment) {
                return alignmentMods[i].mod;
            }
        }

        return null;
    }
}
