using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudInterface : MonoBehaviour
{
    public struct SessionInfo
    {
        public long timestamp;
    }

    bool _sentSession = false;
    string _sessionId = "";

    public static CloudInterface instance = null;

    private void Start()
    {
        instance = this;
    }

    long _serverTimestamp = -1L;
    float _serverTimestampTime = -1f;

    public long currentServerTimestamp {
        get {
            return _serverTimestamp + (long)((Time.unscaledTime - _serverTimestampTime)*1000f);
        }
    }

    void SessionPushSucceeded(string id)
    {
        _sessionId = id;
        string path = string.Format("/Players/{0}/Sessions/{1}", GameConfig.instance.PlayerGuid, id);
        Debug.Log("Got session id: " + id + " / " + path);
        DataStore.instance.GetData(path, (string msg, string etag) => {
            SessionInfo sessionInfo = Glowwave.Json.FromJson<SessionInfo>(msg);
            _serverTimestamp = sessionInfo.timestamp;
            _serverTimestampTime = Time.unscaledTime;

            if(_openSessionsMonitor == null) {
                _openSessionsMonitor = DataStore.instance.MonitorData("/OpenSessions", UpdateOpenSessions);
            }
        });
    }

    private void Update()
    {
        if(_sentSession == false && DataStore.instance != null) {
            _sentSession = true;

            string path = string.Format("/Players/{0}/Sessions", GameConfig.instance.PlayerGuid);

            SessionInfo info = new SessionInfo() {
                timestamp = Glowwave.Json.firebaseTimestampLongPlaceholder,
            };

            PushData(path, Glowwave.Json.ToJson(info), this.SessionPushSucceeded);
        }

        PingGameStatus();
        PruneOpenSessions();
    }

    DataStore.Monitor _openSessionsMonitor = null;

    [System.Serializable]
    public class OpenSession
    {
        public string playerGuid;
        public string gameId;
        public string username;
        public long timestamp;

        public UnitType unitType = null;
        public string rulerName;
        public int nround;

        public int gameSeed;
    }

    float _timeSinceLastPing = -1f;
    string _gameId = null;

    public void BeginPlayingGame(string gameId)
    {
        _gameId = gameId;
        _timeSinceLastPing = -1f;
    }

    public void PingGameStatus()
    {
        if(string.IsNullOrEmpty(_gameId) || DataStore.instance == null || GameConfig.instance.allowObservers == false) {
            return;
        }

        _timeSinceLastPing += Time.deltaTime;
        if(_timeSinceLastPing >= 0f && _timeSinceLastPing <= 30f) {
            return;
        }

        _timeSinceLastPing = 0f;

        string path = string.Format("/OpenSessions/{0}", GameConfig.instance.PlayerGuid);
        OpenSession info = new OpenSession() {
            playerGuid = GameConfig.instance.PlayerGuid,
            gameId = _gameId,
            gameSeed = GameController.instance.gameState.seed,
            username = GameConfig.instance.username,
            timestamp = Glowwave.Json.firebaseTimestampLongPlaceholder,

            unitType = GameController.instance?.playerTeamInfo?.GetRuler()?.unitInfo?.unitType,
            rulerName = GameController.instance?.playerTeamInfo?.GetRuler()?.unitInfo?.characterName,
            nround = GameController.instance.gameState.nround,
        };

        Debug.LogFormat("Write open session: {0}", path);
        PutData(path, Glowwave.Json.ToJson(info), "", PutDataFailed, () => {
        });
    }

    void PutDataFailed(string msg)
    {

    }

    public bool connected {
        get {
            return DataStore.instance != null;
        }
    }

    public Dictionary<string, OpenSession> openSessions {
        get {
            if(connected == false) {
                return null;
            }

            return _openSessions;
        }
    }

    Dictionary<string, OpenSession> _openSessions = new Dictionary<string, OpenSession>();


    void UpdateOpenSessions(object obj)
    {
        Dictionary<string, OpenSession> sessions = null;
        sessions = (Dictionary<string, OpenSession>)Glowwave.Json.DecodeJson(obj, _openSessions.GetType(), null);

        _openSessions.Clear();

        if(sessions != null) {
            foreach(var p in sessions) {
                if(p.Value != null && p.Value.playerGuid != GameConfig.instance.PlayerGuid) {
                    _openSessions[p.Key] = p.Value;
                }
            }

            PruneOpenSessions();
        }
    }

    void PruneOpenSessions()
    {
        if(_openSessions == null || _openSessions.Count == 0) {
            return;
        }

        List<string> prune = new List<string>();
        foreach(var p in _openSessions) {
            if(p.Value.timestamp+1000L*120L < currentServerTimestamp) {
                prune.Add(p.Key);
            }
        }

        foreach(string item in prune) {
            _openSessions.Remove(item);
        }
    }

    public void StopPlayingGame()
    {
        if(string.IsNullOrEmpty(_gameId) == false) {
            string path = string.Format("/OpenSessions/{0}", GameConfig.instance.PlayerGuid);
            DeleteData(path);
        }

        _gameId = null;
    }

    public void PushData(string path, string data, DataStore.PushSucceedHandler handler = null)
    {
        if(DataStore.instance == null)
            return;

        DataStore.instance.PushData(path, data, handler);
    }

    public void PutData(string path, string data, string etag = "", DataStore.TransactionFailedHandler transactionFailed = null, DataStore.PutSucceedHandler succeedHandler = null)
    {
        if(DataStore.instance == null)
            return;

        DataStore.instance.PutData(path, data, etag, transactionFailed, succeedHandler);
    }

    public void DeleteData(string path)
    {
        if(DataStore.instance == null)
            return;

        DataStore.instance.DeleteData(path);
    }
}
