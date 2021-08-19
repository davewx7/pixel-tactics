using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/PersonalVisit")]
public class QuestPersonalVisit : Quest
{
    public DiplomacyNode offerNode = null;

    public override bool IsEligible(Team clientTeam)
    {
        foreach(QuestInProgress completedQuest in clientTeam.teamInfo.completedQuests) {
            if(completedQuest.quest is QuestPersonalVisit) {
                return false;
            }
        }

        return true;
    }

    public override QuestType questType {
        get { return QuestType.PersonalVisit; }
    }

    public override float ScoreQuest(QuestInProgress info)
    {
        Unit playerRuler = GameController.instance.playerTeamInfo.GetRuler();
        Unit clientRuler = info.clientTeam.teamInfo.GetRuler();

        if(playerRuler == null || clientRuler == null) {
            return 0f;
        }

        float score = 50f;

        foreach(TeamInfo otherTeam in GameController.instance.teams) {
            if(otherTeam.team != info.clientTeam) {
                foreach(QuestInProgress q in otherTeam.currentQuests) {
                    if(q.quest is QuestPersonalVisit) {
                        score -= 20f;
                    }
                }
            }
        }

        int distance = Tile.DistanceBetween(playerRuler.loc, clientRuler.loc);

        if(distance < 12) {
            score -= (12 - distance)*5f;
        }

        return score;
    }

    public override string GetDetails(QuestInProgress info)
    {
        return string.Format("In the days of old, the rulers of Wesnoth would come to visit and break bread with us. Yet it has been more than a century since a ruler of Wesnoth set foot in our domains, as humble as they are. Come and visit our castle and we shall discuss with you whether we should support your claim.");
    }

    public override string GetSummary(QuestInProgress info)
    {
        return string.Format("Make a personal visit to the {0} castle.", info.clientTeam.teamName);
    }

    public override string GetCompletionDescription(QuestInProgress info)
    {
        return "we have wanted a ruler to show us the courtesy of visiting us, and here you are";
    }

    public override string AchievementText(QuestInProgress info)
    {
        return "Making a personal visit";
    }


    public override void OnUnitArrivesAtLoc(Unit unit, QuestInProgress questInProgress)
    {
        //see if we've arrived at their castle, in which case we mark the quest as complete.
        if(unit.team.player && unit.unitInfo.ruler && unit.tile.terrain.rules.castle) {
            Debug.Log("QUEST UNIT ARRIVE: " + unit.loc);

            foreach(var ruler in GameController.instance.units) {
                if(ruler.unitInfo.ruler && ruler.team == questInProgress.clientTeam && ruler.tile.terrain.rules.keep) {
                    Dictionary<Loc, Pathfind.Path> recruitLocs = Pathfind.FindPaths(GameController.instance, unit.unitInfo, 1, new Pathfind.PathOptions() {
                        recruit = true,
                        excludeOccupied = false,
                    });

                    Debug.Log("HAVE RECRUIT LOCS: " + recruitLocs.Count);

                    if(recruitLocs.ContainsKey(unit.tile.loc)) {
                        Debug.Log("MARK QUEST COMPLETE");
                        questInProgress.count++;
                        break;
                    }
                }
            }
        } else if(unit.team.player && unit.unitInfo.ruler) {
            int dist = Tile.DistanceBetween(questInProgress.clientTeam.teamInfo.keepLoc, GameController.instance.playerTeamInfo.GetRuler().loc);
            questInProgress.progressEstimate = dist <= questInProgress.progressEstimateMax ? questInProgress.progressEstimateMax : 0;
        }
    }

    public override string QuestHint(QuestInProgress questInProgress)
    {
        return "I have heard that they feel forgotten by the rulers of Wesnoth, and wish that one would visit their castle.";
    }

    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        questInProgress.progressEstimateMax = Tile.DistanceBetween(clientTeam.teamInfo.keepLoc, GameController.instance.playerTeamInfo.GetRuler().loc)/2;
    }

    public List<DiplomacyNode> noProgressAlmostExpired;

    public override List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        if(AlmostFailed(questInProgress)) {
            return noProgressAlmostExpired;
        } else {
            return base.GetCustomCheckin(questInProgress);
        }
    }

    public override bool AlmostFailed(QuestInProgress questInProgress)
    {
        return questInProgress.progressEstimate < questInProgress.progressEstimateMax && questInProgress.timeUntilExpired <= 4;
    }

    public override bool FailedDeclareWar(QuestInProgress questInProgress)
    {
        return (questInProgress.timeUntilExpired <= 0 && questInProgress.progressEstimate < questInProgress.progressEstimateMax) || questInProgress.timeUntilExpired <= -4;
    }
}
