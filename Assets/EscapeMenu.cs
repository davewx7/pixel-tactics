using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI _textGameGuid = null;

    [SerializeField]
    TMPro.TMP_InputField _textGameGuidInput = null;

    [SerializeField]
    Transform[] _nonSpectatorControls = null, _spectatorControls = null;

    private void OnEnable()
    {
        ++GameConfig.modalDialog;

        //set controls based on whether we're spectating or not.
        foreach(Transform c in _spectatorControls) {
            c.gameObject.SetActive(GameController.instance.spectating);
        }

        foreach(Transform c in _nonSpectatorControls) {
            c.gameObject.SetActive(!GameController.instance.spectating);
        }


        _textGameGuidInput.text = string.Format("GAME INFO\nVersion: {0}\nSeed: {1}\nGuid: {2}", GameConfig.instance.gameVersion, GameController.instance.gameState.seed, GameController.instance.gameState.guid);
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
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
