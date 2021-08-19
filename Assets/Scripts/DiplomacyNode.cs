using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum DiplomacyNodeCondition
{
    None,
    NotNext,
    PlayerHasContactWithQuestTargetTeam,
    PlayerAtWarWithQuestTargetTeam,
    HasQuest,
    PlayerIsSwornEnemy,
    PlayerHasDiplomacy,
}

[System.Serializable]
public class DiplomacyNodeInfo
{
    public Unit playerUnit;
    public Unit aiUnit;
    public DiplomacyNode node;

}

[System.Serializable]
public class DiplomacyNodeOption
{
    public string text;
    public DiplomacyNode result;
    public List<DiplomacyInstruction> instructions;

    public List<DiplomacyNodeCondition> conditions;
}

[CreateAssetMenu(menuName = "Wesnoth/DiplomacyNode")]
public class DiplomacyNode : GWScriptableObject
{
    [TextArea(5,5)]
    public string message;
    public List<DiplomacyNodeOption> options;

    public bool executeOnce = false;

    public bool executeOnceExpired {
        get {
            return executeOnce && GameController.instance.gameState.diplomacyNodesExecuted.Contains(this);
        }
    }

    public void RecordExecuteOnce()
    {
        if(executeOnce) {
            GameController.instance.gameState.diplomacyNodesExecuted.Add(this);
        }
    }

    public bool focusPlayer = false;

    public bool focusQuestEnemyTeam = false;


    public Unit FocusUnit(DiplomacyNodeInfo info)
    {
        return focusPlayer ? info.playerUnit : info.aiUnit;
    }

    public virtual Loc FocusLoc(DiplomacyNodeInfo info)
    {
        if(focusQuestEnemyTeam) {
            return info.aiUnit.teamInfo.currentQuest.enemyTeam.teamInfo.GetRuler().loc;
        }

        return FocusUnit(info).loc;
    }

    public virtual int RevealAroundFocusLoc(DiplomacyNodeInfo info)
    {
        if(focusQuestEnemyTeam) {
            return 2;
        }
        return 0;
    }

    public virtual string GetTitle(DiplomacyNodeInfo info)
    {
        string charName = FocusUnit(info).unitInfo.characterName;
        if(string.IsNullOrEmpty(charName) == false) {
            return charName;
        }

        return FocusUnit(info).unitInfo.unitType.description;
    }

    public virtual string GetMessage(DiplomacyNodeInfo info)
    {
        return CustomizeString(info, message);
    }

    public bool ConditionPasses(DiplomacyNodeInfo info, DiplomacyNodeCondition condition)
    {
        switch(condition) {
            case DiplomacyNodeCondition.PlayerHasContactWithQuestTargetTeam:
                if(info.aiUnit.teamInfo.currentQuest != null && info.aiUnit.teamInfo.currentQuest.enemyTeam != null) {
                    return info.aiUnit.teamInfo.currentQuest.enemyTeam.teamInfo.hasPlayerContact;
                }
                break;
            case DiplomacyNodeCondition.PlayerAtWarWithQuestTargetTeam:
                if(info.aiUnit.teamInfo.currentQuest != null && info.aiUnit.teamInfo.currentQuest.enemyTeam != null) {
                    return info.aiUnit.teamInfo.currentQuest.enemyTeam.teamInfo.enemyOfPlayer;
                }
                break;
            case DiplomacyNodeCondition.HasQuest:
                return info.aiUnit.teamInfo.currentQuest != null;
            case DiplomacyNodeCondition.PlayerIsSwornEnemy:
                return info.aiUnit.team.swornEnemies.Contains(info.playerUnit.team);
            case DiplomacyNodeCondition.PlayerHasDiplomacy:
                return info.playerUnit.teamInfo.diplomaticScore > 0;
        }

        return true;
    }

    public string GetOptionText(DiplomacyNodeInfo info, DiplomacyNodeOption option)
    {
        return CustomizeString(info, option.text);
    }

    public virtual List<DiplomacyNodeOption> GetOptions(DiplomacyNodeInfo info)
    {
        List<DiplomacyNodeOption> result = new List<DiplomacyNodeOption>();
        foreach(var option in options) {
            bool passes = true;
            bool conditionFailure = false;
            foreach(var cond in option.conditions) {
                if(cond == DiplomacyNodeCondition.NotNext) {
                    conditionFailure = true;
                    continue;
                }
                else if(ConditionPasses(info, cond) == conditionFailure) {
                    passes = false;
                    break;
                }

                conditionFailure = false;
            }

            if(passes) {
                result.Add(option);
            }
        }
        return result;
    }

    public virtual Sprite GetPrimarySprite(DiplomacyNodeInfo info)
    {
        if(focusQuestEnemyTeam) {
            return null;
        }
        return info.playerUnit.unitInfo.portrait;
    }

    public virtual Sprite GetSecondarySprite(DiplomacyNodeInfo info)
    {
        if(focusQuestEnemyTeam) {
            return null;
        }

        return info.aiUnit.unitInfo.portrait;
    }

    public virtual bool HighlightPrimarySprite(DiplomacyNodeInfo info)
    {
        return FocusUnit(info) == info.playerUnit;
    }


    static public string CustomizeString(DiplomacyNodeInfo info, string str)
    {
        Unit playerRuler = info.playerUnit.teamInfo.GetRuler();
        bool rulerFemale = playerRuler.unitInfo.gender == UnitGender.Female;
        bool rulerMale = !rulerFemale;

        string rulerTitle = rulerFemale ? "Queen" : "King";

        string result = str;

        if(info.playerUnit.unitInfo.ruler) {
            result = result.Replace("player_unit_introduction", string.Format("{0}, rightful {1} of Wesnoth", info.playerUnit.unitInfo.characterName, rulerTitle));
            result = result.Replace("player_unit_i_declare", string.Format("I, {0}, rightful {1} of Wesnoth", info.playerUnit.unitInfo.characterName, rulerTitle));
        } else {
            result = result.Replace("player_unit_introduction", string.Format("{0}, and I speak for {1}, rightful {2} of Wesnoth", info.playerUnit.unitInfo.characterName, playerRuler.unitInfo.characterName, rulerTitle));
            result = result.Replace("player_unit_i_declare", string.Format("In the name of {0}, rightful {1} of Wesnoth I", playerRuler.unitInfo.characterName, rulerTitle));
        }

        result = result.Replace("player_lisar", string.Format("{0}", playerRuler.unitInfo.characterName));

        result = result.Replace("player_king", rulerTitle);
        result = result.Replace("player_queen", rulerTitle);

        result = result.Replace("player_his", rulerMale ? "his" : "her");
        result = result.Replace("player_her", rulerMale ? "his" : "her");
        result = result.Replace("player_His", rulerMale ? "His" : "Her");
        result = result.Replace("player_Her", rulerMale ? "His" : "Her");
        result = result.Replace("player_your", info.playerUnit.unitInfo.ruler ? "your" : (rulerMale ? "his" : "her"));
        result = result.Replace("player_Your", info.playerUnit.unitInfo.ruler ? "Your" : (rulerMale ? "His" : "Her"));
        result = result.Replace("player_you", info.playerUnit.unitInfo.ruler ? "you" : (rulerMale ? "him" : "her"));
        result = result.Replace("player_You", info.playerUnit.unitInfo.ruler ? "You" : (rulerMale ? "Him" : "Her"));

        result = result.Replace("player_human", info.aiUnit.unitInfo.DescribeOtherUnitRace(info.playerUnit.unitInfo));
        result = result.Replace("ai_human", info.playerUnit.unitInfo.DescribeOtherUnitRace(info.aiUnit.unitInfo));

        result = result.Replace("player_he", rulerMale ? "he" : "she");
        result = result.Replace("player_He", rulerMale ? "He" : "She");

        result = result.Replace("npc_team", info.aiUnit.teamInfo.team.teamNameAsProperNoun);

        Unit npcRuler = info.aiUnit.teamInfo.GetRuler();

        string npcTeam = string.Format("{0}{1}", info.aiUnit.team.teamNameIsProperNoun ? "" : "the ", info.aiUnit.team.teamName);

        if(info.aiUnit.unitInfo.ruler) {
            result = result.Replace("npc_unit_introduction", string.Format("I am {0}, ruler of {1}", info.aiUnit.unitInfo.characterName, npcTeam));
        } else if(npcRuler != null) {
            result = result.Replace("npc_unit_introduction", string.Format("I serve {0}, ruler of {1}", npcRuler.unitInfo.characterName, npcTeam));
        }

        if(npcRuler == null && info.aiUnit.teamInfo.killedBy != null) {
            result = result.Replace("ruler_fate", string.Format("slain by {0}", info.aiUnit.teamInfo.killedBy.teamNameAsProperNoun));
        } else if(npcRuler == null) {
            result = result.Replace("ruler_fate", "slain");
        }

        var conspirators = info.aiUnit.teamInfo.teamsConspiredWithPlayerAgainst;
        if(conspirators.Count == 0) {
            conspirators = info.aiUnit.teamInfo.teamsPlayerRejectedConspiracy;
        }
        if(conspirators.Count > 0) {
            string conspiredTeams = "our enemies";

            if(conspirators.Count == 1) {
                conspiredTeams = conspirators[0].teamNameAsProperNoun;
            } else if(conspirators.Count == 2) {
                conspiredTeams = conspirators[0].teamNameAsProperNoun + " and " + conspirators[1].teamNameAsProperNoun;
            }

            result = result.Replace("our_enemies", conspiredTeams);
        }

        QuestInProgress quest = info.aiUnit.teamInfo.currentQuest;
        if(quest != null) {
            if(quest.enemyTeam) {
                result = result.Replace("quest_enemy_team_name", quest.enemyTeam.teamNameAsProperNoun);
                result = result.Replace("Quest_enemy_team_name", quest.enemyTeam.teamNameAsProperNounCap);

            }

            if(quest.itemRetrieveLoc.valid) {
                Tile t = GameController.instance.map.GetTile(quest.itemRetrieveLoc);
                if(string.IsNullOrEmpty(t.GetLabelText()) == false) {
                    result = result.Replace("quest_village_recapture_name", t.GetLabelText());
                }
            }

            result = result.Replace("quest_description", quest.quest.GetSummary(quest));
            result = result.Replace("quest_aspiration", quest.quest.GetAspiration(quest));
            result = result.Replace("quest_completion", quest.quest.GetCompletionDescription(quest));

            if(string.IsNullOrEmpty(quest.dungeonGuid) == false) {
                string dungeon_description = "monsters";
                var dungeon = GameController.instance.gameState.GetDungeon(quest.dungeonGuid);
                if(dungeon != null) {
                    dungeon_description = string.Format("<link=\"dungeon_description\">{0}</link>", dungeon.monsterDescription);
                }

                result = result.Replace("dungeon_description", dungeon_description);
            }
        }

        if(info.aiUnit.teamInfo.completedQuests.Count > 0) {
            quest = info.aiUnit.teamInfo.completedQuests[info.aiUnit.teamInfo.completedQuests.Count-1];
            result = result.Replace("quest_complete_description", quest.quest.GetCompletionDescription(quest));
        }

        if(result.Contains("unit_foster")) {
            UnitInfo unitFoster = info.aiUnit.team.GenerateUnitToFoster();
            result = result.Replace("unit_foster_name", unitFoster.characterName);
            result = result.Replace("unit_foster_him", unitFoster.gender == UnitGender.Female ? "her" : "him");
            result = result.Replace("unit_foster_he", unitFoster.gender == UnitGender.Female ? "she" : "he");
            result = result.Replace("unit_foster_He", unitFoster.gender == UnitGender.Female ? "She" : "He");
            result = result.Replace("unit_foster_lord", unitFoster.gender == UnitGender.Female ? "lady" : "lord");
        }

        int defaultRelationship = info.aiUnit.team.baseDifficultyRelationship + info.aiUnit.team.GetDefaultRelationship(info.playerUnit.team);
        int currentRelationship = info.aiUnit.teamInfo.relationsWithPlayerRating;

        string startingRelationshipDescription = info.playerUnit.team.DescribeDefaultRelationship(defaultRelationship);

        result = result.Replace("starting_relationship_description", startingRelationshipDescription);

        string[] relationshipContinuations = new string[] {
            "<link=\"relation_info\">what we have heard of you</link> has done little to change this.",
            "based upon <link=\"relation_info\">what we know</link>, we still foster this point of view.",
        };

        if(currentRelationship <= defaultRelationship-20 && currentRelationship <= 20) {
            relationshipContinuations = new string[] {
                "however <link=\"relation_info\">what we have heard of you</link> has greatly displeased us.",
                "based upon <link=\"relation_info\">what we know</link>, we find ourselves increasingly hostile toward you.",
            };
        } else if(currentRelationship <= defaultRelationship-20 && currentRelationship <= 30) {
            relationshipContinuations = new string[] {
                "however <link=\"relation_info\">what we have heard of you</link> has displeased us.",
                "based upon <link=\"relation_info\">what we know</link>, we find ourselves more hesitant about you.",
            };
        } else if(currentRelationship >= defaultRelationship+20 && currentRelationship >= 40 && defaultRelationship <= 30) {
            relationshipContinuations = new string[] {
                "however <link=\"relation_info\">what we have heard of you</link> has made us feel more charitably toward you.",
                "though <link=\"relation_info\">what we have heard about you</link> leaves us considering whether we may have misjudged you.",
            };
        } else if(currentRelationship >= defaultRelationship+20 && currentRelationship >= 60) {
            relationshipContinuations = new string[] {
                "I must say <link=\"relation_info\">what we have heard of you</link> has reinforced our positive feelings about you.",
                "and <link=\"relation_info\">what we have heard about you</link> has pleased us.",
            };
        }

        result = result.Replace("starting_relationship_delta", relationshipContinuations[GameController.instance.rng.Next()%relationshipContinuations.Length]);

        string[] aiRelationshipOffer = new string[] {
            "Befriending you sounds as though it may be dangerous.",
            "You must understand our hesistance to befriend one such as you.",
        };
        string[] playerPeacefulOffer = new string[] {
            "I am the rightful ruler. I would like your support.",
        };
        string[] playerHostileOffer = new string[] {
            "I have no interest in 'friends' such as you anyhow. As the rightful ruler I am determined to destroy you!",
        };

        if(currentRelationship >= 50) {
            aiRelationshipOffer = new string[] {
            "We are sympathetic to your claim, and find ourselves intrigued by you.",
            "The path will not be easy, but you may be the rightful ruler of this land.",
            };

            playerPeacefulOffer = new string[] {
            "I appreciate considering the possibility of supporting my claim to the throne. I hope we can be close allies.",
            };
            playerHostileOffer = new string[] {
            "You were foolish enough to think I might be sympathetic toward you? As ruler of the land I decree that you shall die!",
            };
        } else if(currentRelationship <= 20) {
            aiRelationshipOffer = new string[] {
            "We are more likely to stick your head on a pike than support your claim to the throne!",
            "You think that we might be friendly toward you? We see little chance of that!",
            };

            playerPeacefulOffer = new string[] {
            "I understand your hostility toward me. Is there anything I can do to prove to you that we may put past enmity aside and you can see me as your friend?",
            };
            playerHostileOffer = new string[] {
            "I do not want your support. We are natural enemies and you shall surely die!",
            };
        } else if(currentRelationship <= 30) {
            aiRelationshipOffer = new string[] {
            "We see little reason why we should be friends with you, let alone support your claim.",
            "You think that we might be friendly toward you? We see little chance of that!",
            };
            playerPeacefulOffer = new string[] {
            "I understand your hesitance in supporting my claim. Is there anything I can do to prove to you that I am the rightful ruler?",
            };
            playerHostileOffer = new string[] {
            "I do not want your support. We are natural enemies and you shall surely die!",
            };
        }

        result = result.Replace("starting_relationship_offer", aiRelationshipOffer[GameController.instance.rng.Next()%aiRelationshipOffer.Length]);
        result = result.Replace("player_peaceful_offer", playerPeacefulOffer[GameController.instance.rng.Next()%playerPeacefulOffer.Length]);
        result = result.Replace("player_hostile_offer", playerHostileOffer[GameController.instance.rng.Next()%playerHostileOffer.Length]);

        //describe relationship with player.
        //string playerRelationship = 

        return result;
    }
}
