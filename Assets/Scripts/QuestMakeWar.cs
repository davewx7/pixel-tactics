using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Quests/MakeWar")]
public class QuestMakeWar : Quest
{
    public override QuestType questType {
        get { return QuestType.War; }
    }

    public DiplomacyNode dialogAlreadyFighting, dialogHasContact, dialogNoContact;

    //Score this quest vs other possible quests.
    public override float ScoreQuest(QuestInProgress info)
    {
        Unit playerRuler = GameController.instance.playerTeamInfo.GetRuler();
        Unit enemyRuler = info.enemyTeam.teamInfo.GetRuler();

        int nteamsWithPlayerContact = 0;
        foreach(TeamInfo teamInfo in GameController.instance.teams) {
            if(teamInfo.team.player == false && teamInfo.team.barbarian == false && teamInfo.playerContactRound >= 0) {
                nteamsWithPlayerContact++;
            }
        }

        if(enemyRuler == null) {
            return 0f;
        }

        bool newWar = info.enemyTeam.teamInfo.enemyOfPlayer == false;

        int numCurrentWars = 0;

        foreach(TeamInfo team in GameController.instance.teams) {
            if(team.team.barbarian || team.team.player || team.GetRuler() == null) {
                continue;
            }

            if(team.enemyOfPlayer) {
                ++numCurrentWars;
            }
        }

        float score = 70f;

        if(nteamsWithPlayerContact > 2) {
            if(numCurrentWars == 0) {
                score *= 2f;
            } else if(numCurrentWars == 1) {
                score *= 1.5f;
            }
        }

        int dist = Tile.DistanceBetween(playerRuler.loc, enemyRuler.loc);
        if(dist > 20) {
            score -= 8f*(dist - 20);
        }

        return score;
    }


    TeamInfo FindBestTarget(TeamInfo clientTeam, bool secondary=false)
    {
        TeamInfo banTeam = null;
        if(secondary) {
            banTeam = FindBestTarget(clientTeam);
        }

        Unit clientRuler = clientTeam.GetRuler();
        Unit playerRuler = GameController.instance.playerTeamInfo.GetRuler();
        if(playerRuler == null || clientRuler == null) {
            return null;
        }

        int existingEnemyStrength = 0;
        foreach(TeamInfo candidate in GameController.instance.teams) {
            if(candidate.team.regularAITeam && candidate.enemyOfPlayer) {
                existingEnemyStrength += candidate.numUnits;
            }
        }

        int playerUnits = GameController.instance.playerTeamInfo.numUnits;
        if(playerUnits <= 0) {
            playerUnits = 1;
        }

        //the higher the number this enemy is the more the player has already bitten off
        //in terms of enemies.
        float enemyRatio = existingEnemyStrength / (float)(playerUnits);

        float maxRatio = 3f;

        switch(GameController.instance.gameState.difficulty) {
            case 0: maxRatio = 0.8f; break;
            case 1: maxRatio = 2f; break;
            case 2: maxRatio = 10f; break;
        }

        bool avoidStartingNewWars = enemyRatio > maxRatio;

        Debug.Log("FIND WAR ENEMY...");

        float bestScore = 0f;
        TeamInfo bestEnemy = null;
        foreach(TeamInfo candidate in GameController.instance.teams) {
            if(banTeam == candidate) {
                continue;
            }

            if(candidate.team.player || candidate.team.barbarian || candidate.team.primaryEnemy || clientTeam.team.permanentFriends.Contains(candidate.team) || candidate.team == clientTeam.team) {
                continue;
            }

            if(avoidStartingNewWars && candidate.enemyOfPlayer == false) {
                continue;
            }

            Unit candidateRuler = candidate.GetRuler();
            if(candidateRuler == null) {
                continue;
            }

            float score = candidate.GetUnits().Count;
            if(candidate.hasPlayerContact) {
                //prefer to convince the player to make war with someone they have contact with.
                score *= 1.2f;
            }

            //good to have war both with someone near the player and also near the requester.
            int dist = Mathf.Max(14, Tile.DistanceBetween(playerRuler.loc, candidate.GetRuler().loc)) + Tile.DistanceBetween(clientRuler.loc, candidate.GetRuler().loc);

            score /= dist;

            if(clientTeam.team.swornEnemies.Contains(candidate.team)) {
                score *= 2f;
            }

            //score is reduced if there are existing teams that have requested war with this opponent.
            score /= (1f + candidate.teamsConspiredWithPlayerAgainst.Count);

            if(clientTeam.teamsPlayerRejectedConspiracy.Contains(candidate.team)) {
                //heavily prefer to just request war against a team that asked
                //the player to attack this team.
                score *= 10f;
            }

            Debug.LogFormat("WAR SCORE FOR {0}; score = {1}; num units = {2}; player contact = {3}; dist = {4}; sworn = {5}; existing wars = {6}", candidate.team.teamName, score, candidate.GetUnits().Count, candidate.hasPlayerContact, dist, clientTeam.team.swornEnemies.Contains(candidate.team), candidate.teamsConspiredWithPlayerAgainst.Count);

            if(score > bestScore) {
                bestEnemy = candidate;
                bestScore = score;
            }
        }

        if(bestEnemy != null) {
            Debug.LogFormat("WAR DECLARE: {0}", bestEnemy.team.teamName);
        }

        return bestEnemy;
    }

    public override bool IsEligible(Team clientTeam)
    {
        return FindBestTarget(clientTeam.teamInfo) != null;
    }

    public override Loc GetQuestTarget(QuestInProgress info)
    {
        var target = FindBestTarget(info.clientTeam.teamInfo, info.secondary);
        if(target != null) {
            return target.GetRuler().loc;
        }

        return Loc.invalid;
    }

    public override void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
        questInProgress.enemyTeam = FindBestTarget(clientTeam.teamInfo, questInProgress.secondary).team;
        questInProgress.progressEstimateMax = 4;
    }

    public override string GetDetails(QuestInProgress info)
    {
        return string.Format("Defeat {0}", info.enemyTeam.teamNameAsProperNoun);
    }

    public override string GetSummary(QuestInProgress info)
    {
        return string.Format("Defeat {0}", info.enemyTeam.teamNameAsProperNoun);
    }

    public override string GetCompletionDescription(QuestInProgress info)
    {
        return string.Format("{0} has fallen", info.enemyTeam.teamNameAsProperNoun);
    }

    public override string AchievementText(QuestInProgress info)
    {
        return "Defeating " + info.enemyTeam.teamNameAsProperNoun;
    }


    public override void OnAcceptQuest(QuestInProgress questInProgress)
    {
        //no longer instantly declare war.
        questInProgress.enemyTeam.teamInfo.relationsWithPlayerChanges.Add(new TeamInfo.RelationsWithPlayerEntry() {
            rating = -100,
            nround = GameController.instance.gameState.nround + 3,
            reason = string.Format("Conspiring against us with our enemies, {0}", questInProgress.clientTeam.teamNameAsProperNoun),
        });

        questInProgress.enemyTeam.teamInfo.teamsPlayerRejectedConspiracy.Remove(questInProgress.clientTeam);
        questInProgress.enemyTeam.teamInfo.teamsConspiredWithPlayerAgainst.Add(questInProgress.clientTeam);
    }

    public override void OnRequestQuest(QuestInProgress questInProgress)
    {
        //if an AI requests the player to attack another AI, those two AI's should now be
        //at war with one another.
        questInProgress.clientTeam.teamInfo.DeclareWarOn(questInProgress.enemyTeam.teamInfo);

        //The team we were asked to declare war on knows about it.
        questInProgress.enemyTeam.teamInfo.teamsPlayerRejectedConspiracy.Add(questInProgress.clientTeam);
    }

    public override void OnEnemyUnitKilled(Unit unit, QuestInProgress questInProgress)
    {
        if(unit.unitInfo.ruler && unit.team == questInProgress.enemyTeam) {
            ++questInProgress.count;
        } else if(unit.team == questInProgress.enemyTeam) {
            ++questInProgress.progressEstimate;
        }
    }

    public override string QuestHint(QuestInProgress questInProgress)
    {
        return string.Format("It is said that they are bitter enemies of {0} and are looking for allies to help them fight them.", questInProgress.enemyTeam.teamNameAsProperNoun);
    }

    public List<DiplomacyNode> goodEarlyProgress, noProgressAlmostExpired, goodProgressAlmostExpired;

    public override List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        if(questInProgress.progressEstimate >= 3 && questInProgress.timeUntilExpired >= 6) {
            return goodEarlyProgress;
        } else if(questInProgress.timeUntilExpired >= 6) {
            return checkinNode;
        } else if(questInProgress.progressEstimate >= 3) {
            return goodProgressAlmostExpired;
        } else {
            return noProgressAlmostExpired;
        }
    }
}