using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameHarness : MonoBehaviour
{
    [SerializeField]
    DebugGameScreen _debugGameScreen = null;

    [SerializeField]
    Camera _dummyCamera = null;
    [SerializeField]
    GameController _gameController = null;

    GameController _backupGameController = null;

    GameController _oldGameController = null;
    int _oldControllerDeleteTime = -1;

    [SerializeField]
    Titlescreen _titlescreen = null;

    [SerializeField]
    BrowseGamesScreen _browseGamesScreen = null;

    BrowseGamesScreen _browseGamesScreenInstance = null;

    [SerializeField]
    LoadingScreen _loadingScreen = null;

    LoadingScreen _loadingScreenInstance = null;

    [SerializeField]
    ChooseTeamScreen _chooseTeamScreen = null;

    ChooseTeamScreen _chooseTeamScreenInstance = null;

    [SerializeField]
    Button _newGameButton = null, _loadGameButton = null, _quitButton = null;

    public void BrowseGames()
    {
        DestroyBrowseGamesScreenInstance();
        _browseGamesScreenInstance = Instantiate(_browseGamesScreen, transform);
        _browseGamesScreenInstance.gameObject.SetActive(true);
        _titlescreen.gameObject.SetActive(false);
    }

    void DestroyBrowseGamesScreenInstance()
    {
        if(_browseGamesScreenInstance != null) {
            GameObject.Destroy(_browseGamesScreenInstance.gameObject);
            _browseGamesScreenInstance = null;
        }
    }

    public void ExitBrowseGames()
    {
        DestroyBrowseGamesScreenInstance();
        _titlescreen.gameObject.SetActive(true);
    }

    void InitGameController(GameController gameController)
    {
        GameController.instance = gameController;
    }

    public void CancelChooseGameScreen()
    {
        GameObject.Destroy(_chooseTeamScreenInstance.gameObject);
        _chooseTeamScreenInstance = null;
        _titlescreen.gameObject.SetActive(true);
    }

    public void NewGame()
    {
        _chooseTeamScreenInstance = Instantiate(_chooseTeamScreen, transform);
        _chooseTeamScreenInstance.gameObject.SetActive(true);

        _titlescreen.gameObject.SetActive(false);
    }

    public void PlayNewGame()
    {
        GameConfig.instance.username = _chooseTeamScreen.username;
        GameConfig.instance.allowObservers = _chooseTeamScreen.allowObservers;

        Team team = _chooseTeamScreenInstance.chosenTeam;

        _gameController.mapGenerator.randomSeed = _chooseTeamScreenInstance.seed;

        _gameController.mapGenerator.SetPlayerTeam(team, _chooseTeamScreenInstance.difficulty);
        _gameController.gameState.difficulty = _chooseTeamScreenInstance.difficulty;

        PlayerPrefs.SetInt("difficulty", _gameController.gameState.difficulty);

        Debug.Log("Set difficulty: " + _gameController.gameState.difficulty);

        GameObject.Destroy(_chooseTeamScreenInstance.gameObject);
        _chooseTeamScreenInstance = null;

        InitGameController(_gameController);
        _gameController.gameState.guid = System.Guid.NewGuid().ToString();
        StartCoroutine(_gameController.mapGenerator.GenerateMap());
        StartCoroutine(WaitForMapGeneration(team));
    }

    public void SpectateGame(CloudInterface.OpenSession session)
    {
        GameConfig.instance.username = _browseGamesScreenInstance.username;

        InitGameController(_gameController);

        _loadingScreenInstance = Instantiate(_loadingScreen, transform);

        _loadingScreenInstance.waitingForBeginButton = false;
        _loadingScreenInstance.complete = false;

        StartCoroutine(_gameController.SpectateGame(_loadingScreenInstance, session.gameId));
        StartCoroutine(WaitForLoad());
    }

    IEnumerator WaitForMapGeneration(Team playerTeam)
    {
        _loadingScreenInstance = Instantiate(_loadingScreen, transform);

        _loadingScreenInstance.playerTeam = playerTeam;
        _loadingScreenInstance.waitingForBeginButton = true;

        _titlescreen.gameObject.SetActive(false);
        _loadingScreenInstance.gameObject.SetActive(true);

        while(_gameController.mapGenerator.finished == false) {
            _loadingScreenInstance.UpdateProgress(_gameController.mapGenerator.progressDescription, _gameController.mapGenerator.progressPercent);
            yield return null;
        }

        _loadingScreenInstance.complete = true;

        while(_loadingScreenInstance.waitingForBeginButton) {
            yield return null;
        }

        _gameController.StartNewGame();
        _gameController.gameObject.SetActive(true);

        while(_gameController.readyToDisplay == false) {
            yield return null;
        }

        GameObject.Destroy(_loadingScreenInstance.gameObject);
        _dummyCamera.gameObject.SetActive(false);
        _gameController.ActivateUI();
    }

    public bool replayGameOnLoad = false;
    public int replayStart = 0;

    public void LoadGame()
    {
        InitGameController(_gameController);

        _loadingScreenInstance = Instantiate(_loadingScreen, transform);

        _loadingScreenInstance.waitingForBeginButton = false;
        _loadingScreenInstance.complete = false;

        if(replayGameOnLoad) {
            StartCoroutine(_gameController.ReplayGame(_loadingScreenInstance, replayStart));
        } else {
            StartCoroutine(_gameController.LoadGame(_loadingScreenInstance));
        }

        StartCoroutine(WaitForLoad());
    }

    IEnumerator WaitForLoad()
    {
        _debugGameScreen.gameObject.SetActive(false);
        _titlescreen.gameObject.SetActive(false);
        DestroyBrowseGamesScreenInstance();
        _loadingScreenInstance.gameObject.SetActive(true);

        while(_loadingScreenInstance.complete == false) {
            yield return null;
        }

        _gameController.GameLoaded();
        _gameController.gameObject.SetActive(true);

        while(_gameController.readyToDisplay == false) {
            yield return null;
        }

        GameObject.Destroy(_loadingScreenInstance.gameObject);

        _dummyCamera.gameObject.SetActive(false);
        _gameController.ActivateUI();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ReturnFromGame()
    {
        _gameController.gameObject.SetActive(false);
        _oldGameController = _gameController;
        _oldControllerDeleteTime = 3;

        _gameController = _backupGameController;
        _backupGameController = null;

        _dummyCamera.gameObject.SetActive(true);
        _titlescreen.gameObject.SetActive(true);

        Start();
    }

    private void Awake()
    {
        _gameController.GameStartSetup();
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.developerConsoleVisible = false;

        Glowwave.Json.UnitTest();

        _backupGameController = Instantiate(_gameController, transform);

        _loadGameButton.interactable = _gameController.hasSaveState;

        if(_debugGameScreen != null && string.IsNullOrEmpty(_debugGameScreen.debugGameGuid) == false) {
            _titlescreen.gameObject.SetActive(false);
            _debugGameScreen.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(_oldGameController != null) {
            if(_oldControllerDeleteTime-- <= 0) {
                GameObject.Destroy(_oldGameController);
                _oldGameController = null;
            }
        }
    }
}
