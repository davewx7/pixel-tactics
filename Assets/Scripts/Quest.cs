using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Quest : GWScriptableObject
{
    public List<DiplomacyNode> acceptedNode, rejectedNode, completedNode, checkinNode, failedNode;

    public enum QuestType { None, War, PersonalVisit, ClearDungeon, FosterUnit, LevelUpRuler, RetrieveItem, RecoverVillage };

    public virtual QuestType questType {
        get { return QuestType.None; }
    }

    public float debugBonusScore = 0f;

    public string questOfferTitle = "Quest";

    public int timeExpected = 12;

    public bool hasSecondary = false;

    public bool CanAssignQuest(Team clientTeam)
    {
        foreach(var q in clientTeam.teamInfo.completedQuests) {
            if(q.quest == this) {
                //don't allow the same type of quest multiple times.
                return false;
            }
        }

        return IsEligible(clientTeam);
    }

    public virtual bool IsEligible(Team clientTeam)
    {
        return true;
    }

    public virtual float ScoreQuest(QuestInProgress info)
    {
        return 0f;
    }

    public virtual string GetDetailsPrelude(QuestInProgress info)
    {
        return null;
    }

    public virtual Loc GetQuestTarget(QuestInProgress info)
    {
        return Loc.invalid;
    }

    public virtual int GetQuestReveal(QuestInProgress info)
    {
        return 0;
    }

    public virtual string GetDetails(QuestInProgress info)
    {
        return "Unknown quest";
    }

    public virtual string GetSummary(QuestInProgress info)
    {
        return GetDetails(info);
    }

    public virtual string AchievementText(QuestInProgress info)
    {
        return "Quests";
    }

    public virtual string GetAspiration(QuestInProgress info)
    {
        return "they will be destroyed";
    }

    public virtual string GetCompletionDescription(QuestInProgress info)
    {
        return "???";
    }

    public QuestInProgress GenerateQuest(QuestInProgress questProto, Team clientTeam, bool secondary=false)
    {
        QuestInProgress result = questProto.Clone();

        result.secondary = secondary;
        result.seed = new ConsistentRandom().Next();
        result.clientTeam = clientTeam;
        result.expirationRound = GameController.instance.gameState.nround + timeExpected;

        InitQuest(clientTeam, result);

        return result;
    }

    public virtual void InitQuest(Team clientTeam, QuestInProgress questInProgress)
    {
    }

    public virtual void OnDungeonCleared(string dungeonGuid, QuestInProgress questInProgress)
    {
        if(dungeonGuid == questInProgress.dungeonGuid) {
            questInProgress.count++;
        }
    }

    public virtual void OnUnitArrivesAtLoc(Unit unit, QuestInProgress questInProgress)
    {
    }

    public virtual void OnEnemyUnitKilled(Unit unit, QuestInProgress questInProgress)
    {}

    public virtual void OnUnitLeveled(Unit unit, QuestInProgress questInProgress)
    { }

    public virtual void OnAcceptQuest(QuestInProgress questInProgress)
    {

    }

    public virtual void OnRequestQuest(QuestInProgress questInProgress)
    {

    }

    public virtual string QuestHint(QuestInProgress questInProgress)
    {
        return null;
    }


    public virtual List<DiplomacyNode> GetCustomCheckin(QuestInProgress questInProgress)
    {
        return checkinNode;
    }

    public virtual bool AlmostFailed(QuestInProgress questInProgress)
    {
        return false;
    }

    public virtual bool FailedDeclareWar(QuestInProgress questInProgress)
    {
        return false;
    }

    public virtual bool Completed(QuestInProgress questInProgress)
    {
        return questInProgress.count >= questInProgress.countNeeded;
    }
}

[System.Serializable]
public class QuestInProgress
{
    public int seed = 0;
    public int count = 0;
    public int countNeeded = 1;

    //secondary version of quest of this type.
    public bool secondary = false;

    public bool isValid {
        get {
            if(quest.questType == Quest.QuestType.War && enemyTeam == null) {
                return false;
            }

            return true;
        }
    }


    //Counters for if the player has made progress on this quest.
    //If progressEstimate >= progressEstimateMax the player has
    //made 'significant progress' on this quest.
    public int progressEstimate = 0;
    public int progressEstimateMax = 1;

    public int completedRound = -1;
    public int expirationRound = -1; //the round it must be done by for full credit.
    public int timeUntilExpired {
        get {
            return expirationRound - GameController.instance.gameState.nround;
        }
    }
    public Quest quest = null;
    public Team clientTeam = null;

    //Used in make war type quests.
    public Team enemyTeam = null;

    public List<UnitTag> unitTags = null;

    public Equipment itemToRetrieve = null;
    public Loc itemRetrieveLoc;

    //Used in foster quests.
    public string unitGuid;
    public string unitName;

    public Unit GetUnit()
    {
        return GameController.instance.GetUnitByGuid(unitGuid);
    }

    //have to clear dungeon with this guid.
    public string dungeonGuid;

    public bool completed {
        get {
            return quest.Completed(this);
        }
    }

    public QuestInProgress Clone()
    {
        return (QuestInProgress)MemberwiseClone();
    }
}