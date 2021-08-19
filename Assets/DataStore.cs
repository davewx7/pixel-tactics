using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class DataStore : MonoBehaviour
{
    public static int dataErrors = 0;
    public static bool authRevoked = false;

    public abstract class Monitor
    {
        public bool isConnected = false;

        public abstract void Update();
        public abstract void RefreshConnection();
        public abstract void Close();
    }

    public enum MonitorType { Data, Updates };

    public class MonitorRemote : Monitor
    {
        MonitorType _monitorType = MonitorType.Data;

        BestHTTP.ServerSentEvents.EventSource _eventSource;
        string _path;
        MonitorDataCallback _callback;
        DataStore _dataStore;
        string _url;

        bool _closed = false;
        string _authstringUsed = "";

        public MonitorRemote(MonitorType monitorType, string path, MonitorDataCallback callback, DataStore dataStore, string url)
        {
            _monitorType = monitorType;
            _path = path;
            _callback = callback;
            _dataStore = dataStore;
            _url = url;

            RefreshConnection();
        }

        void OnClosed(BestHTTP.ServerSentEvents.EventSource source)
        {
            if(source != _eventSource) {
                return;
            }
            Debug.Log("MONITOR CLOSED: " + _path);
            _closed = true;
            isConnected = false;
        }

        void OnAuthRevoked(BestHTTP.ServerSentEvents.EventSource source, BestHTTP.ServerSentEvents.Message message)
        {
            if(source != _eventSource) {
                return;
            }

            if(_authstringUsed == _dataStore.authstring) {
                authRevoked = true;
            }
        }

        void OnError(BestHTTP.ServerSentEvents.EventSource source, string error)
        {
            if(source != _eventSource) {
                return;
            }
            Debug.Log("Monitor data error: " + error);
            _closed = true;

            DataStore.dataErrors++;
            isConnected = false;
        }

        void OnOpen(BestHTTP.ServerSentEvents.EventSource source)
        {
            if(source != _eventSource) {
                return;
            }

            isConnected = true;
        }

        public override void Update()
        {
            if(_closed) {
                RefreshConnection();
            }
        }

        public override void RefreshConnection()
        {
            if(_eventSource != null) {
                var src = _eventSource;
                _eventSource = null;
                src.Close();
            }
            isConnected = false;
            _closed = false;

            string url = _url + _dataStore.authstring;
            _authstringUsed = _dataStore.authstring;

            Debug.Log("MONITOR REFRESH CONNECTION: " + url);

            _eventSource = new BestHTTP.ServerSentEvents.EventSource(new Uri(url));
            _eventSource.OnClosed += this.OnClosed;
            _eventSource.OnError += this.OnError;
            _eventSource.OnOpen += this.OnOpen;
            _eventSource.On("auth_revoked", this.OnAuthRevoked);

            if(_monitorType == MonitorType.Data) {
                _dataStore.DoMonitorData(_eventSource, _path, _callback);
            } else {
                _dataStore.DoMonitorUpdates(_eventSource, _path, _callback);
            }
        }


        public override void Close()
        {
            _eventSource.Close();
            _dataStore._dataMonitors.Remove(this);
        }
    }

    List<Monitor> _dataMonitors = new List<Monitor>();


    [SerializeField]
    string _server = "rpgtactics-51240.firebaseio.com";

    public string authstring {
        get { return "?auth=" + _loginController.token; }
    }

    [SerializeField]
    LoginController _loginController = null;

    public delegate void Callback(string msg, string etag);
    public delegate void TransactionFailedHandler(string msg);
    public delegate void PushSucceedHandler(string id);
    public delegate void PutSucceedHandler();

    public void GetData(string path, Callback cb, TransactionFailedHandler onFail = null)
    {
        StartCoroutine(GetDataCo(path, cb, onFail));
    }

    public IEnumerator GetDataCo(string path, Callback cb, TransactionFailedHandler onFail)
    {
        string url = "https://" + _server + path + ".json" + authstring;

        var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-Firebase-ETag", "true");

        yield return request.SendWebRequest();

        if(request.isNetworkError || request.isHttpError) {
            Debug.Log("ERROR IN HTTP");
            onFail?.Invoke("HTTP error");
        } else {
            cb(request.downloadHandler.text, request.GetResponseHeader("ETag"));
        }
    }

    public void PutData(string path, string data, string etag = "", TransactionFailedHandler transactionFailed = null, PutSucceedHandler succeedHandler = null)
    {
        StartCoroutine(WriteDataCo(path, data, etag, transactionFailed, succeedHandler));
    }

    public void PatchData(string path, string data, string etag = "", TransactionFailedHandler transactionFailed = null, PutSucceedHandler succeedHandler = null)
    {
        StartCoroutine(WriteDataCo(path, data, etag, transactionFailed, succeedHandler, "PATCH"));
    }


    IEnumerator WriteDataCo(string path, string payload, string etag, TransactionFailedHandler transactionFailed, PutSucceedHandler succeedHandler, string method = "PUT")
    {
        string url = "https://" + _server + path + ".json" + authstring;

        var request = new UnityWebRequest(url);
        request.method = method;
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.uploadHandler.contentType = "application/json";
        request.SetRequestHeader("Content-Type", "application/json");
        if(etag != "") {
            request.SetRequestHeader("if-match", etag);
        }
        request.chunkedTransfer = false;

        yield return request.SendWebRequest();

        if(request.responseCode == 412) {
            Debug.Log("Net write failed 412: " + request.downloadHandler.text);

            transactionFailed?.Invoke(request.downloadHandler.text);
        } else if(request.responseCode == 200) {
            //Debug.Log("Write succeeded");
            succeedHandler?.Invoke();
        } else {
            Debug.Log("Error in write, code " + request.responseCode + ": " + request.downloadHandler.text);
            transactionFailed?.Invoke(request.downloadHandler.text);
        }
    }

    public void DeleteData(string path)
    {
        StartCoroutine(DeleteDataCo(path));
    }

    IEnumerator DeleteDataCo(string path)
    {
        string url = "https://" + _server + path + ".json" + authstring;

        var request = new UnityWebRequest(url);
        request.method = "DELETE";
        request.downloadHandler = new DownloadHandlerBuffer();

        request.chunkedTransfer = false;

        yield return request.SendWebRequest();
    }


    public void PushData(string path, string data, PushSucceedHandler handler = null)
    {
        StartCoroutine(PushDataCo(path, data, handler));
    }

    [Serializable]
    struct PushResponse
    {
        public string name;
    }

    IEnumerator PushDataCo(string path, string payload, PushSucceedHandler handler)
    {
        bool pushSuccess = false;

        float waitTime = 0f;

        while(pushSuccess == false) {
            if(waitTime > 0f) {
                if(waitTime > 20f) {
                    if(handler != null) {
                        Debug.Log("Data failed to push. Aborting.");
                        handler(null);
                        yield break;
                    }
                }
                yield return new WaitForSecondsRealtime(waitTime);
            }

            waitTime = (waitTime + 0.5f)*2f;

            string url = "https://" + _server + path + ".json" + authstring;

            var request = new UnityWebRequest(url);
            request.method = "POST";
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();

            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");
            request.chunkedTransfer = false;

            yield return request.SendWebRequest();
            if(request.isNetworkError || request.isHttpError) {
                Debug.Log("Error in data push. Will re-try");
            } else {
                pushSuccess = true;
                PushResponse response = Glowwave.Json.FromJson<PushResponse>(request.downloadHandler.text);
                Debug.Log("Push succeeded: " + path + " / " + response.name + " / " + request.downloadHandler.text);
                if(handler != null) {
                    handler(response.name);
                }
            }
        }
    }

    public delegate void MonitorDataCallback(object new_data);

    public void RefreshConnections()
    {
        foreach(var monitor in _dataMonitors) {
            monitor.RefreshConnection();
        }
    }

    public Monitor MonitorData(string path, MonitorDataCallback callback)
    {
        string url = "https://" + _server + path + ".json";

        Debug.Log("START MONITORING: " + path);

        Monitor result = new MonitorRemote(MonitorType.Data, path, callback, this, url);

        _dataMonitors.Add(result);

        return result;
    }

    public Monitor MonitorUpdates(string path, MonitorDataCallback callback)
    {
        string url = "https://" + _server + path + ".json";

        Debug.Log("START MONITORING: " + path);

        Monitor result = new MonitorRemote(MonitorType.Updates, path, callback, this, url);

        _dataMonitors.Add(result);

        return result;
    }


    void DoMonitorData(BestHTTP.ServerSentEvents.EventSource eventSource, string path, MonitorDataCallback callback)
    {
        object node = null;

        BestHTTP.ServerSentEvents.OnEventDelegate onMsgFn = (BestHTTP.ServerSentEvents.EventSource source, BestHTTP.ServerSentEvents.Message message) => {
            Debug.Log("GOT STREAM MESSAGE FOR " + path + ": " + message.Event + ": " + message.Data);
            if(message.Event != "put" && message.Event != "patch") {
                return;
            }

            object obj = BestHTTP.JSON.Json.Decode(message.Data);
            if(obj == null) {
                Debug.Log("JSON Parse error in stream");
                return;
            }
            Dictionary<string, object> dict = obj as Dictionary<string, object>;
            if(dict == null) {
                Debug.Log("Could not convert JSON to dict in stream");
                return;
            }

            string patchPath = dict["path"] as string;
            object patchData = dict["data"];

            Utils.PatchJson(patchPath, patchData, ref node, message.Event == "put");
            callback(node);
        };

        eventSource.On("put", onMsgFn);
        eventSource.On("patch", onMsgFn);

        eventSource.Open();
    }


    void DoMonitorUpdates(BestHTTP.ServerSentEvents.EventSource eventSource, string path, MonitorDataCallback callback)
    {
        object node = null;

        BestHTTP.ServerSentEvents.OnEventDelegate onMsgFn = (BestHTTP.ServerSentEvents.EventSource source, BestHTTP.ServerSentEvents.Message message) => {

            //We get data in one of these formats:
            // /updates: put: { "path":"/","data":null}
            //
            // /updates: put: { "path":"/-LfDhcb5qVCKFLOHpvg1","data":456}
            //
            // /updates: put: { "path":"/","data":{ "-LfDhcP2dHYDFMLsuYas":456,"-LfDhcP5ycHlsxFG0-e3":456,"-LfDhcPGUVPqokbKb4oq":456,"-LfDhcP_WQEVRMfqTJPC":456,"-LfDhcPn0C6Q3ZWJSry9":456,"-LfDhcPvKBCrUxBnjuDS":456,"-LfDhcPyin53D3DlUdbS":456,"-LfDhcQ85AIkcX52fdRV":456,"-LfDhcQLduAD35URjMZe":456,"-LfDhcQLduAD35URjMZf":456,"-LfDhcQp5tZ_4dRy5mlz":456,"-LfDhcQuO0qU2vrJF5HA":456,"-LfDhcR0ErA41sCvNenE":456,"-LfDhcR0ErA41sCvNenF":456,"-LfDhcRAKho7ATMyEqUC":456,"-LfDhcRVPw_iQ_6u_JrR":456,"-LfDhcRicOGvGcasIZv8":456,"-LfDhcRn454gknViF2bB":456,"-LfDhcRn454gknViF2bC":456,"-LfDhcRpIH25qDLWbZU1":456,"-LfDhcS9zR7V1Xf6rRt-":456,"-LfDhcSCPM7o0ePx4JAI":456,"-LfDhcSFba_vOSiAPQ2b":456,"-LfDhcSQk-uKJtaxrTAv":456,"-LfDhcSdTGT1tXeI2awu":456,"-LfDhcSr75vqw-HjydfK":456,"-LfDhcTIYAEK2SSFmpvF":456,"-LfDhcTiCfN4lwd8OZs5":456,"-LfDhcTvmgpEU1t4NFWA":456,"-LfDhcU7bGBupHPRRztI":456,"-LfDhcUZQZAEVr3xX-gy":456,"-LfDhcUm_WSiPLK1TDmr":456,"-LfDhcVH4dSSoOWYfmOF":456,"-LfDhcVSWXW3vV7kRTqI":456,"-LfDhcVpcTRIJTtpWP53":456,"-LfDhcW4Gmcw6D1QhLa8":456,"-LfDhcW_VE0Dc8UMA7_Q":456,"-LfDhcWsxAfW71jdcMTx":456,"-LfDhcXHgjiNE6BxS9RW":456,"-LfDhcXJV_rtczgWPG4p":456,"-LfDhcXfk2ats88hFNBJ":456,"-LfDhcXwTVjKi04LT3Cg":456,"-LfDhcYHJz9le3IWft-Q":456,"-LfDhcYXoD55kqDnCenG":456,"-LfDhcYmw4ohsa1QqYkU":456,"-LfDhcYz8HRj01EF-RRM":456,"-LfDhcZF8bhpAlrq5AXv":456,"-LfDhcZaTmbm6Zc4usMy":456,"-LfDhcZrnULna2FeTxiE":456,"-LfDhc_4elxbrlphPw0K":456,"-LfDhc_OIz3ar02TfAa1":456,"-LfDhc_pbH2N__bP_85m":456,"-LfDhc_r1qDf8bg6zVbp":456,"-LfDhcaEblw1oYL-ny83":456,"-LfDhcaZxrUowei2v7eX":456,"-LfDhcam-7u9o9FAhBxL":456,"-LfDhcb5qVCKFLOHpvg1":456,"-LfDhcbIsZmrC7IbxniU":456,"-LfDhcbdsqSfIzZptwUo":456,"-LfDhcbs8SXXeVaVsICv":456,"-LfDhccHNxBv4CerecNp":456,"-LfDhccRXtfBywGLYhzJ":456,"-LfDhcchJqf84DffbQCy":456} }
            Debug.Log("GOT STREAM MESSAGE FOR UPDATES: " + path + ": " + message.Event + ": " + message.Data);
            if(message.Event != "put" && message.Event != "patch") {
                return;
            }

            object obj = BestHTTP.JSON.Json.Decode(message.Data);
            if(obj == null) {
                Debug.Log("JSON Parse error in stream");
                return;
            }

            Dictionary<string, object> dict = obj as Dictionary<string, object>;
            if(dict == null) {
                Debug.Log("Could not convert JSON to dict in stream");
                return;
            }

            object pathObj = null;
            dict.TryGetValue("path", out pathObj);
            if(!(pathObj is string)) {
                Debug.Log("Could not find path");
                return;
            }

            string pathStr = pathObj.ToString();
            if(pathStr == "/") {
                object dataObj = null;
                dict.TryGetValue("data", out dataObj);
                Dictionary<string, object> dataDict = dataObj as Dictionary<string, object>;
                if(dataDict == null) {
                    //nothing interesting about a null case, not an error, but just return.
                    // /updates: put: { "path":"/","data":null}
                    //

                    return;
                }

                // CASE:
                // /updates: put: { "path":"/","data":{ "-LfDhcP2dHYDFMLsuYas":456,"-LfDhcP5ycHlsxFG0-e3":456,"-LfDhcPGUVPqokbKb4oq":456,"-LfDhcP_WQEVRMfqTJPC":456,"-LfDhcPn0C6Q3ZWJSry9":456,"-LfDhcPvKBCrUxBnjuDS":456,"-LfDhcPyin53D3DlUdbS":456,"-LfDhcQ85AIkcX52fdRV":456,"-LfDhcQLduAD35URjMZe":456,"-LfDhcQLduAD35URjMZf":456,"-LfDhcQp5tZ_4dRy5mlz":456,"-LfDhcQuO0qU2vrJF5HA":456,"-LfDhcR0ErA41sCvNenE":456,"-LfDhcR0ErA41sCvNenF":456,"-LfDhcRAKho7ATMyEqUC":456,"-LfDhcRVPw_iQ_6u_JrR":456,"-LfDhcRicOGvGcasIZv8":456,"-LfDhcRn454gknViF2bB":456,"-LfDhcRn454gknViF2bC":456,"-LfDhcRpIH25qDLWbZU1":456,"-LfDhcS9zR7V1Xf6rRt-":456,"-LfDhcSCPM7o0ePx4JAI":456,"-LfDhcSFba_vOSiAPQ2b":456,"-LfDhcSQk-uKJtaxrTAv":456,"-LfDhcSdTGT1tXeI2awu":456,"-LfDhcSr75vqw-HjydfK":456,"-LfDhcTIYAEK2SSFmpvF":456,"-LfDhcTiCfN4lwd8OZs5":456,"-LfDhcTvmgpEU1t4NFWA":456,"-LfDhcU7bGBupHPRRztI":456,"-LfDhcUZQZAEVr3xX-gy":456,"-LfDhcUm_WSiPLK1TDmr":456,"-LfDhcVH4dSSoOWYfmOF":456,"-LfDhcVSWXW3vV7kRTqI":456,"-LfDhcVpcTRIJTtpWP53":456,"-LfDhcW4Gmcw6D1QhLa8":456,"-LfDhcW_VE0Dc8UMA7_Q":456,"-LfDhcWsxAfW71jdcMTx":456,"-LfDhcXHgjiNE6BxS9RW":456,"-LfDhcXJV_rtczgWPG4p":456,"-LfDhcXfk2ats88hFNBJ":456,"-LfDhcXwTVjKi04LT3Cg":456,"-LfDhcYHJz9le3IWft-Q":456,"-LfDhcYXoD55kqDnCenG":456,"-LfDhcYmw4ohsa1QqYkU":456,"-LfDhcYz8HRj01EF-RRM":456,"-LfDhcZF8bhpAlrq5AXv":456,"-LfDhcZaTmbm6Zc4usMy":456,"-LfDhcZrnULna2FeTxiE":456,"-LfDhc_4elxbrlphPw0K":456,"-LfDhc_OIz3ar02TfAa1":456,"-LfDhc_pbH2N__bP_85m":456,"-LfDhc_r1qDf8bg6zVbp":456,"-LfDhcaEblw1oYL-ny83":456,"-LfDhcaZxrUowei2v7eX":456,"-LfDhcam-7u9o9FAhBxL":456,"-LfDhcb5qVCKFLOHpvg1":456,"-LfDhcbIsZmrC7IbxniU":456,"-LfDhcbdsqSfIzZptwUo":456,"-LfDhcbs8SXXeVaVsICv":456,"-LfDhccHNxBv4CerecNp":456,"-LfDhccRXtfBywGLYhzJ":456,"-LfDhcchJqf84DffbQCy":456} }
                foreach(KeyValuePair<string, object> p in dataDict) {
                    callback(p.Value);
                }

                //indicate end of batch processing of initial batch.
                callback(null);
            } else {
                // CASE:
                // /updates: put: { "path":"/-LfDhcb5qVCKFLOHpvg1","data":456}

                object dataObj = null;
                dict.TryGetValue("data", out dataObj);
                callback(dataObj);
            }
        };

        eventSource.On("put", onMsgFn);
        eventSource.On("patch", onMsgFn);

        eventSource.Open();
    }

    public static DataStore instance = null;

    // Use this for initialization
    void Start()
    {
        instance = this;
    }

    private void Awake()
    {
        BestHTTP.HTTPManager.MaxConnectionPerServer = 32;
    }

    // Update is called once per frame
    void Update()
    {
        foreach(Monitor monitor in _dataMonitors) {
            monitor.Update();
        }
    }
}
