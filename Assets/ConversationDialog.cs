using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class ConversationDialog : MonoBehaviour
{
    public class Result
    {
        public bool finished = false;
        public int optionChosen = -1;
    }

    public class Info
    {
        public string title;
        public string text;

        public Dictionary<string, TooltipText.Options> linkOptions = new Dictionary<string, TooltipText.Options>();

        public Info AddLink(string linkText, TooltipText.Options info)
        {
            linkOptions[linkText] = info;
            return this;
        }

        public string tooltipText;
        public TooltipText.Options tooltipOptions = null;
        public Sprite primarySprite;
        public Sprite secondarySprite;
        public bool highlightPrimary = true;
        public bool highlightSecondary = false;

        public bool teletype = true;

        public List<string> options = new List<string>();
        public List<string> optionTooltips = new List<string>();

        public int optionChosen = -1;

        public bool Equals(Info other)
        {
            bool result = title == other.title &&
                   text == other.text &&
                   primarySprite == other.primarySprite &&
                   secondarySprite == other.secondarySprite &&
                   highlightPrimary == other.highlightPrimary &&
                   highlightSecondary == other.highlightSecondary;
            if(result == false) {
                return false;
            }

            if(options.Count != other.options.Count) {
                return false;
            }

            for(int i = 0; i != options.Count; ++i) {
                if(options[i] != other.options[i]) {
                    return false;
                }
            }

            return true;
        }
    }

    public Info info = new Info();

    public int optionChosen {
        get {
            return info.optionChosen;
        }
        set {
            info.optionChosen = value;
        }
    }

    public TMPro.TextMeshProUGUI titleText, mainText;

    [SerializeField]
    ConversationDialogOption _dialogOption = null;

    [SerializeField]
    Transform _mainPanel = null;

    [SerializeField]
    Image _avatarImage = null, _secondaryAvatarImage = null;

    [SerializeField]
    Image _promptImage = null;

    List<ConversationDialogOption> _options = new List<ConversationDialogOption>();

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(info.options == null) {
            info.options = new List<string>();
        }

        titleText.text = info.title;
        mainText.text = info.text;

        if(info.linkOptions != null && info.linkOptions.Count > 0) {
            UnitStatusPanel.SetTooltip(mainText, info.linkOptions);
        } else if(string.IsNullOrEmpty(info.tooltipText) == false) {
            if(info.tooltipOptions == null) {
                info.tooltipOptions = new TooltipText.Options();
            }

            info.tooltipOptions.useMousePosition = true;
            UnitStatusPanel.SetTooltip(mainText, info.tooltipText, info.tooltipOptions);
        }

        if(info.primarySprite == null) {
            _avatarImage.gameObject.SetActive(false);
        } else {
            _avatarImage.gameObject.SetActive(true);
            _avatarImage.sprite = info.primarySprite;
            _avatarImage.color = info.highlightPrimary ? Color.white : new Color(0.4f, 0.4f, 0.4f);

            var rt = _avatarImage.GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, info.primarySprite.rect.width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, info.primarySprite.rect.height);
        }

        if(info.secondarySprite == null) {
            _secondaryAvatarImage.gameObject.SetActive(false);
        } else {
            _secondaryAvatarImage.gameObject.SetActive(true);
            _secondaryAvatarImage.sprite = info.secondarySprite;
            _secondaryAvatarImage.color = info.highlightSecondary ? Color.white : new Color(0.4f, 0.4f, 0.4f);

            var rt = _secondaryAvatarImage.GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, info.secondarySprite.rect.width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, info.secondarySprite.rect.height);
        }

        int optionNum = 0;
        float ypos = -64f;
        foreach(string op in info.options) {
            _mainPanel.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, 64f);
            ConversationDialogOption option = Instantiate(_dialogOption, _mainPanel);
            option.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, ypos);
            option.optionNum = optionNum;
            option.text.text = op;

            if(info.optionTooltips != null && optionNum < info.optionTooltips.Count && string.IsNullOrEmpty(info.optionTooltips[optionNum]) == false) {
                UnitStatusPanel.SetTooltip(option.text, info.optionTooltips[optionNum], new TooltipText.Options() {
                    useMousePosition = true,
                });
            }

            option.gameObject.SetActive(info.teletype == false);
            ypos -= 64f;
            ++optionNum;

            _options.Add(option);
        }

        if(info.teletype) {
            _runningTeletype = true;
            StartCoroutine(RunTeletype());
        }
    }

    bool _runningTeletype = false;

    float verticalSpaceAvailable = 156f;

    float _teletypeYoffset = 0f;
    int _teletypeFirstLineShown = 0;
    int _teletypeNumLinesShown = 0;

    List<TMPro.TMP_LineInfo> _lineInfos {
        get {
            List<TMPro.TMP_LineInfo> result = new List<TMPro.TMP_LineInfo>();
            foreach(TMPro.TMP_LineInfo info in mainText.textInfo.lineInfo) {
                if(info.lineHeight <= 0f) {
                    continue;
                }

                result.Add(info);
            }

            return result;
        }
    }

    int _teletypePromptNumChars {
        get {
            int n = _teletypeFirstLineShown + _teletypeNumLinesShown;
            if(n >= _lineInfos.Count) {
                return mainText.textInfo.characterCount;
            } else {
                return _lineInfos[n].firstCharacterIndex;
            }
        }
    }

    bool _teletypeAdvanceLines(int nlines)
    {
        float ydelta = 0f;
        for(int i = 0; i < nlines; ++i) {
            int n = _teletypeFirstLineShown + _teletypeNumLinesShown;
            if(n >= _lineInfos.Count) {
                break;
            }

            ydelta += _lineInfos[n].lineHeight;

            ++_teletypeFirstLineShown;
        }

        var rt = mainText.GetComponent<RectTransform>();
        rt.DOAnchorPosY(rt.anchoredPosition.y + ydelta, 0.2f);

        return ydelta > 0f;
    }

    IEnumerator RunTeletype()
    {
        mainText.maxVisibleCharacters = 0;
        yield return null;
        yield return null;

        float charsPerSecond = 30f;

        float countedHeight = 0f;

        foreach(var line in _lineInfos) {
            Debug.LogFormat("LINE: ({0}): {1} / {2}", line.firstCharacterIndex, line.lineHeight, verticalSpaceAvailable);
            if(_teletypeNumLinesShown > 0 && line.lineHeight + countedHeight > verticalSpaceAvailable) {
                Debug.LogFormat("BREAK LINE: {0} > {1}", line.lineHeight + countedHeight, verticalSpaceAvailable);
                break;
            }

            countedHeight += line.lineHeight;

            ++_teletypeNumLinesShown;
        }

        TooltipText tooltipText = mainText.GetComponent<TooltipText>();

        int characterCount = _lineInfos.Count == 0 ? 0 : _lineInfos[_lineInfos.Count-1].lastCharacterIndex+1;

        float charTime = 0f;

        int nchars = 0;
        while(nchars < characterCount) {
            while(nchars < _teletypePromptNumChars) {
                nchars = (int)(charsPerSecond*charTime);
                mainText.maxVisibleCharacters = nchars;
                if(tooltipText != null) {
                    mainText.ForceMeshUpdate();
                    tooltipText.SetLinkColors();
                }

                yield return null;

                if(Input.anyKeyDown) {
                    mainText.maxVisibleCharacters = nchars = _teletypePromptNumChars;
                    if(tooltipText != null) {
                        mainText.ForceMeshUpdate();
                        tooltipText.SetLinkColors();
                    }

                    charTime = nchars/charsPerSecond;
                    yield return null;
                    break;
                }

                charTime += Time.deltaTime;
            }

            if(nchars < characterCount) {
                float promptTime = Time.time;

                while(true) {
                    float t = Time.time - promptTime;
                    t -= Mathf.Floor(t);
                    _promptImage.gameObject.SetActive(t < 0.5f);
                    yield return null;

                    if(Input.anyKeyDown) {
                        break;
                    }
                }

                _promptImage.gameObject.SetActive(false);

                bool advanced = _teletypeAdvanceLines(3);
                if(advanced == false) {
                    break;
                }
            } else {
                break;
            }
        }

        mainText.maxVisibleCharacters = mainText.textInfo.characterCount;
        if(tooltipText != null) {
            mainText.ForceMeshUpdate();
            tooltipText.SetLinkColors();
        }

        foreach(var option in _options) {
            option.gameObject.SetActive(true);
        }

        yield return null;
        yield return null;

        if(tooltipText != null) {
            mainText.ForceMeshUpdate();
            tooltipText.SetLinkColors();
        }

        _runningTeletype = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(info.options == null || info.options.Count == 0) {
            if(Input.anyKeyDown && _runningTeletype == false) {
                GameController.instance.CloseConversationDialog();
            }
        }
    }
}
