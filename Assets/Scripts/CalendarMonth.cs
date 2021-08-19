using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/CalendarMonth")]
public class CalendarMonth : GWScriptableObject
{
    public string description;
    public string ordinal;

    public CalendarSeason season;
}
