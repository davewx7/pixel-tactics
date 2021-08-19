using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/TileFogData")]

public class TileFogData : GWScriptableObject
{
    public Sprite[] fog;
    public Sprite[] adj;
}
