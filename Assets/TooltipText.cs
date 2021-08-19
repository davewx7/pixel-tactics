using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class TooltipText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public class Options
    {
        public string text;
        public bool useMousePosition = false;
        public float delay = -1f;
        public Sprite icon = null;
        public Vector2 iconSize = Vector2.zero;
        public Material iconMaterial = null;
        public Color linkColor = new Color(1f, 0f, 1f);
        public Color linkNormalColor = new Color(0f, 0f, 0f, 0f);
    }

    [HideInInspector]
    public Dictionary<string, Options> linkOptions = null;

    string _linkStr = null;

    string linkStr {
        get { return _linkStr; }
        set {
            if(_linkStr != value) {
                _linkStr = value;

                if(linkOptions != null) {
                    Options options = null;
                    if(_linkStr != null && linkOptions.TryGetValue(_linkStr, out options)) {
                        ShowTooltip(options.text, options);
                    } else {
                        HideTooltip();
                    }
                }
            }
        }
    }

    public Options options = new Options();

    public string tooltipText = null;
    float _delaycount = -1f;

    bool _mouseover = false;

    void ShowTooltip(string text, Options tipOptions)
    {
        if(string.IsNullOrEmpty(text) == false) {
            if(tipOptions.useMousePosition) {
                GameCanvas.instance.ShowTooltip(text, tipOptions, transform);
            } else {
                GameCanvas.instance.ShowTooltip(text, tipOptions, GetComponent<RectTransform>());
            }
        }
    }

    void HideTooltip()
    {
        if(options.useMousePosition) {
            GameCanvas.instance.ClearTooltip(transform);
        } else {
            GameCanvas.instance.ClearTooltip(GetComponent<RectTransform>());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _mouseover = true;

        if(options.delay > 0f) {
            _delaycount = 0f;
        } else {
            ShowTooltip(tooltipText, options);
            _delaycount = -1f;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _mouseover = false;
        HideTooltip();
        _delaycount = -1f;
        linkStr = null;
        nlink = -1;
    }

    // Start is called before the first frame update
    void Start()
    {
        SetLinkColors();
    }

    public void SetLinkColors()
    {
        TMPro.TextMeshProUGUI tmpro = GetComponent<TMPro.TextMeshProUGUI>();
        if(tmpro == null) {
            return;
        }

        if(linkOptions != null && tmpro != null) {
            foreach(var p in linkOptions) {
                if(p.Value.linkNormalColor.a > 0) {
                    foreach(var linkInfo in tmpro.textInfo.linkInfo) {
                        if(linkInfo.GetLinkID() == p.Key) {
                            SetTextColor(tmpro, linkInfo.linkTextfirstCharacterIndex, linkInfo.linkTextfirstCharacterIndex+linkInfo.linkTextLength, p.Value.linkNormalColor);
                        }
                    }
                }
            }

            SaveTextColor();
        }
    }

    Dictionary<int, Color32[]> _textColors = new Dictionary<int, Color32[]>();
    void SaveTextColor()
    {
        TMPro.TextMeshProUGUI tmpro = GetComponent<TMPro.TextMeshProUGUI>();

        if(tmpro == null) {
            return;
        }

        _textColors.Clear();
        for(int i = 0; i != tmpro.textInfo.characterCount; ++i) {
            int materialIndex = tmpro.textInfo.characterInfo[i].materialReferenceIndex;
            if(_textColors.ContainsKey(materialIndex) == false) {
                Color32[] storedColors = tmpro.textInfo.meshInfo[materialIndex].colors32;
                Color32[] colors = new Color32[storedColors.Length];
                for(int j = 0; j != storedColors.Length; ++j) {
                    colors[j] = storedColors[j];
                }

                _textColors[materialIndex] = colors;
            }
        }
    }

    void RestoreTextColor()
    {
        if(_textColors.Count == 0) {
            return;
        }

        TMPro.TextMeshProUGUI tmpro = GetComponent<TMPro.TextMeshProUGUI>();

        foreach(var p in _textColors) {
            Color32[] storedColors = tmpro.textInfo.meshInfo[p.Key].colors32;
            Color32[] colors = p.Value;
            if(storedColors.Length == colors.Length) {
                for(int i = 0; i != storedColors.Length; ++i) {
                    storedColors[i] = colors[i];
                }
            }
        }

        tmpro.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
    }

    void SetTextColor(TMPro.TMP_Text text, int beginChar, int endChar, Color color)
    {
        TMPro.TMP_TextInfo textInfo = text.textInfo;

        for(int i = beginChar; i < endChar; ++i) {
            if(textInfo.characterInfo[i].isVisible == false) {
                continue;
            }

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Color32[] newVertexColors = textInfo.meshInfo[materialIndex].colors32;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            for(int j = 0; j != 4; ++j) {
                newVertexColors[vertexIndex+j] = color;
            }
        }

        text.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
    }

    int _nlink = -1;
    int nlink {
        get { return _nlink; }
        set {
            if(_nlink != value) {
                RestoreTextColor();
                SaveTextColor();

                _nlink = value;

                TMPro.TextMeshProUGUI tmpro = GetComponent<TMPro.TextMeshProUGUI>();
                if(tmpro != null && _nlink >= 0) {
                    var linkInfo = tmpro.textInfo.linkInfo[_nlink];
                    linkStr = linkInfo.GetLinkID();

                    Options options = null;
                    if(_linkStr != null && linkOptions.TryGetValue(_linkStr, out options)) {
                        SetTextColor(tmpro, linkInfo.linkTextfirstCharacterIndex, linkInfo.linkTextfirstCharacterIndex+linkInfo.linkTextLength, options.linkColor);
                    }

                } else {
                    linkStr = null;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(_mouseover && _delaycount >= 0f) {
            _delaycount += Time.deltaTime;
            if(_delaycount >= options.delay) {
                _delaycount = -1f;
                ShowTooltip(tooltipText, options);
            }
        }

        if(_mouseover && linkOptions != null && linkOptions.Count > 0) {
            TMPro.TextMeshProUGUI tmpro = GetComponent<TMPro.TextMeshProUGUI>();
            if(tmpro != null) {
                nlink = TMPro.TMP_TextUtilities.FindIntersectingLink(tmpro, Input.mousePosition, null);
            }
        }
    }
}
