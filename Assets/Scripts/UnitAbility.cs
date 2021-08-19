using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/UnitAbility")]
public class UnitAbility : GWScriptableObject
{
    public string description;
    public Sprite icon;

    [TextArea(3,5)]
    public string tooltip;

    public UnitMod unitMod;

    public List<TerrainRules> disadvantageInTerrain;
}

[System.Serializable]
public struct UnitAbilityArg
{
    public UnitAbility ability;
    public int arg;
}