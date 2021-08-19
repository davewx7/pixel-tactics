using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrowseGamesScreen : MonoBehaviour
{
    [SerializeField]
    TMPro.TMP_InputField _nameInput = null;

    [SerializeField]
    RectTransform _content = null;

    [SerializeField]
    BrowseGamePanel _panelProto = null;

    List<BrowseGamePanel> _panels = new List<BrowseGamePanel>();


    void ClearPanels()
    {
        foreach(var panel in _panels) {
            GameObject.Destroy(panel.gameObject);
        }

        _panels.Clear();
    }

    public string username {
        get {
            return _nameInput.text;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _nameInput.text = GameConfig.instance.username;

        ClearPanels();

        Debug.LogFormat("Open Sessions:: {0}", CloudInterface.instance.openSessions.Count);

        int index = 0;
        foreach(var session in CloudInterface.instance.openSessions.Values) {
            BrowseGamePanel panel = Instantiate(_panelProto, _content);
            panel.gameSession = session;
            panel.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -index*120f);
            panel.gameObject.SetActive(true);
            _panels.Add(panel);

            Debug.Log("Add panel!");

            ++index;
        }

        _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 120f*CloudInterface.instance.openSessions.Count);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
