using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ContextMenuEntry : MonoBehaviour
{
    public Button button;
    public TMPro.TextMeshProUGUI text;

    public void Init(GameContextMenu.Entry entry)
    {
        if(entry.disabled) {
            text.color = Color.gray;
        }
        text.text = entry.text;

        if(string.IsNullOrEmpty(entry.tooltip) == false) {
            UnitStatusPanel.SetTooltip(text, entry.tooltip);
        }

        if(entry.disabled == false) {
            button.onClick.AddListener(entry.action);
        }
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
