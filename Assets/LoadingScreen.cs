using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class LoadingScreen : MonoBehaviour
{
    [System.NonSerialized]
    public Team playerTeam = null;

    public string[] tips;

    [SerializeField]
    TMPro.TextMeshProUGUI _storyTextProto = null;

    List<TMPro.TextMeshProUGUI> _storyText = new List<TMPro.TextMeshProUGUI>();

    [SerializeField]
    Button _beginButton = null;

    [SerializeField]
    Slider _progressBar = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    public bool complete = false;
    public bool waitingForBeginButton = false;

    bool _beginButtonPressed = false;

    public void UpdateProgress(string text, float r)
    {
        for(int i = 0; i != _storyText.Count; ++i) {
            float needed = i == 0 ? 0f : (((float)i) / (float)(_storyText.Count-1));
            _storyText[i].gameObject.SetActive(r >= needed);
        }

        _progressBar.value = r;
        _text.text = text;
    }

    public void BeginButtonClicked()
    {
        waitingForBeginButton = false;
        _beginButtonPressed = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(playerTeam != null && playerTeam.playerStoryText != null && playerTeam.playerStoryText.Length > 0) {
            float top = 220f;
            float bot = -220f;
            for(int i = 0; i != playerTeam.playerStoryText.Length; ++i) {
                var text = Instantiate(_storyTextProto, transform);
                text.text = playerTeam.playerStoryText[i];
                if(playerTeam.playerStoryText.Length > 1) {
                    float segmentLength = (top - bot) / (playerTeam.playerStoryText.Length-1);
                    text.transform.localPosition += new Vector3(0f,1f,0f) * (top - i*segmentLength);
                }

                _storyText.Add(text);
            }
        } else {
            var text = Instantiate(_storyTextProto, transform);
            text.text = tips[new ConsistentRandom().Next()%tips.Length];
            text.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool showButton = complete && waitingForBeginButton;
        _beginButton.gameObject.SetActive(showButton);
        _text.gameObject.SetActive(!showButton && !_beginButtonPressed);
        _progressBar.gameObject.SetActive(!showButton && !_beginButtonPressed);
    }
}
