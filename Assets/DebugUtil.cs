using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugUtil : MonoBehaviour
{
    public bool resetPlayerXP = false;

    // Start is called before the first frame update
    void Start()
    {
        if(resetPlayerXP) {
            PlayerPrefs.SetInt("PlayerLevel", 0);
            PlayerPrefs.SetInt("PlayerXP", 0);
            PlayerPrefs.SetInt("TeamsUnlocked", 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
