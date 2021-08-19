using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/InventorySlot")]
public class InventorySlot : GWScriptableObject
{
    public string description;
    public string describeAsArticle;
    public string describePlural;
    public Sprite sprite;
    public bool weapon = false;
}
