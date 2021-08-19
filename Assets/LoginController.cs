using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class LoginController : MonoBehaviour
{
    string loginCookie {
        get {
            if(_loginCookie != "") {
                return _loginCookie;
            }

            return PlayerPrefs.GetString("LoginCookie", "");
        }
        set {
            _loginCookie = value;
        }
    }

    string _loginCookie = "";

    [Serializable]
    public struct LoginMessage
    {
        public string email;
        public string password;
        public bool returnSecureToken;
    }

    [Serializable]
    struct RegisterAnonymouslyMessage
    {
        public bool returnSecureToken;
    }


    [Serializable]
    struct RegisterMessage
    {
        public string email;
        public string password;
        public string displayName;
        public bool returnSecureToken;
    }

    [Serializable]
    public struct VerifyTokenMessage
    {
        public string grant_type;
        public string refresh_token;
    }

    [Serializable]
    public class LoginError
    {
        public int code = 0;
        public string message = "";
    }

    [Serializable]
    public class LoginResponse
    {
        public LoginError error = null;

        public string errorMessage = null;

        public string kind;
        public string localId;
        public string email;
        public string displayName;
        public string firstName;
        public string photoUrl;

        public string idToken;
        public bool registered = true;
        public string refreshToken;
        public string expiresIn;

        public string providerId = null;

        public void Init()
        {
            if(providerId == "facebook.com") {
                displayName = firstName;
            }
        }
    }

    [Serializable]
    public class AuthResponse
    {
        public string expires_in;
        public string token_type;
        public string refresh_token;
        public string id_token;
        public string user_id;
        public string project_id;
    }

    //TODO: refresh info based on expiry.
    LoginResponse _loginInfo = null;
    AuthResponse _authInfo = null;

    public bool isLoggedIn {
        get { return _loginInfo != null && _authInfo != null; }
    }

    public string token {
        get { return _authInfo.id_token; }
    }

    public string firebaseUserId {
        get { return _authInfo.user_id; }
    }

    [System.Serializable]
    public struct ErrorMessageEntry
    {
        public string key;
        public string value;
    }

    string _apikey = "AIzaSyCbs9Bv98KpwyCJeJl44KZ3Eclamc_ZbZA";

    public void RegisterAsGuest()
    {
        Register();
    }

    public void Register()
    {
        loginCookie = "";
        PlayerPrefs.SetString("LoginCookie", "");
        StartCoroutine(DoLogin(true));
    }

    public void Login()
    {
        StartCoroutine(DoLogin(false));
    }
    
    bool _loggedIn = false;

    IEnumerator DoLogin(bool register = false, bool refreshConnection = false)
    {
        
        if(loginCookie != "") {
            try {
                Debug.Log("LOGGING IN USING COOKIE: (" + loginCookie + ")");
                _loginInfo = JsonUtility.FromJson<LoginResponse>(loginCookie);

                if(_loginInfo != null) {
                    _loginInfo.Init();

                    yield return ExchangeFirebaseRefreshTokenForSession(register, refreshConnection);
                    yield break;
                }
            } finally {
                //just in case loginCookie is somehow corrupted
            }
        }

        string email, passwd;

        string payload;

        if(register) {

                //we are registering anonymously
                RegisterAnonymouslyMessage message;
                message.returnSecureToken = true;
                payload = JsonUtility.ToJson(message);

                email = "";
                passwd = "";
        } else {
            LoginMessage message = new LoginMessage();
            //email = message.email = _emailInput.text;
            //passwd = message.password = _passwdInput.text;
            message.returnSecureToken = true;
            payload = JsonUtility.ToJson(message);
        }

        string url = "https://www.googleapis.com/identitytoolkit/v3/relyingparty/" + (register ? "signupNewUser" : "verifyPassword") + "?key=" + _apikey;

        var request = new UnityWebRequest(url);
        request.method = "POST";
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.uploadHandler.contentType = "application/json";
        request.SetRequestHeader("Content-Type", "application/json");
        request.chunkedTransfer = false;

        yield return request.SendWebRequest();


        Debug.Log("LOGIN RESPONSE: " + request.downloadHandler.text);


        if(request.isNetworkError || request.isHttpError) {
            Debug.Log("Error sending request to " + url + " code: " + request.responseCode + " error: " + request.error);
            Debug.Log("PAYLOAD: " + payload);

            HandleHTTPError(request, register);
        } else {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            if(response.error != null && response.error.message != "") {
                Debug.Log("Firebase auth error: " + response.error.message);
            } else {
                Debug.Log("Authenticated: " + response.kind);
                _loginInfo = response;
                _loginInfo.Init();

                //Now exchange our token for the user id.
                yield return ExchangeFirebaseRefreshTokenForSession(register, refreshConnection);
            }
        }
    }

    public bool guestAccount = false;

    IEnumerator ExchangeFirebaseRefreshTokenForSession(bool register = false, bool refreshConnection = false)
    {
        string url = "https://securetoken.googleapis.com/v1/token?key=" + _apikey;

        VerifyTokenMessage message = new VerifyTokenMessage();
        message.refresh_token = _loginInfo.refreshToken;
        message.grant_type = "refresh_token";
        string payload = JsonUtility.ToJson(message);

        UnityWebRequest request = new UnityWebRequest(url);
        request.method = "POST";
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
        request.downloadHandler = new DownloadHandlerBuffer();

        request.uploadHandler.contentType = "application/json";
        request.SetRequestHeader("Content-Type", "application/json");
        request.chunkedTransfer = false;

        yield return request.SendWebRequest();

        Debug.Log("AUTH EXCHANGE RESPONSE: " + request.downloadHandler.text);

        while(request.isNetworkError) {
            yield return request.SendWebRequest();
        }

        if(request.isHttpError) {
            Debug.Log("Error sending request to " + url + " code: " + request.responseCode + " error: " + request.error);
            Debug.Log("PAYLOAD: " + payload);

            HandleHTTPError(request, register);
            yield break;
        }

        _authInfo = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);


        _connectionExpiryTime = Time.unscaledTime + (float)int.Parse(_authInfo.expires_in) - 10.0f;

        Debug.Log("Login firebase ID: " + firebaseUserId + " connection expiry: " + _authInfo.expires_in);

        bool guest = false;
        Debug.Log("LOG EMAIL: " + _loginInfo.email);

        guestAccount = guest;


        _loggedIn = true;
        DataStore.dataErrors = 0;
        DataStore.authRevoked = false;

        _dataStore.gameObject.SetActive(true);

        _dataStore.PutData("/test", "4");

        Debug.Log("LOGGED IN SUCCESSFULLY");
    }

    [SerializeField]
    DataStore _dataStore = null;

    void HandleHTTPError(UnityWebRequest request, bool register)
    {
        Debug.Log("HTTP Error");
        httpError = "HTTP failure";
    }

    public string httpError = "";

    float _connectionExpiryTime = -1.0f;




    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DoLogin(true));
    }

    // Update is called once per frame
    void Update()
    {
        bool refreshConnectionKeyCommand = Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftControl);
        if(refreshConnectionKeyCommand || (_connectionExpiryTime > 0.0f && Time.unscaledTime > _connectionExpiryTime) || DataStore.dataErrors > 10 || DataStore.authRevoked) {
            DataStore.dataErrors = 0;
            DataStore.authRevoked = false;
            _connectionExpiryTime = -1.0f;
            StartCoroutine(DoLogin(true));
            Debug.Log("Refreshing connection...");
        }
    }
}
