using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class FormatUtil : MonoBehaviour
{
    public static string Format(string str, string baseline)
    {
        if(baseline == null || str == baseline) {
            return str;
        }

        return string.Format("<color=#9999ff>{0}</color>", str);
    }

    public static string Cmp(int attr, int baselineAttr, int permanentAttr, string str)
    {
        string prefix = "";
        string postfix = "";
        string color = "ffffff";
        if(attr > baselineAttr) {
            color = "99ff99";
            prefix = "<b>";
            postfix = "</b>";
        } else if(attr < baselineAttr) {
            color = "ff9999";
            prefix = "<b>";
            postfix = "</b>";
        } else if(attr > permanentAttr) {
            color = "aaffaa";
        } else if(attr < permanentAttr) {
            color = "ffaaaa";
        }

        return string.Format(prefix + "<color=#{0}>{1}</color>" + postfix, color, str);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
