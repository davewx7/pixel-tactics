using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogEntry
{
    public enum LogType { ChatMessage, Emote };

    public LogType logType = LogType.ChatMessage;

    public string guid;
    public string nick;
    public string message;
    public long timestamp;
}

public class GameLogPanel : MonoBehaviour
{

    Dictionary<string, GameLogEntry> _entries = new Dictionary<string, GameLogEntry>();
    List<GameLogEntry> _entriesOrdered = new List<GameLogEntry>();

    public HashSet<string> _messagesPending = new HashSet<string>();

    [SerializeField]
    TMPro.TMP_Text _text = null;

    public TMPro.TMP_InputField inputField = null;

    public string path {
        get {
            return string.Format("/games/{0}/gamelog", GameController.instance.gameState.guid);
        }
    }

    public bool isFocused {
        get {
            return inputField.isFocused;
        }
    }

    public void SendChatMessage(GameLogEntry entry)
    {
        entry.guid = System.Guid.NewGuid().ToString();
        entry.timestamp = Glowwave.Json.firebaseTimestampLongPlaceholder;

        string payload = Glowwave.Json.ToJson(entry);

        CloudInterface.instance.PushData(path, payload);

        _entriesOrdered.Add(entry);
        _messagesPending.Add(entry.guid);
        RenderText();
    }

    public void SendChat(string msg)
    {
        if(string.IsNullOrEmpty(msg)) {
            return;
        }

        inputField.text = "";

        SendChatMessage(new GameLogEntry() {
            logType = GameLogEntry.LogType.ChatMessage,
            nick = GameConfig.instance.username,
            message = msg,
        });
    }

    private void Awake()
    {
        inputField.onSubmit.AddListener(SendChat);
    }

    private void Start()
    {
    }

    DataStore.Monitor _monitor = null;

    public void UpdateLog()
    {
        if(_monitor == null && DataStore.instance != null) {
            _monitor = DataStore.instance.MonitorData(path, ReceiveChat);
        }
    }

    public void ReceiveChat(object obj)
    {
        if(obj == null) {
            return;
        }

        Dictionary<string, object> messages = obj as Dictionary<string, object>;
        Debug.Log("Receive chat: " + messages.Count);

        foreach(var p in messages) {
            if(_entries.ContainsKey(p.Key)) {
                //already have this chat message.
                continue;
            }

            if(p.Value == null) {
                continue;
            }

            GameLogEntry entry = Glowwave.Json.FromObject<GameLogEntry>(p.Value, null);
            _entries[p.Key] = entry;

            bool foundExisting = false;
            for(int i = 0; i != _entriesOrdered.Count; ++i) {
                if(_entriesOrdered[i].guid == entry.guid) {
                    _entriesOrdered[i] = entry;
                    _messagesPending.Remove(entry.guid);
                    foundExisting = true;
                    break;
                }
            }

            if(foundExisting == false) {
                _entriesOrdered.Add(entry);
            }
        }

        _entriesOrdered.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

        RenderText();
    }

    public void RenderText()
    {
        string text = "";

        foreach(var entry in _entriesOrdered) {
            bool pending = _messagesPending.Contains(entry.guid);
            string textColor = pending ? "#777777" : "#ffffff";

            switch(entry.logType) {
                case GameLogEntry.LogType.ChatMessage:
                    text += string.Format("<color=#ffbbbb><b>{0}:</b></color> <color={2}>{1}</color>\n", entry.nick, entry.message, textColor);
                    break;
                case GameLogEntry.LogType.Emote:
                    text += string.Format("<color=#bbffbb>* <b>{0}</b> {1}</color>\n", entry.nick, entry.message);
                    break;
            }
        }

        _text.text = text;

        gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
}
