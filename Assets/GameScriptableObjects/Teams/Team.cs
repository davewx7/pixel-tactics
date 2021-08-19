using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ScoreInfo
{
    public int tilesDiscovered = 0;
    public int dungeonsLooted = 0;
    public int villagesCaptured = 0;
    public int goldEarned = 0;
    public int kindness = 0;
    public List<int> enemiesKilledByLevel = new List<int>();
    public List<int> levelUpsByLevel = new List<int>();

    public int totalScore {
        get {
            int result = 0;
            foreach(ScoreField field in GetScores()) {
                result += field.score;
            }

            return result;
        }
    }

    public List<ScoreField> GetScores()
    {
        List<ScoreField> result = new List<ScoreField>();

        result.Add(new ScoreField() {
            description = "Tiles Discovered",
            count = tilesDiscovered,
            multiplier = 1,
            divider = 6,
        });

        result.Add(new ScoreField() {
            description = "Gold Earned",
            count = goldEarned,
            multiplier = 1,
        });

        result.Add(new ScoreField() {
            description = "Villages Captured",
            count = villagesCaptured,
            multiplier = 10,
        });

        result.Add(new ScoreField() {
            description = "Enemies Killed",
            count = -1,
            multiplier = -1,
        });

        while(enemiesKilledByLevel.Count < 4) {
            enemiesKilledByLevel.Add(0);
        }

        int mult = 5;
        for(int i = 0; i < enemiesKilledByLevel.Count; ++i) {
            result.Add(new ScoreField() {
                description = "    ......Level " + i,
                count = enemiesKilledByLevel[i],
                multiplier = mult,
            });

            mult *= 2;
        }

        while(levelUpsByLevel.Count < 4) {
            levelUpsByLevel.Add(0);
        }


        result.Add(new ScoreField() {
            description = "Units Leveled",
            count = -1,
            multiplier = -1,
        });

        mult = 50;
        for(int i = 2; i < levelUpsByLevel.Count; ++i) {
            result.Add(new ScoreField() {
                description = "    ......Level " + i,
                count = levelUpsByLevel[i],
                multiplier = mult,
            });

            mult *= 2;
        }

        result.Add(new ScoreField() {
            description = "Dungeons Looted",
            count = dungeonsLooted,
            multiplier = 200,
        });

        result.Add(new ScoreField() {
            description = "Kindness to your people",
            count = kindness,
            multiplier = 1,
        });


        return result;
    }

    public void RecordLevelUp(int level)
    {
        while(levelUpsByLevel.Count <= level) {
            levelUpsByLevel.Add(0);
        }

        levelUpsByLevel[level]++;
    }

    public void RecordEnemyKilled(int level)
    {
        while(enemiesKilledByLevel.Count <= level) {
            enemiesKilledByLevel.Add(0);
        }

        enemiesKilledByLevel[level]++;
    }

}

[System.Serializable]
public class TeamInfo
{
    public TeamInfo()
    {
        //Constructor used for serialization.
        team = null;
    }

    public TeamInfo(Team t)
    {
        team = t;
        if(t.startingGold >= 0) {
            gold = t.startingGold;
        }

        if(t.baseIncome >= 0) {
            baseIncome = t.baseIncome;
        }
    }

    public ScoreInfo scoreInfo = new ScoreInfo();

    public Team team;

    public Unit GetRuler()
    {
        foreach(Unit unit in GameController.instance.units) {
            if(unit.team == team && unit.unitInfo.ruler) {
                return unit;
            }
        }

        return null;
    }

    public List<Unit> GetUnits()
    {
        List<Unit> result = new List<Unit>();
        foreach(Unit unit in GameController.instance.units) {
            if(unit.team == team) {
                result.Add(unit);
            }
        }

        return result;
    }

    public AIState aiState {
        get {
            int index = 0;
            foreach(TeamInfo teamInfo in GameController.instance.teams) {
                if(teamInfo == this) {
                    if(GameController.instance.aiStates.Count > index) {
                        return GameController.instance.aiStates[index];
                    }

                    return null;
                }

                ++index;
            }

            return null;
        }
    }

    public void SetRelations(Team.DiplomacyStatus status)
    {
        if(status == _playerDiplomacyStatus) {
            return;
        }

        if(status == Team.DiplomacyStatus.Hostile) {
            StartWarWithPlayer();
        } else {
            _playerDiplomacyStatus = status;
        }
    }

    public void StartWarWithPlayer()
    {
        currentQuests.Clear();

        if(_playerDiplomacyStatus == Team.DiplomacyStatus.Hostile) {
            return;
        }

        _playerDiplomacyStatus = Team.DiplomacyStatus.Hostile;
        if(team.ai != null) {
            team.ai.WarWithPlayerStarted(aiState);
        }
    }

    //which team killed this team's leader?
    public Team killedBy = null;

    public Loc lastRecruitLoc;

    public List<UnitInfo> availableRecruits = new List<UnitInfo>();
    public bool availableRecruitsInit = false;

    public class RelationsWithPlayerEntry
    {
        public string reason;
        public int rating;
        public int nround = -1;
    }

    public List<RelationsWithPlayerEntry> relationsWithPlayerChanges = new List<RelationsWithPlayerEntry>();

    public List<RelationsWithPlayerEntry> relationsWithPlayerEntries {
        get {
            var result = new List<RelationsWithPlayerEntry>();

            foreach(var delta in relationsWithPlayerChanges) {
                if(GameController.instance.gameState.nround >= delta.nround) {
                    result.Add(delta);
                }
            }

            if(teamsPlayerRejectedConspiracy.Count > 0) {
                result.Add(new RelationsWithPlayerEntry() {
                    reason = "Showing loyalty",
                    rating = 5*teamsPlayerRejectedConspiracy.Count,
                });
            }

            foreach(TeamInfo teamInfo in GameController.instance.teams) {
                if(teamInfo != this && teamInfo.team.player == false && teamInfo.team.primaryEnemy == false && teamInfo.team.barbarian == false) {
                    if(team.IsAlly(teamInfo.team)) {
                        if(teamInfo.enemyOfPlayer) {
                            result.Add(new RelationsWithPlayerEntry() {
                                reason = string.Format("Fighting our friends, {0}", teamInfo.team.teamNameAsProperNoun),
                                rating = -5,
                            });
                        }
                    } else if(team.IsEnemy(teamInfo.team)) {
                        if(teamInfo.enemyOfPlayer) {
                            result.Add(new RelationsWithPlayerEntry() {
                                reason = string.Format("Fighting our enemies, {0}", teamInfo.team.teamNameAsProperNoun),
                                rating = 5,
                            });
                        }

                        if(teamInfo.allyOfPlayer) {
                            result.Add(new RelationsWithPlayerEntry() {
                                reason = string.Format("Friendly with our enemies, {0}", teamInfo.team.teamNameAsProperNoun),
                                rating = -20,
                            });
                        }
                    }
                }
            }

            return result;
        }
    }

    public struct RelationWithPlayerDescription
    {
        public Color color;
        public string text;
        public int rating;
    }

    public RelationWithPlayerDescription relationsWithPlayerDescription {
        get {
            int rating = relationsWithPlayerRating;
            if(rating <= 0) {
                return new RelationWithPlayerDescription() {
                    text = "Hostile",
                    color = new Color(1.0f, 0.4f, 0.4f),
                    rating = rating,
                };
            } else if(rating <= 20) {
                return new RelationWithPlayerDescription() {
                    text = "Displeased",
                    color = new Color(1.0f, 0.6f, 0.6f),
                    rating = rating,
                };
            } else if(rating <= 40) {
                return new RelationWithPlayerDescription() {
                    text = "Cautious",
                    color = new Color(1.0f, 1.0f, 0.6f),
                    rating = rating,
                };
            } else if(rating <= 60) {
                return new RelationWithPlayerDescription() {
                    text = "Indifferent",
                    color = new Color(0.6f, 1.0f, 0.6f),
                    rating = rating,
                };
            } else if(rating <= 80) {
                return new RelationWithPlayerDescription() {
                    text = "Sympathetic",
                    color = new Color(0.6f, 1.0f, 0.6f),
                    rating = rating,
                };
            } else if(rating < 100) {
                return new RelationWithPlayerDescription() {
                    text = "Friendly",
                    color = new Color(0.6f, 1.0f, 0.6f),
                    rating = rating,
                };
            } else {
                return new RelationWithPlayerDescription() {
                    text = "Allied",
                    color = new Color(0.6f, 1.0f, 0.6f),
                    rating = rating,
                };
            }

        }
    }

    public string relationsWithPlayerTooltip {
        get {
            var info = relationsWithPlayerDescription;

            Team playerTeam = GameController.instance.playerTeam;
            int defaultRelationship = team.GetDefaultRelationship(playerTeam);

            string[] difficultyNames = new string[] { "Casual", "Challenging", "Hardcore" };

            string result = string.Format("Base Attitude ({0}): {1}\n", difficultyNames[GameController.instance.gameState.difficulty], team.baseDifficultyRelationship);

            if(defaultRelationship < 0) {
                result += string.Format("<color=#ffaaaa>{0} dislike for {1}: {2}</color>\n", team.teamNameAsProperNounCap, playerTeam.teamName, defaultRelationship);
            } else if(defaultRelationship == 0) {
                result += string.Format("<color=#aaaaaa>{0} neutrality toward {1}: 0</color>\n", team.teamNameAsProperNounCap, playerTeam.teamName);
            } else {
                result += string.Format("<color=#aaffaa>{0} fondness toward {1}: +{2}</color>\n", team.teamNameAsProperNounCap, playerTeam.teamName, defaultRelationship);
            }

            foreach(var entry in relationsWithPlayerEntries) {
                result += string.Format("<color={3}>{0}: {1}{2}</color>\n", entry.reason, entry.rating >= 0 ? "+" : "", entry.rating, entry.rating >= 0 ? "#aaffaa" : "#ffaaaa");
            }

            result += string.Format("<b><color=#{0}>Current Attitude: {1} ({2})</color></b>", ColorUtility.ToHtmlStringRGB(info.color), info.rating, info.text);

            result += "\n\nIf attitude rises to 100 they will support your claim.\nIf attitude drops to 0 they will become hostile to you.";

            return result;
        }
    }

    public void AddRelationsWithPlayerChange(string reason, int amount)
    {
        foreach(var entry in relationsWithPlayerEntries) {
            if(entry.reason == reason) {
                entry.rating += amount;
                return;
            }
        }

        relationsWithPlayerChanges.Add(new RelationsWithPlayerEntry() {
            reason = reason,
            rating = amount,
        });
    }

    public int relationsWithPlayerRating {
        get {
            Team playerTeam = GameController.instance.playerTeam;
            int result = team.baseDifficultyRelationship;
            result += team.GetDefaultRelationship(playerTeam);
            foreach(var entry in relationsWithPlayerEntries) {
                result += entry.rating;
            }

            return result;
        }
    }

    [SerializeField]
    Team.DiplomacyStatus _playerDiplomacyStatus = Team.DiplomacyStatus.Peaceful;

    public Team.DiplomacyStatus playerDiplomacyStatus {
        get {
            if(team.player) {
                return Team.DiplomacyStatus.Ally;
            }

            if(team.primaryEnemy || team.barbarian) {
                return Team.DiplomacyStatus.Hostile;
            }

            return _playerDiplomacyStatus;
        }
        set {
            _playerDiplomacyStatus = value;
        }
    }

    public bool enemyOfPlayer {
        get {
            return _playerDiplomacyStatus == Team.DiplomacyStatus.Hostile || team.barbarian || team.primaryEnemy;
        }
    }

    public bool allyOfPlayer {
        get {
            return _playerDiplomacyStatus == Team.DiplomacyStatus.Ally;
        }
    }

    public void DeclareWarOn(TeamInfo otherTeam)
    {
        if(addedEnemies.Contains(otherTeam.team) == false) {
            addedEnemies.Add(otherTeam.team);
        }

        if(otherTeam.addedEnemies.Contains(team) == false) {
            otherTeam.addedEnemies.Add(team);
        }
    }

    public List<Team> addedEnemies = new List<Team>();

    public List<Team> enemies {
        get {
            List<Team> result = new List<Team>(team.swornEnemies);
            if(enemyOfPlayer) {
                result.Add(GameController.instance.playerTeam);
            }

            foreach(Team t in addedEnemies) {
                result.Add(t);
            }

            return result;
        }
    }

    public List<ScoreField> storyScoreEvents = new List<ScoreField>();
    public void AddStoryScore(string description, int amount)
    {
        foreach(ScoreField score in storyScoreEvents) {
            if(score.description == description && score.multiplier == amount) {
                score.count++;
                return;
            }
        }

        storyScoreEvents.Add(new ScoreField() {
            description = description,
            count = 1,
            multiplier = amount,
        });
    }

    public List<Equipment> equipmentStored = new List<Equipment>();

    [System.Serializable]
    public class MarketInfo
    {
        public List<Equipment> equipment = new List<Equipment>();
        public int priceMultiplier = 100;
        public string info = "";
    }

    [SerializeField]
    Dictionary<Loc, MarketInfo> _temporaryMarkets = new Dictionary<Loc, MarketInfo>();

    public void AddTemporaryMarket(Loc loc, MarketInfo info)
    {
        _temporaryMarkets[loc] = info;
    }

    public MarketInfo GetTemporaryMarket(Loc loc)
    {
        if(_temporaryMarkets.ContainsKey(loc)) {
            return _temporaryMarkets[loc];
        }

        return null;
    }

    public bool HasTemporaryMarket(Loc loc)
    {
        return _temporaryMarkets.ContainsKey(loc);
    }

    public List<UnitType> recruitTypes {
        get {
            List<UnitType> result = new List<UnitType>();
            foreach(UnitType t in team.recruitmentOptions) {
                result.Add(t);
            }

            foreach(var building in buildingsCompleted) {
                foreach(UnitType t in building.unitRecruits) {
                    if(result.Contains(t) == false) {
                        result.Add(t);
                    }
                }
            }

            return result;
        }
    }

    public MarketInfo GetMarketUnitHasAccessTo(Unit unit)
    {
        if(GameController.instance.debugAllItemsAvailable) {
            return new MarketInfo() {
                equipment = Equipment.all,
            };
        }

        int priceMultiplier = 100;
        MarketInfo market = null;
        bool inVillageOrCastle = unit.inCastleWeOwn || unit.tile.terrain.rules.village;
        if(inVillageOrCastle) {
            market = new MarketInfo() {
                equipment = equipmentInMarket,
            };
        }

        if(_temporaryMarkets.ContainsKey(unit.loc)) {
            var tmpMarket = _temporaryMarkets[unit.loc];
            if(tmpMarket.priceMultiplier < priceMultiplier) {
                priceMultiplier = tmpMarket.priceMultiplier;
            }
            if(market == null) {
                market = new MarketInfo();
            }
            foreach(var equip in tmpMarket.equipment) {
                market.equipment.Add(equip);
            }
        }

        if(equipmentStored.Count > 0 && inVillageOrCastle) {
            if(market == null) {
                market = new MarketInfo();
            }

            foreach(var equip in equipmentStored) {
                if(market.equipment.Contains(equip) == false) {
                    market.equipment.Add(equip);
                }
            }
        }

        if(market != null) {
            market.priceMultiplier = priceMultiplier;
        }

        if(market != null && market.equipment.Count == 0) {
            return null;
        }

        return market;
    }

    public List<Equipment> equipmentInMarket {
        get {
            List<Equipment> result = new List<Equipment>();
            foreach(EconomyBuilding building in buildingsCompleted) {
                foreach(var equip in building.equipmentInMarket) {
                    if(result.Contains(equip) == false) {
                        result.Add(equip);
                    }
                }
            }

            return result;
        }
    }

    public List<Equipment> consumablesInMarket {
        get {
            List<Equipment> result = new List<Equipment>();
            foreach(EconomyBuilding building in buildingsCompleted) {
                foreach(var item in building.equipmentInMarket) {
                    if(item.consumable && result.Contains(item) == false) {
                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }

    public bool doubleXpHighLevelUnits {
        get {
            foreach(EconomyBuilding building in buildingsCompleted) {
                if(building.doubleXpHighLevelUnits) {
                    return true;
                }
            }

            return false;
        }
    }

    public int diplomaticScore {
        get {
            int result = 0;
            foreach(EconomyBuilding building in buildingsCompleted) {
                result += building.increaseDiplomacy;
            }

            return result;
        }
    }

    public int villageHealAmount {
        get {
            int result = 4;
            foreach(EconomyBuilding building in buildingsCompleted) {
                if(building.villageHealAmount > result) {
                    result = building.villageHealAmount;
                }
            }

            return result;
        }
    }

    public List<EconomyBuilding> buildingsCompleted = new List<EconomyBuilding>();
    public EconomyBuilding buildingProject = null;

    //unit types that have offered to join us in the past. Won't have repeats of the same type.
    public List<UnitType> joinOffers = new List<UnitType>();

    //teams that have requested the player war with this team.
    public List<Team> teamsConspiredWithPlayerAgainst = new List<Team>();

    //A list of teams that the player was asked by to conspire against this team but were turned down.
    public List<Team> teamsPlayerRejectedConspiracy = new List<Team>();

    public int numConspiraciesThankedPlayerFor = 0;

    public int questsCompleted = 0;

    public int favorabilityToPlayer {
        get {
            if(enemyOfPlayer) {
                return 0;
            }

            return teamsPlayerRejectedConspiracy.Count + questsCompleted;
        }
    }

    public int numGiftsGivenToPlayer = 0;

    public int giftsOwingToPlayer {
        get {
            if(favorabilityToPlayer > numGiftsGivenToPlayer) {
                return favorabilityToPlayer - (numGiftsGivenToPlayer+0);
            } else {
                return 0;
            }
        }
    }

    [SerializeField]
    List<UnitSpell> _additionalSpells = new List<UnitSpell>();

    [System.NonSerialized]
    public List<UnitSpell> newSpells = new List<UnitSpell>();

    public void LearnSpell(UnitSpell spell)
    {
        if(GetKnownSpells().Contains(spell) == false) {
            _additionalSpells.Add(spell);
            newSpells.Add(spell);
        }
    }

    public List<UnitSpell> GetKnownSpells(UnitInfo unit=null)
    {
        if(unit != null && unit.unitType.unitInfo.spells.Count == 0) {
            return new List<UnitSpell>();
        }

        List<UnitSpell> spells = new List<UnitSpell>(team.knownSpells);
        foreach(UnitSpell s in _additionalSpells) {
            if(spells.Contains(s) == false) {
                spells.Add(s);
            }
        }

        foreach(UnitType u in recruitTypes) {
            foreach(var s in u.unitInfo.spells) {
                if(spells.Contains(s) == false) {
                    spells.Add(s);
                }
            }
        }

        if(unit != null) {
            foreach(UnitSpell s in unit.spells) {
                if(spells.Contains(s) == false) {
                    spells.Add(s);
                }
            }

            foreach(UnitSpell s in unit.unitType.unitInfo.spells) {
                if(spells.Contains(s) == false) {
                    spells.Add(s);
                }
            }

            List<UnitSpell> result = new List<UnitSpell>();
            foreach(UnitSpell spell in spells) {
                if(unit.CanCastSpell(spell)) {
                    result.Add(spell);
                }
            }

            spells = result;
        }

        return spells;
    }



    public Loc keepLoc;

    public int numVillages = 0;

    public int numUnits = 0;

    public int rulerLevel {
        get {
            return buildingsCompleted.Count;
        }
    }

    public int scoreNeededForNextLevel {
        get {
            int index = rulerLevel;
            if(index < GameConfig.instance.renownLevelThresholds.Length) {
                return GameConfig.instance.renownLevelThresholds[index];
            }

            int n = GameConfig.instance.renownLevelThresholds.Length-1;
            return GameConfig.instance.renownLevelThresholds[n] + (GameConfig.instance.renownLevelThresholds[n] - GameConfig.instance.renownLevelThresholds[n-1])*(index-n);
        }
    }

    public int economicDevelopment {
        get {
            return scoreInfo.totalScore;
        }
    }

    public int gold = 20;

    public int baseIncome = 0;

    public int income {
        get {
            int result = baseIncome + numVillages;

            foreach(var ramp in team.incomeRamp) {
                if(ramp.nround <= GameController.instance.gameState.nround) {
                    if(ramp.repeating && ramp.nround > 0) {
                        result += ramp.namount * (GameController.instance.gameState.nround / ramp.nround);
                    } else {
                        result += ramp.namount;
                    }
                }
            }

            if(affordUpkeep > numUnits) {
                result += affordUpkeep - numUnits;
            }

            if(team.player == false) {
                float[] multipliers = new float[] { 0.7f, 1.0f, 1.3f };
                result = (int)(multipliers[GameController.instance.gameState.difficulty]*result);
            } else {
                result += GameController.instance.gameState.GetBuildingGoldIncome(nteam);
            }

            return result;
        }
    }

    public int affordUpkeep {
        get {
            if(numVillages <= 8) {
                return numVillages + team.baseUpkeep;
            } else {
                return 8 + (numVillages-8)/2 + team.baseUpkeep;
            }
        }
    }

    public int temporalUnitDuration {
        get {
            int result = 3;
            foreach(var building in buildingsCompleted) {
                result += building.temporalUnitDuration;
            }
            return result;
        }
    }

    public bool shownGreeting = false;

    public List<QuestInProgress> completedQuests = new List<QuestInProgress>();

    public List<QuestInProgress> currentQuests = new List<QuestInProgress>();
    public QuestInProgress currentQuest {
        get {
            if(currentQuests.Count == 0) {
                return null;
            }

            return currentQuests[currentQuests.Count-1];
        }
    }

    public bool agreedToSwornEnemies = false;
    public bool rejectedSwornEnemies = false;

    public bool questRejected = false;
    public bool allianceRejected = false;

    public bool noPlayerContact {
        get {
            return playerContactRound == -1;
        }
    }

    public bool RecordPlayerContact()
    {
        if(playerContactRound == -1) {
            Debug.Log("REVEALED: RECORD PLAYER CONTACT NEW TEAM");
            playerContactRound = GameController.instance.gameState.nround;
            playerLastRequestRound = GameController.instance.gameState.nround;
            return true;
        }

        return false;
    }

    public int warnedPlayerAboutQuest = -1;

    public bool wantsPlayerContact {
        get {
            if(team.player) {
                return false;
            }

            if(wantsAllianceWithPlayer || wantsWarWithPlayer) {
                return true;
            }

            bool result = (playerContactRound == -1 && team.initialGreeting != null  && team.initialGreeting.executeOnceExpired == false) || (giftsOwingToPlayer > 0);

            if(enemyOfPlayer == false && playerContactRound >= 0 && currentQuest != null && currentQuest.completed == false && currentQuest.quest.FailedDeclareWar(currentQuest)) {
                //Want to declare war on the player for failure to complete quest.
                return true;
            }

            if(enemyOfPlayer == false && playerContactRound >= 0 && currentQuest != null && currentQuest.completed == false && currentQuest.quest.AlmostFailed(currentQuest) && (warnedPlayerAboutQuest == -1 || GameController.instance.gameState.nround - warnedPlayerAboutQuest >= 6)) {
                //Want to warn player about lack of quest progress.
                return true;
            }


            //wants player contact if they haven't talked to the player yet
            //and they have an initial greeting to give.
            return (playerContactRound == -1 && team.initialGreeting != null  && team.initialGreeting.executeOnceExpired == false) || (giftsOwingToPlayer > 0) || (currentQuest != null && currentQuest.completed);
        }
    }

    public bool hasPlayerContact {
        get {
            return team.player || playerContactRound != -1;
        }
    }

    public int playerContactRound = -1;

    public int playerLastRequestRound = -1;

    public void RecordPlayerRequest()
    {
        playerLastRequestRound = GameController.instance.gameState.nround;
    }

    public bool wantsWarWithPlayer {
        get {
            return allyOfPlayer == false && enemyOfPlayer && relationsWithPlayerRating <= 0;
        }
    }

    public bool wantsAllianceWithPlayer {
        get {
            return allyOfPlayer == false && relationsWithPlayerRating >= 100;
        }
    }

    public HashSet<Loc> mapUnfogged = new HashSet<Loc>();
    public HashSet<Loc> mapRevealed = new HashSet<Loc>();
    public int numTilesRevealed = 0;

    public bool CanAffordUnit(UnitType unitType)
    {
        return gold >= unitType.cost;
    }

    public bool CanRecruitUnit(UnitType unitType)
    {
        return CanAffordUnit(unitType) && numUnits < affordUpkeep;
    }

    public void EarnGold(int amount)
    {
        gold += amount;

        if(amount > 0) {
            scoreInfo.goldEarned += amount;
        }
    }

    public void BeginTurn()
    {
        EarnGold(income);

        _temporaryMarkets.Clear();

        if(currentQuest != null && currentQuest.timeUntilExpired <= 0) {
            AddRelationsWithPlayerChange("Taking too long to uphold your promises", -4);
        }
    }

    public List<VillageBuilding> villageBuildingsAvailableToBuild {
        get {
            List<VillageBuilding> result = new List<VillageBuilding>();
            foreach(EconomyBuilding building in buildingsCompleted) {
                foreach(VillageBuilding villageBuilding in building.villageBuildingsAvailable) {
                    if(result.Contains(villageBuilding) == false) {
                        result.Add(villageBuilding);
                    }
                }
            }

            return result;
        }
    }

    public bool unitsHaveHaste {
        get {
            if(team.unitsHaveHaste) {
                return true;
            }

            foreach(EconomyBuilding building in buildingsCompleted) {
                if(building.givesHaste) {
                    return true;
                }
            }

            return false;
        }
    }

    public bool unitsGrantedItemsOnRecruit {
        get {
            foreach(EconomyBuilding building in buildingsCompleted) {
                if(building.potionGranted) {
                    return true;
                }
            }

            return false;
        }
    }

    public int recruitExperienceBonus {
        get {
            int result = 0;
            foreach(EconomyBuilding building in buildingsCompleted) {
                result += building.recruitExperienceBonus;
            }

            return result;
        }
    }

    public int nteam {
        get {
            int index = 0;
            foreach(var t in GameController.instance.teams) {
                if(t == this) {
                    return index;
                }
                ++index;
            }

            return -1;
        }
    }

    public List<Loc> keepsOwned {
        get {
            List<Loc> result = new List<Loc>();
            int n = nteam;
            foreach(var p in GameController.instance.gameState.owners) {
                if(p.Value.currentOwner == n && GameController.instance.map.GetTile(p.Key).terrain.rules.keep) {
                    result.Add(p.Key);
                }
            }

            return result;
        }
    }
}

[CreateAssetMenu(menuName = "Wesnoth/Team")]
public class Team : GWScriptableObject
{
    public bool debugForceShareVisionWithPlayer = false;
    public bool debugAISkip = false;

    //which AI team this player team replaces.
    public Team playerTeamReplaces;

    public bool playerOrAllyOfPlayer {
        get {
            return player || teamInfo.allyOfPlayer;
        }
    }

    public List<SpeechPrompt> speechPrompts = new List<SpeechPrompt>();


    public bool player = false;
    public bool barbarian = false;
    public bool primaryEnemy = false;
    public bool handlesCaveSpawns = false;
    public bool unitsHaveHaste = false;
    public bool unitsDieOnRulerDeath = false;

    public bool regularAITeam {
        get {
            return player == false && barbarian == false && primaryEnemy == false;
        }
    }

    public float aiConquestRatio = 0f;
    public float aiAggroRatio = 0f;

    public int numQuestsForAlliance = 3;

    public List<Team> permanentFriends = new List<Team>();
    public List<Team> swornEnemies = new List<Team>();

    [System.Serializable]
    public class DefaultRelationship {
        public Team team;
        public int rating;
    }

    public List<DefaultRelationship> defaultRelationships;

    public int baseDifficultyRelationship {
        get {
            int[] baseValue = new int[] { 60, 50, 40 };
            return baseValue[GameController.instance.gameState.difficulty];
        }
    }

    public int GetDefaultRelationship(Team t)
    {
        foreach(var r in defaultRelationships) {
            if(r.team == t) {
                return r.rating - 50;
            }
        }

        return 0;
    }

    //Maximum depth this team will venture to. 2 = goes to underworld, 1 = stay in overworld.
    public int maxDepth = 2;

    public DiplomacyNode initialGreeting {
        get {
            if(diplomacy != null && diplomacy.initialGreeting.Count > 0) {
                return diplomacy.initialGreeting[0];
            }

            return null;
        }
    }

    public DiplomacyNode initialGreetingAlreadyAtWar;


    public DiplomacyNode friendlyDiplomacy;

    public DiplomacyNode completedQuestDiplomacy;

    public DiplomacyNode alliedDiplomacy;

    public string[] playerStoryText;

    [TextArea(3, 4)]
    public string fealtyOffer;

    [System.Serializable]
    public enum DiplomacyStatus
    {
        Hostile, Peaceful, Ally
    }

    public int teamIndex {
        get {
            int n;
            for(n = 0; n != GameController.instance.teams.Count; ++n) {
                if(GameController.instance.teams[n].team == this) {
                    return n;
                }
            }

            return n;
        }
    }

    public TeamInfo teamInfo {
        get {
            return GameController.instance.gameState.GetTeamInfo(this);
        }
    }

    public bool IsTeamInGame {
        get {
            return GameController.instance.gameState.IsTeamInGame(this);
        }
    }

    public int baseUpkeep = 3;

    public TeamColoring coloring;
    public AI ai = null;

    [SerializeField]
    DiplomacyRegistry _diplomacy = null;

    public DiplomacyRegistry diplomacy {
        get {
            return _diplomacy ?? GameConfig.instance.defaultDiplomacyRegistry;
        }
    }

    public FlagType flagType = null;

    public string teamNameAsProperNounCap {
        get {
            if(teamNameIsProperNoun) {
                return teamName;
            } else {
                return "The " + teamName;
            }
        }
    }


    public string teamNameAsProperNoun {
        get {
            if(teamNameIsProperNoun) {
                return teamName;
            } else {
                return "the " + teamName;
            }
        }
    }
    public bool teamNameIsProperNoun = false;
    public string teamName;
    public string rulerName;
    public string rulerTitle;

    [TextArea(3,3)]
    public string factionDescription;

    [TextArea(3, 3)]
    public string factionTooltipDescription;


    public int startingGold = -1;
    public int baseIncome = -1;

    [System.Serializable]
    public class IncomeRamp
    {
        public int nround;
        public int namount;
        public bool repeating = false;
    }

    public List<IncomeRamp> incomeRamp = new List<IncomeRamp>();

    public bool hasRoads = true;
    public bool aquatic = false;

    public UnitType rulerType;
    public List<UnitType> recruitmentOptions = new List<UnitType>();

    public int recruitmentSlots = -1;

    public List<QuestInProgress> possibleQuests = new List<QuestInProgress>();

    public List<UnitSpell> knownSpells = new List<UnitSpell>();


    public bool IsEnemy(Team other)
    {
        if(other == this) {
            return false;
        }

        if(barbarian || other.barbarian) {
            return barbarian != other.barbarian;
        }

        if(primaryEnemy || other.primaryEnemy) {
            return playerOrAllyOfPlayer || other.playerOrAllyOfPlayer;

        }

        if(playerOrAllyOfPlayer && other.playerOrAllyOfPlayer) {
            //for now players are always allies?
            return false;
        }

        if(other.playerOrAllyOfPlayer) {
            return other.IsEnemy(this);
        }

        if(playerOrAllyOfPlayer) {
            return GameController.instance.gameState.GetTeamInfo(other).playerDiplomacyStatus == DiplomacyStatus.Hostile;
        }

        if(swornEnemies.Contains(other) || other.swornEnemies.Contains(this)) {
            return true;
        }

        if(GameController.instance.gameState.GetTeamInfo(this).enemies.Contains(other)) {
            return true;
        }

        return false;
    }

    public bool IsAlly(Team other)
    {
        if(other == this) {
            return true;
        }

        if(barbarian || other.barbarian) {
            return false;
        }

        if(playerOrAllyOfPlayer && other.playerOrAllyOfPlayer) {
            //for now players are always allies?
            return true;
        }

        if(other.playerOrAllyOfPlayer) {
            return other.IsAlly(this);
        }

        if(playerOrAllyOfPlayer) {
            return GameController.instance.gameState.GetTeamInfo(other).playerDiplomacyStatus == DiplomacyStatus.Ally;
        }

        return false;
    }

    public bool IsIndifferent(Team other)
    {
        return !IsAlly(other) && !IsEnemy(other);
    }

    [SerializeField]
    EconomyBuilding[] _uniqueEconomyBuildings = null;

    public List<EconomyBuilding> economyBuildings {
        get {
            List<EconomyBuilding> result = new List<EconomyBuilding>();

            List<Vector2Int> locs = new List<Vector2Int>();

            if(_uniqueEconomyBuildings != null) {
                foreach(EconomyBuilding b in _uniqueEconomyBuildings) {
                    result.Add(b);
                    locs.Add(b.loc);
                }
            }

            foreach(EconomyBuilding b in GameConfig.instance.baseEconomyBuildings) {
                if(locs.Contains(b.loc) == false) {
                    result.Add(b);
                }
            }

            return result;
        }
    }

    public EconomyBuilding startingEconomyBuilding {
        get {
            foreach(var b in economyBuildings) {
                if(b.loc == Vector2Int.zero) {
                    return b;
                }
            }

            return null;
        }
    }

    public UnitInfo GenerateUnitToFoster() {
        if(recruitmentOptions.Count == 0)
            return null;

        int index = GameController.instance.gameState.seed%recruitmentOptions.Count;
        return recruitmentOptions[index].createUnit(GameController.instance.gameState.seed);
    }

    [System.Serializable]
    struct RelationsDescription
    {
        public int maxRelations;
        public string[] descriptions;
    }

    [SerializeField]
    RelationsDescription[] relationsDescriptions;

    public string DescribeDefaultRelationship(int nrelationship)
    {
        foreach(var desc in relationsDescriptions) {
            if(nrelationship <= desc.maxRelations) {
                int n = GameController.instance.rng.Next()%desc.descriptions.Length;
                return desc.descriptions[n];
            }
        }

        return "been on good terms with you";
    }
}

