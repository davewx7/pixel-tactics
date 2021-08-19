using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiplomacyKingdomPanel : MonoBehaviour
{
    public TeamInfo teamInfo;

    [SerializeField]
    Image _avatarImage = null, _flagImage = null, _questIcon = null, _questCompleteIcon = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _rulerNameText = null, _kingdomNameText = null, _relationsText = null;

    [SerializeField]
    Slider _relationsSlider = null;

    // Start is called before the first frame update
    void Start()
    {
        _avatarImage.sprite = teamInfo.team.rulerType.avatarImage;
        _flagImage.color = teamInfo.team.coloring.color;

        _rulerNameText.text = teamInfo.team.rulerName;

        if(teamInfo.GetRuler() == null) {
            _rulerNameText.fontStyle = TMPro.FontStyles.Strikethrough;
        }

        _kingdomNameText.text = teamInfo.team.teamName;

        _relationsSlider.gameObject.SetActive(false);

        if(teamInfo.team.player) {
            _relationsText.text = "";
            _questIcon.gameObject.SetActive(false);
        } else if(teamInfo.GetRuler() == null) {
            _relationsText.text = "Dead";
            _relationsText.color = new Color(0.5f, 0.5f, 0.5f);
            _questIcon.gameObject.SetActive(false);
        } else {
            string mainTooltip = string.Format("{0}, of {1}\n", teamInfo.team.rulerName, teamInfo.team.teamNameAsProperNoun);
            foreach(TeamInfo otherTeam in GameController.instance.teams) {
                if(otherTeam != teamInfo && otherTeam.team.player == false && otherTeam.team.barbarian == false && otherTeam.team.primaryEnemy == false) {
                    if(teamInfo.team.IsAlly(otherTeam.team)) {
                        mainTooltip += string.Format("\n<color=#aaffaa>Friendly with {0}</color>", otherTeam.team.teamNameAsProperNoun);
                    }
                }
            }

            foreach(TeamInfo otherTeam in GameController.instance.teams) {
                if(otherTeam != teamInfo && otherTeam.team.player == false && otherTeam.team.barbarian == false && otherTeam.team.primaryEnemy == false) {
                    if(teamInfo.team.IsEnemy(otherTeam.team)) {
                        mainTooltip += string.Format("\n<color=#ffaaaa>Enemy of {0}</color>", otherTeam.team.teamNameAsProperNoun);
                    }
                }
            }

            UnitStatusPanel.SetTooltip(_rulerNameText, mainTooltip);


            switch(teamInfo.playerDiplomacyStatus) {
                case Team.DiplomacyStatus.Ally:
                    _relationsText.text = "Ally";
                    _relationsText.color = new Color(0.8f, 0.8f, 1f);
                    break;
                case Team.DiplomacyStatus.Peaceful:

                    var info = teamInfo.relationsWithPlayerDescription;
                    _relationsText.text = info.text;
                    _relationsText.color = info.color;
                    
                    _relationsSlider.gameObject.SetActive(true);
                    _relationsSlider.value = info.rating*0.01f;

                    UnitStatusPanel.SetTooltip(_relationsText, teamInfo.relationsWithPlayerTooltip);

                    break;
                case Team.DiplomacyStatus.Hostile:
                    _relationsText.text = "Hostile";
                    _relationsText.color = new Color(1f, 0.3f, 0.3f);
                    break;
            }


            _questIcon.gameObject.SetActive(teamInfo.currentQuests.Count > 0);
            if(teamInfo.currentQuests.Count > 0) {
                string tooltip = teamInfo.currentQuests[0].quest.GetSummary(teamInfo.currentQuests[0]);
                if(teamInfo.currentQuest.completed) {
                    tooltip = string.Format("This quest has been completed!\nTalk to {0} to receive a reward.", teamInfo.team.teamNameAsProperNoun);
                    _questCompleteIcon.gameObject.SetActive(true);
                } else {
                    _questCompleteIcon.gameObject.SetActive(false);

                    if(teamInfo.currentQuest.timeUntilExpired > 0) {
                        tooltip += string.Format("\n{0} expects you to complete this quest within {1} {2}", teamInfo.team.teamNameAsProperNounCap, teamInfo.currentQuest.timeUntilExpired, teamInfo.currentQuest.timeUntilExpired == 1 ? "round" : "rounds");
                    } else {
                        tooltip += string.Format("\n{0} expected you to have completed this quest by now and is growing frustrated.", teamInfo.team.teamNameAsProperNounCap);
                    }
                }
                UnitStatusPanel.SetTooltip(_questIcon, tooltip);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
