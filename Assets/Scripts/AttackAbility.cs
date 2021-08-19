using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/AttackAbility")]
public class AttackAbility : GWScriptableObject
{
    public string description;
    public Sprite icon;

    [TextArea(3, 5)]
    public string tooltip;

    public int defaultParam;

    public UnitStatus applyStatus = null;

    public float AIMultiplier = 2.0f;
}
