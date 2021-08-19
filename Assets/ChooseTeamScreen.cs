using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ChooseTeamScreen : MonoBehaviour
{
    [SerializeField]
    Button _playButton = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _playText = null;

    [SerializeField]
    TMPro.TMP_InputField _nameInput = null, _seedInput = null;


    [SerializeField]
    TMPro.TMP_Dropdown _difficultyCombo = null;

    [SerializeField]
    ChooseTeamPanel _chooseTeamPanelProto = null;

    public Team chosenTeam = null;

    List<ChooseTeamPanel> _panels = new List<ChooseTeamPanel>();

    public int difficulty {
        get {
            return _difficultyCombo.value;
        }
    }

    public void ChoosePanel(ChooseTeamPanel panel)
    {
        chosenTeam = panel.team;

        foreach(var p in _panels) {
            p.selected = (p == panel);
        }

        _playButton.gameObject.SetActive(true);
        _playText.text = string.Format("Play as {0}", panel.team.rulerName);
    }

    [SerializeField]
    Toggle _allowObserversToggle = null;

    public bool allowObservers {
        get {
            return _allowObserversToggle.enabled;
        }
    }

    public string username {
        get {
            return _nameInput.text;
        }
    }

    int _seed = -1;

    public int seed {
        get {
            return _seed;
        }
    }

    public void ChangeSeed(string seedStr)
    {
        int result = 0;
        if(int.TryParse(_seedInput.text, out result)) {
            _seed = result;
            Debug.Log("RNG: CHANGE SEED TO " + _seed);
        } else {
            if(_seedInput.text != "") {
                _seedInput.text = string.Format("{0}", _seed);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _nameInput.text = GameConfig.instance.username;

        _seed = new ConsistentRandom().Next();
        _seedInput.text = string.Format("{0}", _seed);

        foreach(ChooseTeamPanel panel in _panels) {
            GameObject.Destroy(panel.gameObject);
        }

        _panels.Clear();

        float left = -360;
        float right = 360f;

        float segmentWidth = (right-left)/(GameConfig.instance.playerTeams.Count-1);

        int teamsUnlocked = GameConfig.instance.teamsUnlocked;

        int index = 0;
        foreach(Team team in GameConfig.instance.playerTeams) {
            ChooseTeamPanel panel = Instantiate(_chooseTeamPanelProto, transform);
            panel.team = team;
            panel.transform.localPosition += new Vector3(left + index*segmentWidth, 0f, 0f);
            panel.locked = (index >= teamsUnlocked);
            panel.gameObject.SetActive(true);
            _panels.Add(panel);

            ++index;
        }

        _difficultyCombo.value = PlayerPrefs.GetInt("difficulty", 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
