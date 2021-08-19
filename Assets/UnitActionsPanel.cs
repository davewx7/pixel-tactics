using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitActionsPanel : MonoBehaviour
{
    [SerializeField]
    List<RectTransform> _buttons = new List<RectTransform>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float ypos = 0f;
        foreach(RectTransform button in _buttons) {
            if(button.gameObject.activeSelf) {
                button.anchoredPosition = new Vector2(0f, ypos);
                ypos += 64f;
            }
        }
    }
}
