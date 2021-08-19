using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/TeamColoring")]
public class TeamColoring : GWScriptableObject
{
    public Color color;

    public float hue {
        get {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            return h;
        }
    }

    public Vector3 hsv {
        get {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            return new Vector3(h, s, v);
        }
    }

}

