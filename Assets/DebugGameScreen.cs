using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugGameScreen : MonoBehaviour
{
    [SerializeField]
    GameHarness _harness = null;

    public string debugGameGuid = "";

    [SerializeField]
    TMPro.TextMeshProUGUI _statusText = null;

    [SerializeField]
    DebugRoundEntry _debugRoundEntryProto = null;

    List<DebugRoundEntry> _debugRoundEntries = new List<DebugRoundEntry>();

    [SerializeField]
    RectTransform _roundScrollContent = null;

    bool _loggedIn = false;

    Glowwave.Json.GameSerializer _serializer = new Glowwave.Json.GameSerializer();

    Dictionary<string, object> _rounds = null;

    public void RoundClicked(int nround)
    {
        string k = string.Format("round{0}", nround);
        PlayerPrefs.SetString("save", BestHTTP.JSON.Json.Encode(_rounds[k]));
        _harness.LoadGame();
    }

    public void ReceiveFailed(string msg)
    {
        _statusText.text = "Error: " + msg;
    }

    public void ReceiveGame(string data, string etag)
    {
        foreach(var entry in _debugRoundEntries) {
            GameObject.Destroy(entry.gameObject);
        }

        _debugRoundEntries.Clear();

        object obj = BestHTTP.JSON.Json.Decode(data);
        _rounds = obj as Dictionary<string, object>;

        _statusText.text = "Have game data: " + data.Length + " / " + (_rounds != null ? _rounds.Keys.Count : -1);

        int nindex = 0;
        for(int nround = 0; nround < 100; ++nround) {
            string k = string.Format("round{0}", nround);
            if(_rounds.ContainsKey(k)) {
                DebugRoundEntry entry = Instantiate(_debugRoundEntryProto, _roundScrollContent);
                entry.nround = nround;
                entry.descriptionText.text = string.Format("Round {0}", nround+1);
                entry.transform.localPosition += new Vector3(0f, -60f, 0f) * nindex;
                entry.gameObject.SetActive(true);
                _debugRoundEntries.Add(entry);

                ++nindex;
            }
        }

        _roundScrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _debugRoundEntries.Count*60);
    }

    public void OnLogIn()
    {
        _loggedIn = true;
        DataStore.instance.GetData(string.Format("/games/{0}/snapshots", debugGameGuid), ReceiveGame, ReceiveFailed);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_loggedIn == false) {
            _statusText.text = DataStore.instance == null ? "Loading..." : "Querying game data...";
        }

        if(DataStore.instance != null && _loggedIn == false) {
            OnLogIn();
        }
    }
}
