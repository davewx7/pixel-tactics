using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDiplomacyManager : MonoBehaviour
{
    public IEnumerator LearnSpells(Unit unit, GenericCommandInfo cmd, string message)
    {
        while(cmd.running == false) {
            yield return null;
        }

        GameController.instance.ShowDialogMessage("Spells Learned", message, null);
        while(GameConfig.modalDialog > 0) {
            yield return null;
        }

        unit.unitInfo.RefreshSpells();

        cmd.Finish();

        GameController.instance.unitDisplayed = unit;
        GameController.instance.PrepareSpells();
    }

    public IEnumerator VillageMarket(Unit unit, GenericCommandInfo cmd)
    {
        while(cmd.running == false) {
            yield return null;
        }

        GameController.instance.ShowDialogMessage("A Secret Market", "The merchant leads you to his wagon, pulling back the curtain on it, he shows you his wares...", null);
        while(GameConfig.modalDialog > 0) {
            yield return null;
        }

        cmd.Finish();

        GameController.instance.unitDisplayed = unit;
        GameController.instance.VisitShop();
    }

    public IEnumerator ShareInfo(BoonInfo.Info info, GenericCommandInfo cmd)
    {
        while(cmd.running == false) {
            yield return null;
        }

        GameController.instance.ShowDialogMessage("Intelligence Gathering...", "Spending some time at one of the village's taverns with this informant, you learn some information which may be of value...", null);
        while(GameConfig.modalDialog > 0) {
            yield return null;
        }

        var scrollCmd = GameController.instance.CreateScrollCameraCmd(info.locs[0]);
        scrollCmd.scrollToFog = true;
        scrollCmd.gameObject.SetActive(true);

        while(scrollCmd.finished == false) {
            yield return null;
        }

        GameObject.Destroy(scrollCmd.gameObject);

        List<Tile> reveal = new List<Tile>();
        foreach(Loc centerLoc in info.locs) {
            foreach(Loc loc in Tile.GetTilesInRadius(centerLoc, 2)) {
                if(GameController.instance.map.LocOnBoard(loc)) {
                    GameController.instance.playerTeamInfo.mapRevealed.Add(loc);
                    reveal.Add(GameController.instance.map.GetTile(loc));
                }
            }
        }

        GameController.instance.RecalculateVision(reveal);

        GameConfig.modalDialog++;
        yield return new WaitForSeconds(1f);
        GameConfig.modalDialog--;

        GameController.instance.ShowDialogMessage(new ConversationDialog.Info() {
            title = "Intelligence Gathered",
            text = info.description,
            linkOptions = info.tooltips,
        });

        GameController.instance.ShowDialogMessage("Intelligence Gathered", info.description, null);
        while(GameConfig.modalDialog > 0) {
            yield return null;
        }

        cmd.Finish();
    }
    
    public IEnumerator RequestSwornEnemies(AIState state, GenericCommandInfo cmd)
    {
        while(cmd.running == false) {
            yield return null;
        }

        TeamInfo teamInfo = GameController.instance.currentTeamInfo;

        bool foundEnemies = false;

        foreach(Team enemy in teamInfo.enemies) {
            TeamInfo enemyTeamInfo = GameController.instance.gameState.GetTeamInfo(enemy);
            if(enemyTeamInfo.playerDiplomacyStatus != Team.DiplomacyStatus.Hostile) {
                foundEnemies = true;
            }
        }

        string enemyName = "";
        foreach(Team enemy in teamInfo.enemies) {
            TeamInfo enemyTeamInfo = GameController.instance.gameState.GetTeamInfo(enemy);
            if(foundEnemies && enemyTeamInfo.playerDiplomacyStatus == Team.DiplomacyStatus.Hostile) {
                //these are already at war with the player so don't include them in the demand.
                continue;
            }

            if(string.IsNullOrEmpty(enemyName) == false) {
                enemyName += " and ";
            }
            
            enemyName += "the " + string.Format("<color=#ffffff>{0}</color>", enemy.teamName);
        }

        if(foundEnemies == false) {
            string msg = string.Format("We note that you are enemies of {0}. This pleases us, it seems like you would make a wise ruler of the land.", enemyName);
            GameController.instance.ShowDialogMessage(teamInfo.team.teamName, msg, teamInfo.team.rulerType.portrait);
            while(GameConfig.modalDialog > 0) {
                yield return null;
            }

        } else {

            List<string> options = new List<string>() { "Agree", "Decline" };

            ConversationDialog.Result dialogResult = new ConversationDialog.Result();
            string message = string.Format("If you are the true ruler of Wesnoth, you surely recognize that {0} are a blight upon the land and must be destroyed. We would only ever consider swearing fealty to you and supporting your claim if you decree they be destroyed. Do you agree?", enemyName);
            GameController.instance.ShowDialogMessage(teamInfo.team.teamName, message, teamInfo.team.rulerType.portrait, options, dialogResult);
            while(dialogResult.finished == false) {
                yield return null;
            }

            int result = dialogResult.optionChosen;

            if(result == 0) {
                GameController.instance.ShowDialogMessage(teamInfo.team.teamName, "Excellent, you have made a wise decision.", teamInfo.team.rulerType.portrait);
                while(GameConfig.modalDialog > 0) {
                    yield return null;
                }

                foreach(Team enemy in teamInfo.enemies) {
                    TeamInfo enemyTeamInfo = GameController.instance.gameState.GetTeamInfo(enemy);
                    enemyTeamInfo.StartWarWithPlayer();
                }

            } else {
                GameController.instance.ShowDialogMessage(teamInfo.team.teamName, "You have made your choice, siding with our enemies. We will not support your claim, and will fight to stop you from seizing the throne, pretender that you are.", teamInfo.team.rulerType.portrait);
                while(GameConfig.modalDialog > 0) {
                    yield return null;
                }

                teamInfo.StartWarWithPlayer();
            }
        }

        cmd.Finish();
    }

    public static AIDiplomacyManager instance = null;

    private void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
