using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BrowseGamePanel : MonoBehaviour
{
    public CloudInterface.OpenSession gameSession = null;

    [SerializeField]
    GameHarness _gameHarness = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _nameText = null, _characterText = null, _roundText = null;

    [SerializeField]
    Image _avatarImage = null;

    [SerializeField]
    InputField _guidText = null, _seedText = null;

    public void OnSpectate()
    {
        _gameHarness.SpectateGame(gameSession);
    }

    // Start is called before the first frame update
    void Start()
    {
        _nameText.text = gameSession.username;
        _characterText.text = gameSession.rulerName;
        _roundText.text = string.Format("Round {0}", gameSession.nround+1);
        _avatarImage.sprite = gameSession.unitType.avatarImage;
        _guidText.text = gameSession.gameId;
        _seedText.text = gameSession.gameSeed.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
