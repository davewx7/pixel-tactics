using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GamePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    static public GamePanel mouseoverPanel = null;

    public void OnDisable()
    {
        if(mouseoverPanel == this) {
            mouseoverPanel = null;
        }
    }

    public void OnDestroy()
    {
        if(mouseoverPanel == this) {
            mouseoverPanel = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseoverPanel = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(mouseoverPanel == this) {
            mouseoverPanel = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }
}
