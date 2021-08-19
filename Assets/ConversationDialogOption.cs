using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationDialogOption : SelectablePanel
{
    [SerializeField]
    ConversationDialog _dialog = null;

    public TMPro.TextMeshProUGUI text;
    public int optionNum = 0;
    public override void OnClicked()
    {
        _dialog.optionChosen = optionNum;
        _dialog.gameObject.SetActive(false);
    }

    public override void OnDoubleClicked()
    {

    }


    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
