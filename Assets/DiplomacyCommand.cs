using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class DiplomacyCommand : GameCommand
{
    static DiplomacyCommand _instance = null;

    [SerializeField]
    SpeechPromptInstance _fosterUnitSpeechPrompt = null;

    public DiplomacyNodeInfo info;

    TeamInfo playerteam {
        get {
            return info.playerUnit.teamInfo;
        }
    }

    TeamInfo aiTeam {
        get {
            return info.aiUnit.teamInfo;
        }
    }

    DiplomacyRegistry diplomacy {
        get {
            return info.aiUnit.teamInfo.team.diplomacy;
        }
    }


    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<DiplomacyNodeInfo>(data); }


    public static void SetNextNode(DiplomacyNode node)
    {
        if(_instance != null) {
            _instance.info.node = node;
        }
    }

    ConversationDialog.Info GetDialogInfo()
    {
        return GetDialogInfo(info.node);
    }

    ConversationDialog.Info GetDialogInfo(DiplomacyNode node)
    {
        ConversationDialog.Info dialogInfo = new ConversationDialog.Info() {
            title = node.GetTitle(info),
            text = node.GetMessage(info),
            primarySprite = node.GetPrimarySprite(info),
            secondarySprite = node.GetSecondarySprite(info),
            highlightPrimary = node.HighlightPrimarySprite(info),
            highlightSecondary = node.HighlightPrimarySprite(info) == false,
            options = new List<string>(),
        };

        dialogInfo.AddLink("relation_info", new TooltipText.Options() {
            useMousePosition = true,
            text = aiTeam.relationsWithPlayerTooltip,
            linkNormalColor = aiTeam.relationsWithPlayerDescription.color,
        });

        QuestInProgress quest = info.aiUnit.teamInfo.currentQuest;

        if(quest != null) {
            var dungeon = GameController.instance.gameState.GetDungeon(quest.dungeonGuid);
            if(dungeon != null) {
                dialogInfo.AddLink("dungeon_description", new TooltipText.Options() {
                    useMousePosition = true,
                    text = dungeon.monsterTooltip,
                    linkNormalColor = new Color(1f, 0.7f, 0.7f),
                });
            }
        }

        List<DiplomacyNodeOption> options = node.GetOptions(info);


        if(options.Count >= 2) {
            foreach(var option in options) {
                dialogInfo.options.Add(node.GetOptionText(info, option));
            }
        }

        return dialogInfo;
    }


    ConversationDialog.Result _dialogResult = null;

    IEnumerator ShowDialog(ConversationDialog.Info info)
    {
        _dialogResult = new ConversationDialog.Result();
        GameController.instance.ShowDialogMessage(info, _dialogResult);
        while(_dialogResult.finished == false) {
            yield return null;
        }
    }

    IEnumerator ShowNode(DiplomacyNode node)
    {
        node.RecordExecuteOnce();

        var dialogInfo = GetDialogInfo(node);

        Debug.Log("Show node: " + node.name);

        bool showDialog = true;

        while(showDialog) {

            showDialog = false;

            IEnumerator e = ShowDialog(dialogInfo);
            while(e.MoveNext()) {
                //var updatedDialogInfo = GetDialogInfo(node);
                //if(updatedDialogInfo.Equals(dialogInfo) == false) {
                //    dialogInfo = updatedDialogInfo;
                //    showDialog = true;
                //    GameController.instance.CloseConversationDialog();
                //}

                yield return e.Current;
            }
        }
    }

    IEnumerator FocusLoc(Loc focusLoc, int revealRadius=0)
    {
        if(focusLoc.valid == false) {
            yield break;
        }

        yield return GameController.instance.StartCoroutine(GameController.instance.ScrollCameraToCo(focusLoc, true, new Vector3(0f, -2f, 0f)));

        if(revealRadius >= 1) {
            List<Loc> reveal = Tile.GetTilesInRadius(focusLoc, revealRadius);
            List<Tile> revealTiles = new List<Tile>();
            foreach(Loc loc in reveal) {
                if(GameController.instance.map.LocOnBoard(loc)) {
                    GameController.instance.playerTeamInfo.mapRevealed.Add(loc);
                    revealTiles.Add(GameController.instance.map.GetTile(loc));
                }
            }

            GameController.instance.RecalculateVision(revealTiles);
        }
    }

    IEnumerator ExecuteNodes(List<DiplomacyNode> nodes)
    {
        _dialogResult = new ConversationDialog.Result();
        if(nodes == null) {
            yield break;
        }

        foreach(DiplomacyNode node in nodes) {
            yield return StartCoroutine(ShowNode(node));
        }
    }

    IEnumerator RunCo()
    {
        _instance = this;

        yield return StartCoroutine(RunCoInternal());

        _instance = null;
        finished = true;
    }

    IEnumerator RunCoInternal()
    {
        Debug.Log("Run diplomacy");
        if(aiTeam.RecordPlayerContact()) {
            //initial greeting.

            List<DiplomacyNode> nodes = diplomacy.initialGreeting;

            if(aiTeam.team.swornEnemies.Contains(playerteam.team)) {
                nodes = diplomacy.initialGreetingSwornEnemies;
                aiTeam.StartWarWithPlayer();
            } else if(aiTeam.enemyOfPlayer || aiTeam.wantsWarWithPlayer) {
                nodes = diplomacy.initialGreetingAlreadyWar;
                aiTeam.StartWarWithPlayer();
            } else if(aiTeam.GetRuler() == null) {
                //their leader is dead already.
                yield return StartCoroutine(LeaderDead(true));
                yield break;
            } else if(aiTeam.team.teamInfo.currentQuest != null && aiTeam.team.teamInfo.currentQuest.completed) {
                //A quest was previewed but it's been completed already.
                aiTeam.AddRelationsWithPlayerChange(aiTeam.team.teamInfo.currentQuest.quest.AchievementText(aiTeam.team.teamInfo.currentQuest), 20);

                aiTeam.questsCompleted++;
                nodes = diplomacy.initialGreetingQuestCompleted;

                aiTeam.currentQuest.completedRound = GameController.instance.gameState.nround;
                aiTeam.completedQuests.Add(aiTeam.currentQuest);
                aiTeam.currentQuests.Clear();

            } else if(aiTeam.giftsOwingToPlayer > 0) {
                nodes = diplomacy.initialGreetingPleased;
            }

            if(nodes == null || nodes.Count == 0 || nodes[0].executeOnceExpired) {
                yield break;
            }

            foreach(DiplomacyNode node in nodes) {
                yield return StartCoroutine(ShowNode(node));

                if(_dialogResult.optionChosen > 0) {
                    break;
                }
            }

            if(aiTeam.team.barbarian == false && aiTeam.team.primaryEnemy == false && aiTeam.enemyOfPlayer == false) {
                if(_dialogResult.optionChosen <= 0) {
                    yield return OfferQuest();
                }

                else if(_dialogResult.optionChosen > 0) {
                    //declare war.
                    aiTeam.SetRelations(Team.DiplomacyStatus.Hostile);
                    yield return StartCoroutine(ExecuteNodes(diplomacy.declareWar));
                }
            }

        } else {
            //Not the initial greeting.

            if(aiTeam.GetRuler() == null) {
                yield return StartCoroutine(LeaderDead(false));
                yield break;
            }

            List<DiplomacyNode> nodes = diplomacy.friendlyGreetingNoQuest;

            if(aiTeam.allyOfPlayer) {
                nodes = diplomacy.allyGreeting;
            } else if(aiTeam.relationsWithPlayerRating <= 0) {
                nodes = diplomacy.declareWar;
            } else if(aiTeam.currentQuest != null) {

                if(aiTeam.currentQuest.quest.questType == Quest.QuestType.PersonalVisit && info.aiUnit.unitInfo.ruler && info.playerUnit.unitInfo.ruler) {
                    aiTeam.currentQuest.count++; //mark quest as complete.
                } else if(aiTeam.currentQuest.quest.questType == Quest.QuestType.RetrieveItem && info.playerUnit.unitInfo.equipment.Contains(aiTeam.currentQuest.itemToRetrieve)) {
                    aiTeam.currentQuest.count++;
                }

                if(aiTeam.currentQuest.completed) {
                    aiTeam.questsCompleted++;

                    aiTeam.AddRelationsWithPlayerChange(aiTeam.team.teamInfo.currentQuest.quest.AchievementText(aiTeam.team.teamInfo.currentQuest), 20);

                    if(aiTeam.currentQuest.quest.completedNode.Count > 0) {
                        nodes = aiTeam.currentQuest.quest.completedNode;
                    } else {
                        nodes = diplomacy.questCompleted;
                    }
                } else if(aiTeam.currentQuest.quest.FailedDeclareWar(aiTeam.currentQuest)) {
                    nodes = new List<DiplomacyNode>(aiTeam.currentQuest.quest.failedNode);
                    aiTeam.SetRelations(Team.DiplomacyStatus.Hostile);
                } else {
                    var questNodes = aiTeam.currentQuest.quest.GetCustomCheckin(aiTeam.currentQuest);
                    if(questNodes != null && questNodes.Count > 0) {
                        nodes = questNodes;
                        if(aiTeam.currentQuest.quest.AlmostFailed(aiTeam.currentQuest)) {
                            aiTeam.warnedPlayerAboutQuest = GameController.instance.gameState.nround;
                        }
                    } else {
                        nodes = diplomacy.friendlyGreetingHasQuest;
                    }
                }
            }

            yield return StartCoroutine(ExecuteNodes(nodes));

            if(aiTeam.enemyOfPlayer) {
                //break off dialog since the custom quest nodes declared war.
                yield break;
            }

            if(_dialogResult.optionChosen > 0) {
                //declare war.
                aiTeam.SetRelations(Team.DiplomacyStatus.Hostile);
                yield return StartCoroutine(ExecuteNodes(diplomacy.declareWar));
            } else {
                yield return StartCoroutine(OfferGift());

                if(aiTeam.currentQuest != null && aiTeam.currentQuest.completed) {
                    aiTeam.currentQuest.completedRound = GameController.instance.gameState.nround;
                    aiTeam.completedQuests.Add(aiTeam.currentQuest);
                    aiTeam.currentQuests.Clear();

                    if(aiTeam.allyOfPlayer) {
                        yield return StartCoroutine(ExecuteNodes(diplomacy.friendlyFarewell));
                    } else if(aiTeam.wantsAllianceWithPlayer) {
                        yield return OfferAlliance();
                    } else {
                        yield return OfferQuest();
                    }
                } else {
                    if(aiTeam.wantsAllianceWithPlayer) {
                        yield return OfferAlliance();
                    } else {
                        yield return StartCoroutine(ExecuteNodes(diplomacy.friendlyFarewell));
                    }
                }
            }
        }
    }

    IEnumerator LeaderDead(bool initialGreeting=false)
    {
        if(initialGreeting) {
            var nodes = diplomacy.initialGreetingLeaderDead;
            yield return StartCoroutine(ExecuteNodes(nodes));
        }

        if(playerteam.gold >= 8) {
            yield return StartCoroutine(ExecuteNodes(diplomacy.offerJoinLeaderDead));
            if(_dialogResult.optionChosen <= 0) {
                info.aiUnit.SetTeam(info.playerUnit.unitInfo.nteam);
                info.playerUnit.teamInfo.gold -= 8;

                yield return StartCoroutine(ShowDialog(new ConversationDialog.Info() {
                    title = "A new recruit",
                    text = string.Format("You have convinced this unit to join you!"),
                }));
            }
        } else {
            yield return StartCoroutine(ExecuteNodes(diplomacy.talkLeaderDead));
        }
    }

    IEnumerator OfferAlliance()
    {
        yield return StartCoroutine(ExecuteNodes(diplomacy.offerFealty));

        aiTeam.playerDiplomacyStatus = Team.DiplomacyStatus.Ally;
        GameController.instance.RecalculateVision();

        yield return StartCoroutine(ShowDialog(new ConversationDialog.Info() {
            title = "Fealty",
            text = string.Format("{0} have sworn fealty to you and are now your vassals. You share vision with them, may recruit from their castle, and may rest your units in their villages. They will support your claim to the throne.", aiTeam.team.teamNameAsProperNounCap),
        }));
    }

    IEnumerator OfferQuest()
    {
        var quest = AssignQuest();
        QuestInProgress primaryQuest = quest;
        QuestInProgress secondaryQuest = null;
        if(quest != null) {

            bool newQuest = true;

            while(newQuest) {
                newQuest = false;

                quest.quest.OnRequestQuest(quest);

                switch(quest.quest.questType) {
                    case Quest.QuestType.RecoverVillage:
                        yield return StartCoroutine(QuestRecoverVillage(quest));
                        break;
                    case Quest.QuestType.RetrieveItem:
                        yield return StartCoroutine(QuestRetrieveItemOffer(quest));
                        break;
                    case Quest.QuestType.LevelUpRuler:
                        yield return StartCoroutine(QuestLevelUp(quest));
                        break;
                    case Quest.QuestType.FosterUnit:
                        yield return StartCoroutine(QuestFoster(quest));
                        break;
                    case Quest.QuestType.War:
                        yield return StartCoroutine(QuestWar(quest));
                        break;
                    case Quest.QuestType.PersonalVisit:
                        yield return StartCoroutine(QuestVisit(quest));
                        break;
                    case Quest.QuestType.ClearDungeon:
                        yield return StartCoroutine(QuestDungeon(quest));
                        break;
                    default:
                        Debug.LogError("Unknown quest type: " + quest.quest.questType);
                        break;
                }

                if(_dialogResult.optionChosen == 2) {
                    newQuest = true;

                    if(secondaryQuest == null) {
                        secondaryQuest = AssignQuest(true);
                        if(secondaryQuest == null) {
                            secondaryQuest = primaryQuest;
                        }
                    }

                    if(quest == primaryQuest) {
                        quest = secondaryQuest;
                    } else {
                        quest = primaryQuest;
                    }

                    if(quest != null) {
                        aiTeam.team.teamInfo.currentQuests.Clear();
                        aiTeam.team.teamInfo.currentQuests.Add(quest);
                    }
                }
            }

            bool questAccepted = _dialogResult.optionChosen <= 0;
            if(questAccepted) {
                quest.quest.OnAcceptQuest(quest);

                yield return StartCoroutine(OfferGift());

                if(quest.quest.acceptedNode.Count > 0) {
                    yield return StartCoroutine(ExecuteNodes(quest.quest.acceptedNode));
                } else if(_dialogResult.optionChosen == 0) {
                    yield return StartCoroutine(ExecuteNodes(diplomacy.peacefulAgreement));
                } else {
                    yield return StartCoroutine(ExecuteNodes(diplomacy.friendlyFarewell));
                }
            } else {
                //declare war.
                aiTeam.SetRelations(Team.DiplomacyStatus.Hostile);

                if(quest.quest.rejectedNode.Count > 0) {
                    yield return StartCoroutine(ExecuteNodes(quest.quest.rejectedNode));
                } else {
                    yield return StartCoroutine(ExecuteNodes(diplomacy.declareWar));
                }
            }
        } else {
            //if we couldn't find a quest to offer, offer the player fealty.
            yield return StartCoroutine(OfferAlliance());
        }
    }

    public List<Equipment> GetGiftCandidates(int tier)
    {
        List<Equipment> result = new List<Equipment>();
        foreach(Equipment equip in Equipment.all) {
            if(equip.tier == tier && info.playerUnit.unitInfo.equipment.Contains(equip) == false && info.playerUnit.teamInfo.equipmentInMarket.Contains(equip) == false) {
                result.Add(equip);
            }
        }

        List<Equipment> teamAssociatedResults = new List<Equipment>();
        foreach(Equipment equip in result) {
            if(equip.teamAssociation.Contains(aiTeam.team)) {
                teamAssociatedResults.Add(equip);
            }
        }

        if(teamAssociatedResults.Count > 0) {
            return teamAssociatedResults;
        }

        //Team has no team-specific equipment so just return any unaffiliated equipment.
        foreach(Equipment equip in result) {
            if(equip.teamAssociation.Count == 0) {
                teamAssociatedResults.Add(equip);
            }
        }

        return teamAssociatedResults;
    }


    IEnumerator OfferGift()
    {
        if(aiTeam.giftsOwingToPlayer <= 0) {
            yield break;
        }

        var nodes = diplomacy.offerGift;

        if(aiTeam.numConspiraciesThankedPlayerFor < aiTeam.teamsPlayerRejectedConspiracy.Count) {
            nodes = diplomacy.offerGiftForRejectingEnemies;
        }

        aiTeam.numConspiraciesThankedPlayerFor = aiTeam.teamsPlayerRejectedConspiracy.Count;

        yield return StartCoroutine(ExecuteNodes(nodes));

        while(aiTeam.giftsOwingToPlayer > 0) {
            aiTeam.numGiftsGivenToPlayer++;

            var candidates = GetGiftCandidates(2);
            if(candidates == null || candidates.Count == 0) {
                continue;
            }

            var equip = candidates[GameController.instance.rng.Next(candidates.Count)];

            string convoyMessage = "";

            if(equip.EquippableForUnit(info.playerUnit.unitInfo) == false) {
                convoyMessage += " This unit cannot carry it, so it has been placed in the convoy. Equip it on a unit by shopping with them while in a castle you control.";
            }

            Material equipMaterial = equip.CreateMaterial();

            string tooltipText = equip.FullTooltip();
            yield return StartCoroutine(ShowDialog(new ConversationDialog.Info() {
                title = "Gift",
                text = string.Format("You have received a <color=#ccccff><link=\"equip\">{0}</link></color>." + convoyMessage, equip.description),
            }.AddLink("equip",
                new TooltipText.Options() {
                    text = tooltipText,
                    icon = equip.icon,
                    iconSize = new Vector2(64f, 64f),
                    iconMaterial = equipMaterial,
                    useMousePosition = true,
                }
            )));

            info.playerUnit.GiveUnitEquipment(equip);
        }
    }

    IEnumerator QuestDungeon(QuestInProgress quest)
    {
        QuestClearDungeon q = (QuestClearDungeon)quest.quest;

        yield return StartCoroutine(FocusLoc(q.GetDungeon(quest).entryLoc, 2));
        yield return StartCoroutine(ShowNode(q.offerNode));
    }

    IEnumerator QuestVisit(QuestInProgress quest)
    {
        QuestPersonalVisit q = (QuestPersonalVisit)quest.quest;

        yield return StartCoroutine(FocusLoc(quest.clientTeam.teamInfo.keepLoc, 2));
        yield return StartCoroutine(ShowNode(q.offerNode));
    }

    IEnumerator QuestRecoverVillage(QuestInProgress quest)
    {
        QuestRecoverVillage q = (QuestRecoverVillage)quest.quest;

        yield return StartCoroutine(FocusLoc(quest.itemRetrieveLoc, 2));
        GameController.instance.map.GetTile(quest.itemRetrieveLoc).RevealLabel();
        yield return StartCoroutine(ShowNode(q.offerNode));

    }

    IEnumerator QuestRetrieveItemOffer(QuestInProgress quest)
    {
        QuestRetrieveItem q = (QuestRetrieveItem)quest.quest;

        yield return StartCoroutine(FocusLoc(quest.itemRetrieveLoc, 2));
        yield return StartCoroutine(ShowNode(q.offerNode));

    }

    IEnumerator QuestLevelUp(QuestInProgress quest)
    {
        QuestLevelUpLeader q = (QuestLevelUpLeader)quest.quest;

        yield return StartCoroutine(ShowNode(q.offerNode));
    }

    IEnumerator QuestFoster(QuestInProgress quest)
    {
        QuestFosterUnit q = (QuestFosterUnit)quest.quest;

        yield return StartCoroutine(ShowNode(q.offerNode));

        bool questAccepted = _dialogResult.optionChosen <= 0;
        if(questAccepted && aiTeam.currentQuest != null) {
            UnitInfo unitInfo = aiTeam.team.GenerateUnitToFoster();
            unitInfo.loc = GameController.instance.FindVacantTileNear(info.playerUnit.loc, unitInfo);
            unitInfo.nteam = GameController.instance.numPlayerTeam;
            unitInfo.originTeam = aiTeam.team;
            GameController.instance.ExecuteSpawnUnit(unitInfo);

            quest.unitGuid = unitInfo.guid;
            quest.unitName = unitInfo.characterName;

            //Queue the unit introducing itself.
            //GameController.instance.ExecuteSpeechPrompt(fosterUnit, _fosterUnitSpeechPrompt.Clone());
        }
    }

    IEnumerator QuestWar(QuestInProgress quest)
    {
        QuestMakeWar q = (QuestMakeWar)quest.quest;
        DiplomacyNode node = null;
        if(quest.enemyTeam.teamInfo.enemyOfPlayer) {
            node = q.dialogAlreadyFighting;
        } else if(quest.enemyTeam.teamInfo.hasPlayerContact) {
            node = q.dialogHasContact;
        } else {
            node = q.dialogNoContact;
        }

        yield return StartCoroutine(FocusLoc(quest.enemyTeam.teamInfo.keepLoc, 2));
        yield return StartCoroutine(ShowNode(node));
    }

    public QuestInProgress AssignQuest(bool secondary=false)
    {
        QuestInProgress quest = AssignQuest(aiTeam.team, secondary);

        if(quest != null && aiTeam.team.teamInfo.currentQuests.Count == 0) {
            aiTeam.team.teamInfo.currentQuests.Add(quest);
        }

        return quest;
    }

    static public QuestInProgress AssignQuest(Team team, bool secondary=false)
    {
        QuestInProgress banQuest = null;
        if(secondary) {
            banQuest = AssignQuest(team, false);
        }

        if(secondary == false && team.teamInfo.currentQuest != null) {
            return team.teamInfo.currentQuest;
        }

        float bestScore = 0f;

        Debug.Log("POSSIBLE QUESTS: " + team.possibleQuests.Count);

        List<QuestInProgress> quests = new List<QuestInProgress>();
        foreach(QuestInProgress quest in team.possibleQuests) {

            if(banQuest != null && quest.quest == banQuest.quest && quest.quest.hasSecondary == false) {
                Debug.Log("QUEST BANNED... " + quest.quest.ToString());
                continue;
            }

            if(quest.quest == null) {
                //If we hit this case it means a team has a bad quest listed.
                Debug.LogError("Bad quest found for team: " + team.factionDescription);
                continue;
            }

            if(quest.quest.CanAssignQuest(team) == false) {
                Debug.Log("QUEST NOT ELIGIBLE... " + quest.quest.ToString());

                continue;
            }

            var generatedQuest = quest.quest.GenerateQuest(quest, team, secondary);

            if(generatedQuest == null || generatedQuest.isValid == false) {
                Debug.Log("QUEST NOT VALID... " + quest.quest.ToString());

                continue;
            }

            float score = quest.quest.ScoreQuest(generatedQuest) + quest.quest.debugBonusScore;
            if(banQuest != null && quest.quest == banQuest.quest) {
                score *= 0.5f;
            }

            Debug.LogFormat("Score quest: {0} -> {1}", quest.quest.GetSummary(generatedQuest), score);

            if(score <= 0f) {
                continue;
            }

            if(score >= bestScore) {
                if(score > bestScore) {
                    quests.Clear();
                    bestScore = score;
                }
                quests.Add(quest);
                Debug.Log("Found candidate quest");
            } else {
                Debug.Log("Quest not eligible");
            }
        }

        Debug.Log("Possible quests: " + quests.Count);

        if(quests.Count > 0) {
            QuestInProgress chosenQuest = quests[GameController.instance.rng.Range(0, quests.Count)];
            chosenQuest = chosenQuest.quest.GenerateQuest(chosenQuest, team);
            return chosenQuest;
            //return chosenQuest.quest.offerQuestNode;

            //GameController.instance.OfferQuest(chosenQuest.quest.GenerateQuest(chosenQuest, team));
            //teamInfo.RecordPlayerRequest();
        }

        return null;
    }

    private void Start()
    {
        StartCoroutine(RunCo());
    }
}
