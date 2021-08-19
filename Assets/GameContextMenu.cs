using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameContextMenu : MonoBehaviour
{
    public class Entry
    {
        public string text;
        public string tooltip;
        public UnityAction action;
        public bool disabled = false;
    }

    [SerializeField]
    ContextMenuEntry _elementPrefab = null;

    List<ContextMenuEntry> _elements = new List<ContextMenuEntry>();

    public void Clear()
    {
        foreach(ContextMenuEntry b in _elements) {
            GameObject.Destroy(b.gameObject);
        }

        _elements.Clear();
    }

    public void Show(List<Entry> entries)
    {
        Clear();

        int nindex = 0;
        foreach(Entry entry in entries) {
            ContextMenuEntry b = Instantiate(_elementPrefab, transform);
            b.Init(entry);
            b.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -36f*nindex);
            b.gameObject.SetActive(true);

            _elements.Add(b);

            ++nindex;
        }

        float scaleFactor = 768f/Screen.height;
        float screenWidth = Screen.width*scaleFactor;
        float screenHeight = Screen.height*scaleFactor;

        RectTransform rt = GetComponent<RectTransform>();

        float ypos = -screenHeight + Input.mousePosition.y*scaleFactor + 36f*nindex;

        Debug.LogFormat("SPAWN NEAR ypos = {0}", ypos);

        if(screenWidth - Input.mousePosition.x*scaleFactor < rt.rect.width) {
            rt.anchoredPosition = new Vector2(screenWidth - rt.rect.width, ypos);
        } else {
            rt.anchoredPosition = new Vector2(Input.mousePosition.x, ypos);
        }

        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        GameConfig.modalDialog++;
    }

    private void OnDisable()
    {
        GameConfig.modalDialog--;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonUp(0)) {
            gameObject.SetActive(false);
        }
    }
}
