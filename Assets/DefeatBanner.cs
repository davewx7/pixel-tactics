using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class DefeatBanner : MonoBehaviour
{
    [SerializeField]
    ChooseTeamPanel _chooseTeamPanelPrefab = null;

    ChooseTeamPanel _chooseTeamPanel = null;

    public void RemoveChooseTeamPanel()
    {
        if(_chooseTeamPanel != null) {
            _chooseTeamPanel.gameObject.SetActive(false);
        }
    }

    [SerializeField]
    Slider _playerProgressSlider = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _playerXPText = null, _playerLevelText = null;

    [SerializeField]
    Button _continueButton = null;

    [SerializeField]
    ScoreDialog _scoreDialog = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _summaryText = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _outcomeText = null;

    public void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    public void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    class SummaryComponents
    {
        public string rulerName = "";
        public string rulerTitle = "";
        public string reignPrefix = "";
        public string reignLength = "";
        public string fateTitle = "";
        public string fateDescription = "";
    }

    void UpdateSummary(SummaryComponents c)
    {
        _summaryText.text = string.Format("<color=#ffffff>{0}</color><color=#aaaaaa>{1}</color>\n<color=#aaaaaa>{2}</color><color=#ffffff>{3}</color>\n<color=#aaaaaa>{4}</color><color=#ffffff>{5}</color>", c.rulerName, c.rulerTitle, c.reignPrefix, c.reignLength, c.fateTitle, c.fateDescription);
    }

    public Unit ruler;
    public bool victory = false;

    IEnumerator Run()
    {
        SummaryComponents components = new SummaryComponents();

        string rulerName = ruler.unitInfo.characterName;
        for(int i = 0; i != rulerName.Length; ++i) {
            components.rulerName += rulerName[i];
            UpdateSummary(components);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        string[] titles = {
            "forgotten",
            "inept",
            "obscure",
            "desperate",
            "stoic",
            "unkneeling",
            "noble",
            "brave",
            "relentless",
            "heroic",
            "valiant",
            "great",
        };

        int titleIndex = GameController.instance.playerTeamInfo.scoreInfo.totalScore/500;
        if(titleIndex >= titles.Length) {
            titleIndex = titles.Length-1;
        }

        string title = titles[titleIndex];
        string rulerTitle = string.Format(", the {0}", title);

        for(int i = 0; i != rulerTitle.Length; ++i) {
            components.rulerTitle += rulerTitle[i];
            UpdateSummary(components);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        string reignPrefix = "Ruled for ";
        if(victory) {
            reignPrefix = "Ascended the throne after ";
        }
        for(int i = 0; i != reignPrefix.Length; ++i) {
            components.reignPrefix += reignPrefix[i];
            UpdateSummary(components);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        int nround = GameController.instance.gameState.nround;
        string reignYears = (nround < 12 ? "" : (nround < 24 ? "One year" : string.Format("{0} years", nround/12)));

        int nmonth = nround%12;
        string reignMonths = "";
        if(nmonth == 1) {
            reignMonths += "One moon";
        } else if(nmonth > 1) {
            reignMonths += string.Format("{0} moons", nmonth);
        }

        string joinMonthYears = (reignYears.Length > 0 && reignMonths.Length > 0) ? " and " : "";

        string reignLength = reignYears + joinMonthYears + reignMonths;

        for(int i = 0; i != reignLength.Length; ++i) {
            components.reignLength += reignLength[i];
            UpdateSummary(components);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        string fateTitle = "Fate: ";
        for(int i = 0; i != fateTitle.Length; ++i) {
            components.fateTitle += fateTitle[i];
            UpdateSummary(components);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        string fateDescription = "escaped into exile";

        if(GameController.instance.unitKillingPlayer != null) {
            fateDescription = string.Format("slain by {0}", GameController.instance.unitKillingPlayer.team.teamNameAsProperNoun);
        }

        bool rulerFemale = ruler.unitInfo.gender == UnitGender.Female;

        if(victory) {
            fateDescription = string.Format("Became {0} of Wesnoth", rulerFemale ? "Queen" : "King");
        }

        for(int i = 0; i != fateDescription.Length; ++i) {
            components.fateDescription += fateDescription[i];
            UpdateSummary(components);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        _scoreDialog.UpdateScores(GameController.instance.playerTeamInfo);
        _scoreDialog.gameObject.SetActive(true);

        yield return _scoreDialog.StartCoroutine(_scoreDialog.AnimateScores());

        yield return new WaitForSeconds(1f);

        GameController.instance.map.gameObject.SetActive(false);

        _continueButton.gameObject.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        _summaryText.text = "";
        _continueButton.gameObject.SetActive(false);
        _scoreDialog.gameObject.SetActive(false);

        _outcomeText.text = victory ? "Victory!" : "Defeat!";

        StartCoroutine(Run());
    }

    int _prevScore = 0;

    public int playerMaxExperience {
        get {
            return GameConfig.instance.GetPlayerLevel(GameConfig.instance.playerLevel).xpcost;
        }
    }

    public int playerExperience {
        get {
            return PlayerPrefs.GetInt("PlayerXP", 0);
        }
        set {
            PlayerPrefs.SetInt("PlayerXP", value);
        }
    }

    bool _firstTime = true;

    // Update is called once per frame
    void Update()
    {
        if(_scoreDialog != null) {
            int newScore = _scoreDialog.scoreTotal;
            if(_firstTime || newScore > _prevScore) {
                _firstTime = false;

                int delta = newScore - _prevScore;
                _prevScore = newScore;

                playerExperience = playerExperience + delta;
                if(playerExperience >= playerMaxExperience) {
                    var unlocks = GameConfig.instance.GetPlayerLevel(GameConfig.instance.playerLevel);

                    if(unlocks.unlockTeam && GameConfig.instance.teamsUnlocked < GameConfig.instance.playerTeams.Count) {
                        Debug.Log("Unlock team");
                        var unlockedTeam = GameConfig.instance.playerTeams[GameConfig.instance.teamsUnlocked];

                        var panel = Instantiate(_chooseTeamPanelPrefab, transform);
                        panel.transform.localPosition -= new Vector3(0f, 1f, 0f)*180f;

                        Vector3 pos = panel.transform.localPosition;
                        panel.transform.localPosition += new Vector3(700f, 0f, 0f);

                        var tween = panel.transform.DOLocalMove(pos, 2f);

                        tween.onComplete += panel.UnlockAnim;

                        panel.SetDefeatBanner(this);
                        panel.interactable = true;
                        panel.team = unlockedTeam;
                        panel.gameObject.SetActive(true);

                        _chooseTeamPanel = panel;

                        GameConfig.instance.teamsUnlocked += 1;
                    } else {
                        Debug.Log("No Unlock team for level " + GameConfig.instance.playerLevel + ": " + unlocks.unlockTeam);
                    }

                    playerExperience = playerExperience - playerMaxExperience;
                    GameConfig.instance.playerLevel = GameConfig.instance.playerLevel + 1;

                    var sequence = DOTween.Sequence();

                    Color startingColor = _playerLevelText.color;

                    sequence.Append(_playerLevelText.DOColor(new Color(2f, 2f, 2f, 2f), 0.5f));
                    sequence.AppendInterval(0.5f);
                    sequence.Append(_playerLevelText.DOColor(startingColor, 2f));
                }

                _playerProgressSlider.value = ((float)playerExperience) / (float)playerMaxExperience;
                _playerXPText.text = string.Format("Experience: {0}/{1}", playerExperience, playerMaxExperience);
                _playerLevelText.text = string.Format("Level: {0}", GameConfig.instance.playerLevel+1);
            }
        }
    }
}
