using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Titlescreen : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI _versionText = null, _gamesBeingPlayedText = null;

    // Start is called before the first frame update
    void Start()
    {
        _versionText.text = string.Format("Version {0}", GameConfig.instance.gameVersion);
    }

    // Update is called once per frame
    void Update()
    {
        var openSessions = CloudInterface.instance.openSessions;
        if(openSessions != null) {
            _gamesBeingPlayedText.text = string.Format("{0} players playing right now.", openSessions.Count);
        }
    }
}
