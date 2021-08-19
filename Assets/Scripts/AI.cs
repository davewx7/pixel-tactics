using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class UnitAITemporaryStatus
{
    public int thinkRound = -1;
}

[System.Serializable]
public enum AIUnitAssignment
{
    None, //none/null value.
    StaticGuard, //this unit guards its location and doesn't move from there.
    HomeGuard, //this unit guards core territories.
    Conquest, //this unit conquers additional territories the kingdom has claimed.
    AttackPlayer, //this unit is dedicated to war with the player.
    Messenger, //this unit seeks out the player to deliver them a message.
}

[System.Serializable]
public struct AIUnitOrders
{
    public string unitGuid;
    public AIUnitAssignment assignment;
}

[System.Serializable]
public class AIState
{
    public TeamInfo teamInfo {
        get {
            if(teamNumber < 0 || teamNumber >= GameController.instance.teams.Count) {
                for(int i = 0; i != GameController.instance.aiStates.Count; ++i) {
                    if(GameController.instance.aiStates[i] != null && GameController.instance.aiStates[i].teamNumber != i) {
                        Debug.LogErrorFormat("Bad team number: {0} -> {1}", i, GameController.instance.aiStates[i].teamNumber);
                    }
                }
                Debug.LogErrorFormat("Could not find teamInfo for aistate: {0}/{1}  {2} vs {3}", teamNumber, GameController.instance.teams.Count, GameController.instance.teams.Count, GameController.instance.aiStates.Count);
            }
            return GameController.instance.teams[teamNumber];
        }
    }

    //for underworld teams the location of the gate to the overworld.
    public Loc gateToOverworld;

    public int teamNumber;

    public Loc keepLoc;
    public List<Loc> coreVillages = new List<Loc>();
    public List<Loc> villageClaims = new List<Loc>();
    public HashSet<Loc> territoryClaims = new HashSet<Loc>();
    public HashSet<Loc> coreTerritory = new HashSet<Loc>();

    //locations the AI is forbidden from moving to.
    public HashSet<Loc> forbiddenLocs = new HashSet<Loc>();

    public List<Loc> exclaves = new List<Loc>();

    //target location used by aggro AI's.
    public Loc targetLoc;

    public Dictionary<Loc, List<string>> villageCaptureOrders = new Dictionary<Loc, List<string>>();

    //Number of villages this kingdom would like to conquer in addition to its starting villages.
    public int totalVillagesGoal = 0;

    //The number of villages claimed when making a new claim to land.
    public int newClaimSize = 5;

    public List<AIUnitOrders> _unitOrders = new List<AIUnitOrders>();

    public float UnitAssignmentRatio(AIUnitAssignment assignment)
    {
        int nresult = 0;
        int ntotal = 0;
        foreach(AIUnitOrders order in _unitOrders) {
            Unit unit = GameController.instance.GetUnitByGuid(order.unitGuid);
            if(unit == null || unit.unitInfo.dead) {
                continue;
            }

            ++ntotal;
            if(order.assignment == assignment) {
                ++nresult;
            }
        }

        if(nresult == 0) {
            return 0f;
        }

        return ((float)nresult)/((float)ntotal);
    }

    public AIUnitOrders GetUnitOrders(Unit unit)
    {
        foreach(var orders in _unitOrders) {
            if(orders.unitGuid == unit.unitInfo.guid) {
                return orders;
            }
        }

        return new AIUnitOrders() { unitGuid = unit.unitInfo.guid };
    }

    public void SetUnitOrders(AIUnitOrders orders)
    {
        Debug.Log("SET UNIT ORDERS: " + orders.assignment.ToString());
        for(int i = 0; i != _unitOrders.Count; ++i) {
            if(orders.unitGuid == _unitOrders[i].unitGuid) {
                _unitOrders[i] = orders;
            }
        }

        _unitOrders.Add(orders);
    }

    public void ClearOrders(AIUnitAssignment assignment)
    {
        var newOrders = new List<AIUnitOrders>();
        foreach(var order in _unitOrders) {
            if(order.assignment != assignment) {
                newOrders.Add(order);
            }
        }

        _unitOrders = newOrders;
    }


    public void NewTurn()
    {
        int numCore = 0, numClaimed = 0, numUnclaimed = 0;
        foreach(Tile t in GameController.instance.map.tiles) {
            if(t.terrain.rules.village && GameController.instance.gameState.GetTeamOwnerOfLoc(t.loc) != null) {
                bool coreVillage = false;
                bool claimedVillage = false;
                foreach(AIState state in GameController.instance.aiStates) {
                    if(state == null) {
                        continue;
                    }

                    if(state.coreVillages.Contains(t.loc)) {
                        coreVillage = true;
                        break;
                    } else if(state.villageClaims.Contains(t.loc)) {
                        claimedVillage = true;
                        break;
                    }
                }

                if(coreVillage) {
                    ++numCore;
                } else if(claimedVillage) {
                    ++numClaimed;
                } else {
                    ++numUnclaimed;
                }
            }
        }

        Debug.LogFormat("VILLAGES: core = {0} claimed = {1} unclaimed = {2}", numCore, numClaimed, numUnclaimed);

        _unitOrders.RemoveAll((u) => (GameController.instance.GetUnitByGuid(u.unitGuid) == null || GameController.instance.GetUnitByGuid(u.unitGuid).unitInfo.dead));

        Dictionary<Loc, List<string>> captureOrders = new Dictionary<Loc, List<string>>();


        //cull dead units from village capture orders.
        foreach(var p in villageCaptureOrders) {
            var list = new List<string>();
            foreach(string guid in p.Value) {
                Unit u = GameController.instance.GetUnitByGuid(guid);
                if(u != null && !u.unitInfo.dead) {
                    list.Add(guid);
                }
            }

            if(list.Count > 0) {
                captureOrders[p.Key] = list;
            }
        }

        villageCaptureOrders = captureOrders;
    }


    public bool readyForNewClaim {
        get {
            return _capturedCoreTerritory && villageClaims.Count < totalVillagesGoal;
        }
    }

    bool _capturedCoreTerritory = false;

    public bool IsVillageWeWantToCapture(Loc villageLoc)
    {
        var owner = GameController.instance.gameState.GetOwnerOfLoc(villageLoc);
        return owner != GameController.instance.currentTeamNumber && (owner < 0 || GameController.instance.currentTeam.IsEnemy(GameController.instance.teams[owner].team));
    }

    public void Calculate()
    {
        if(_capturedCoreTerritory == false) {
            _capturedCoreTerritory = true;
            foreach(Loc villageLoc in villageClaims) {
                if(IsVillageWeWantToCapture(villageLoc)) {
                    _capturedCoreTerritory = false;
                    break;
                }
            }
        }
    }

    public void AddClaim(List<Loc> newClaim)
    {
        foreach(var village in newClaim) {
            villageClaims.Add(village);

            for(int i = 0; i <= 3; ++i) {
                foreach(Loc loc in Tile.GetTilesInRing(village, i)) {
                    territoryClaims.Add(loc);
                }
            }
        }

        _capturedCoreTerritory = false;
    }

    public void CreateVillageCaptureOrder(Unit unit, Loc village)
    {
        if(villageCaptureOrders.ContainsKey(village) == false) {
            villageCaptureOrders[village] = new List<string>();
        }
        villageCaptureOrders[village].Add(unit.unitInfo.guid);
    }

    public List<Unit> UnitAssignedToCaptureVillage(Loc village)
    {
        if(villageCaptureOrders.ContainsKey(village)) {
            List<Unit> result = new List<Unit>();
            foreach(string guid in villageCaptureOrders[village]) {
                result.Add(GameController.instance.GetUnitByGuid(guid));
            }
            return result;
        }

        return null;
    }

    public Loc VillageAssignedToCapture(Unit unit)
    {
        foreach(var p in villageCaptureOrders) {
            if(p.Value.Contains(unit.unitInfo.guid)) {
                return p.Key;
            }
        }

        return Loc.invalid;
    }

    public void MarkVillageCaptured(Loc village)
    {
        villageCaptureOrders.Remove(village);
    }

    public List<Unit> units {
        get {
            List<Unit> result = new List<Unit>();
            foreach(Unit unit in GameController.instance.units) {
                if(unit.team == teamInfo.team) {
                    result.Add(unit);
                }
            }
            return result;
        }
    }

}

[CreateAssetMenu(menuName = "Wesnoth/AI")]
public class AI : GWScriptableObject
{
    protected GameController _controller {
        get { return GameController.instance; }
    }
    GameState _gameState {
        get { return _controller.gameState; }
    }

    virtual protected bool IsTeamAnEnemy(AIState state, Team enemyTeam)
    {
        return enemyTeam != null && enemyTeam.IsEnemy(state.teamInfo.team);
    }

    virtual protected bool IsTeamWantsContact(AIState state, Team enemyTeam)
    {
        return enemyTeam.player && state.teamInfo.wantsPlayerContact;

    }

    virtual protected bool IsTeamAnEnemyOrWantsContact(AIState state, Team enemyTeam)
    {
        return IsTeamAnEnemy(state, enemyTeam) || IsTeamWantsContact(state, enemyTeam);
    }


    protected List<Loc> _locsAdjacentToEnemy = null;
    void CalculateLocsAdjacentToEnemy(AIState aiState)
    {
        _locsAdjacentToEnemy = new List<Loc>();
        foreach(Unit enemy in _controller.units) {
            if(IsTeamAnEnemyOrWantsContact(aiState, enemy.team) == false || enemy.unitInfo.isInvisible || _vision.Contains(enemy.loc) == false) {
                continue;
            }

            if(enemy.team.barbarian && aiState.territoryClaims.Contains(enemy.loc) == false) {
                //don't attack barbarians outside of our core territory.
                continue;
            }

            Loc[] adj = Tile.AdjacentLocs(enemy.loc);
            foreach(Loc a in adj) {
                if(_locsAdjacentToEnemy.Contains(a) == false) {
                    _locsAdjacentToEnemy.Add(a);
                }
            }
        }
    }

    public void MakeClaim(AIState aiState)
    {
        //claim new territory.
        List<Loc> candidates = new List<Loc>();
        foreach(Loc villageLoc in GameController.instance.villageLocs) {
            if(_aiState.villageClaims.Contains(villageLoc)) {
                continue;
            }

            candidates.Add(villageLoc);
        }

        candidates.Sort((a, b) => Tile.DistanceBetween(a,_aiState.keepLoc).CompareTo(Tile.DistanceBetween(b,_aiState.keepLoc)));

        List<Loc> newClaim = new List<Loc>();
        for(int i = 0; i < candidates.Count && i < _aiState.newClaimSize; ++i) {
            Debug.Log("NEW CLAIM: " + candidates[i].ToString());
            newClaim.Add(candidates[i]);
        }

        _aiState.AddClaim(newClaim);
    }

    protected AIState _aiState = null;
    protected HashSet<Loc> _vision = null;
    protected List<Unit> _visibleEnemies = null;
    protected List<Loc> _visibleEnemyVillages = null;

    protected void CalculateVision(AIState aiState)
    {
        _visibleEnemies = new List<Unit>();
        _vision = new HashSet<Loc>();
        _visibleEnemyVillages = new List<Loc>();
        var visionTiles = GameController.instance.CalculateVision(_controller.gameState.nturn);
        foreach(var t in visionTiles) {
            _vision.Add(t.loc);

            if(t.terrain.rules.village && IsTeamAnEnemy(aiState, _gameState.GetTeamOwnerOfLoc(t.loc))) {
                _visibleEnemyVillages.Add(t.loc);
            }

            if(t.unit != null && IsTeamAnEnemy(aiState, t.unit.team) && t.unit.unitInfo.isInvisible == false) {
                _visibleEnemies.Add(t.unit);
            }
        }
    }

    //Analysis of enemies that need to be eliminated.
    Dictionary<Loc, float> _threatsAnalysis;

    void CalculateThreats(AIState aiState)
    {
        _threatsAnalysis = new Dictionary<Loc, float>();
        foreach(var villageLoc in _aiState.coreVillages) {
            if(_vision.Contains(villageLoc) == false) {
                continue;
            }
            Unit unit = GameController.instance.GetUnitAtLoc(villageLoc);
            if(unit == null) {
                continue;
            }

            if(IsTeamAnEnemy(aiState, unit.team)) {
                _threatsAnalysis[villageLoc] = 2f*(float)unit.unitInfo.level;
            }
        }

        foreach(var loc in _aiState.coreTerritory) {
            if(_vision.Contains(loc) == false) {
                continue;
            }

            Unit unit = GameController.instance.GetUnitAtLoc(loc);
            if(unit == null) {
                continue;
            }

            if(IsTeamAnEnemy(_aiState, unit.team)) {
                _threatsAnalysis[loc] = 1f*(float)unit.unitInfo.level;
            }
        }
    }

    float _beginTurnTime = 0f;
    float _beginTurnCommandTime = 0f;

    public void ShowDiplomacy(AIState aiState, Unit aiUnit, Unit playerUnit)
    {
        //Since we have contact anyone whose a messenger stops being so.
        aiState.ClearOrders(AIUnitAssignment.Messenger);

        TeamInfo teamInfo = aiState.teamInfo;
        Team team = teamInfo.team;

        GameController.instance.QueueDiplomacyCommand(new DiplomacyNodeInfo() {
            node = null,
            aiUnit = aiUnit,
            playerUnit = playerUnit,
        });

        return;

        //        if(teamInfo.RecordPlayerContact() && team.initialGreeting != null) {
        //            ShowInitialContact(aiState, aiUnit, playerUnit);
        //            Debug.Log("Showing initial contact");
        //            return;
        //        }

        DiplomacyNode node = teamInfo.noPlayerContact ? team.initialGreeting : team.friendlyDiplomacy;
        Debug.Log("Have friendly diplomacy: " + (node != null));
        if(teamInfo.allyOfPlayer) {
            node = team.alliedDiplomacy;
        }

        QuestInProgress quest = teamInfo.currentQuest;
        if(node != null && quest != null && quest.completed) {
            node = team.completedQuestDiplomacy;
            Debug.Log("Try completed quest");
        }

        if(node != null) {
            Debug.Log("Showing diplomacy");
            GameController.instance.QueueDiplomacyCommand(new DiplomacyNodeInfo() {
                node = node,
                aiUnit = aiUnit,
                playerUnit = playerUnit,
            });
        }
    } 

    public void ShowInitialContact(AIState aiState, Unit aiUnit, Unit playerUnit)
    {
        TeamInfo teamInfo = aiState.teamInfo;
        Team team = teamInfo.team;

        DiplomacyNode greeting = teamInfo.enemyOfPlayer ? team.initialGreetingAlreadyAtWar : team.initialGreeting;

        if(greeting == null || FindRuler(aiState) == null || teamInfo.shownGreeting) {
            return;
        }

        teamInfo.shownGreeting = true;
        teamInfo.RecordPlayerRequest();

        GameController.instance.QueueDiplomacyCommand(new DiplomacyNodeInfo() {
            node = greeting,
            aiUnit = aiUnit,
            playerUnit = playerUnit,
        });
    }

    float CalculatePlayerTension(List<Team> addedEnemies=null)
    {
        float result = 0f;

        //calculate a score of how much tension a player is facing currently.
        //if it's too low we'll try to make sure an AI stirs up some drama.
        foreach(Unit unit in GameController.instance.units) {
            if(unit.team.IsEnemy(GameController.instance.playerTeam) == false) {
                if(addedEnemies == null || addedEnemies.Contains(unit.team) == false) {
                    continue;
                }
            }
            
            if(unit.team.barbarian) {
                if(unit.tile.revealed) {
                    result += 1f*unit.unitInfo.level;
                }
            } else {
                if(unit.tile.revealed) {
                    result += 2f*unit.unitInfo.level;
                } else {
                    result += 1f*unit.unitInfo.level;
                }
            }
        }

        return result;
    }

    float CalculateTensionAfterThreat(TeamInfo teamInfo, Team addedEnemy=null)
    {
        Team team = teamInfo.team;
        if(team.player || team.barbarian || teamInfo.hasPlayerContact == false || teamInfo.enemyOfPlayer || teamInfo.allyOfPlayer || FindRulerForTeam(team) == null) {
            return -1.0f;
        }

        float warWithEnemy = CalculatePlayerTension(new List<Team> { team });

        List<Team> enemiesOfEnemy = teamInfo.enemies;
        if(addedEnemy != null) {
            enemiesOfEnemy.Add(addedEnemy);
        }
        List<Team> enemiesWithContact = new List<Team>();
        foreach(Team t in enemiesOfEnemy) {
            TeamInfo enemyTeamInfo = GameController.instance.gameState.GetTeamInfo(t);
            if(enemyTeamInfo != null && enemyTeamInfo.hasPlayerContact) {
                enemiesWithContact.Add(t);
            }
        }

        float warWithEnemiesEnemies = CalculatePlayerTension(enemiesWithContact);

        return Mathf.Min(warWithEnemy, warWithEnemiesEnemies);
    }

    float CalculateTensionAfterThreatAndAddingEnemy(TeamInfo teamInfo, TeamInfo addedEnemy)
    {
        if(addedEnemy.team.player || addedEnemy.team.barbarian || addedEnemy.team == teamInfo.team || addedEnemy.enemyOfPlayer || addedEnemy.allyOfPlayer || addedEnemy.hasPlayerContact == false || teamInfo.team.permanentFriends.Contains(addedEnemy.team)) {
            return -1f;
        }

        return CalculateTensionAfterThreat(teamInfo, addedEnemy.team);
    }

    float CalculatePlayerPower()
    {
        float result = 0f;
        foreach(Unit unit in GameController.instance.units) {
            if(unit.team.player == false) {
                continue;
            }

            result += unit.unitInfo.level;
        }

        return result;
    }

    public virtual void WarWithPlayerStarted(AIState aiState)
    {
        AIState oldaistate = _aiState;
        _aiState = aiState;
        int numReassignments = Mathf.CeilToInt(aiState._unitOrders.Count*aiState.teamInfo.team.aiAggroRatio);
        Debug.LogFormat("AI: {3} WAR WITH PLAYER STARTED. REASSIGNING {0}x{1} = {2} TO BATTLE", aiState._unitOrders.Count, aiState.teamInfo.team.aiAggroRatio, numReassignments, aiState.teamInfo.team.teamName);

        for(int i = 0; numReassignments > 0 && i != aiState._unitOrders.Count; ++i) {
            AIUnitOrders orders = aiState._unitOrders[i];
            Unit unit = GameController.instance.GetUnitByGuid(orders.unitGuid);

            if(orders.assignment == AIUnitAssignment.HomeGuard && unit != null && unit.unitInfo.dead == false) {
                Debug.Log("AI: REASSIGN " + unit.loc + " TO WAR");
                orders.assignment = AIUnitAssignment.AttackPlayer;
                aiState._unitOrders[i] = orders;
                --numReassignments;
            }
        }

        _aiState = oldaistate;
    }

    public virtual void NewTurn(AIState aiState)
    {
        _aiState = aiState;

        _beginTurnTime = Time.time;
        _beginTurnCommandTime = GameCommandQueue.elapsedTime;
        aiState.NewTurn();

        if(aiState.teamInfo.hasPlayerContact && aiState.teamInfo.currentQuest != null && aiState.teamInfo.currentQuest.completed && aiState.UnitAssignmentRatio(AIUnitAssignment.Messenger) == 0f) {
            //if the player has completed a quest for us we want to chat with them. Assign a unit to that.
            int bestMovement = 0;
            Unit bestUnit = null;
            foreach(Unit unit in aiState.teamInfo.GetUnits()) {
                if(unit.unitInfo.ruler) {
                    continue;
                }

                if(unit.unitInfo.movement > bestMovement) {
                    bestUnit = unit;
                }
            }

            if(bestUnit != null) {
                _aiState.SetUnitOrders(new AIUnitOrders() {
                    unitGuid = bestUnit.unitInfo.guid,
                    assignment = AIUnitAssignment.Messenger,
                });
            }
        }

        using(s_profileCalculateVision.Auto()) {
            CalculateVision(aiState);
        }
    }

    public void OnRecruit(Unit unit)
    {
        if(_aiState == null) {
            _aiState = GameController.instance.aiStates[unit.unitInfo.nteam];
        }

        AIUnitAssignment assignment = AIUnitAssignment.HomeGuard;

        float conquestingUnits = _aiState.UnitAssignmentRatio(AIUnitAssignment.Conquest);

        if(conquestingUnits < _aiState.teamInfo.team.aiConquestRatio) {
            assignment = AIUnitAssignment.Conquest;
        }

        if(IsTeamAnEnemy(_aiState, GameController.instance.playerTeam)) {
            float aggroUnits = _aiState.UnitAssignmentRatio(AIUnitAssignment.AttackPlayer);
            if(aggroUnits < _aiState.teamInfo.team.aiAggroRatio) {
                assignment = AIUnitAssignment.AttackPlayer;
            }
        }

        _aiState.SetUnitOrders(new AIUnitOrders() {
            unitGuid = unit.unitInfo.guid,
            assignment = assignment,
        });
    }

    Unit FindRulerForTeam(Team team)
    {
        foreach(var unit in GameController.instance.units) {
            if(unit.team == team && unit.unitInfo.ruler && _controller.map.GetTile(unit.loc).terrain.rules.keep) {
                return unit;
            }
        }

        return null;
    }

    Unit FindRuler(AIState aiState)
    {
        foreach(var unit in aiState.units) {
            if(unit.unitInfo.ruler && _controller.map.GetTile(unit.loc).terrain.rules.keep) {
                return unit;
            }
        }

        return null;
    }

    public bool ExecuteRecruit(AIState aiState, Unit rulerUnit)
    {
        Dictionary<Loc, Pathfind.Path> recruitLocs = Pathfind.FindPaths(_controller, rulerUnit.unitInfo, 1, new Pathfind.PathOptions() {
            recruit = true,
            excludeOccupied = true,
        });

        var locsList = new List<Loc>(recruitLocs.Keys);

        if(recruitLocs.Count > 0) {
            List<UnitType> options = new List<UnitType>();
            foreach(UnitType u in _controller.currentTeam.recruitmentOptions) {
                if(_controller.currentTeamInfo.CanRecruitUnit(u)) {
                    options.Add(u);
                }
            }

            if(options.Count > 0) {
                var loc = locsList[Random.Range(0, locsList.Count)];
                var option = options[Random.Range(0, options.Count)];
                _controller.ExecuteRecruit(new RecruitCommandInfo {
                    payCost = true,
                    loc = loc,
                    unitType = option,
                    seed = GameController.instance.rng.Next(),
                    haveHaste = GameController.instance.currentTeamInfo.unitsHaveHaste,
                });
                return true;
            }
        }

        return false;
    }

    public virtual void RemoveForbiddenVillagesFromPaths(Unit unit, Dictionary<Loc,Pathfind.Path> paths)
    {
        List<Loc> forbiddenVillages = new List<Loc>();
        foreach(var p in paths) {
            Team ownerOfLoc = GameController.instance.gameState.GetTeamOwnerOfLoc(p.Key);
            if(ownerOfLoc != null && ownerOfLoc != unit.team && IsTeamAnEnemy(_aiState, ownerOfLoc) == false) {
                forbiddenVillages.Add(p.Key);
            }
        }

        foreach(Loc village in forbiddenVillages) {
            paths.Remove(village);
        }
    }

    static ProfilerMarker s_profileCalculateVision = new ProfilerMarker("AI.CalculateVision");
    static ProfilerMarker s_profileCalculateLocsAdjacentToEnemy = new ProfilerMarker("AI.CalculateLocsAdjacentToEnemy");
    static ProfilerMarker s_profileStateCalculate = new ProfilerMarker("AI.StateCalculate");
    static ProfilerMarker s_profileUnitThink = new ProfilerMarker("AI.UnitThink");
    static ProfilerMarker s_profileFindPaths = new ProfilerMarker("AI.FindPaths");


    public virtual void Play(AIState aiState)
    {
        using(s_profileCalculateLocsAdjacentToEnemy.Auto()) {
            CalculateLocsAdjacentToEnemy(aiState);
        }

        _aiState = aiState;

        using(s_profileStateCalculate.Auto()) {
            aiState.Calculate();
            CalculateThreats(aiState);
        }

        if(aiState.readyForNewClaim) {
            MakeClaim(aiState);
        }

        List<Unit> units = new List<Unit>(aiState.units);
        units.Sort((Unit a, Unit b) => a.unitInfo.unitType.AIThinkOrder.CompareTo(b.unitInfo.unitType.AIThinkOrder));

        if(aiState.teamInfo.team.debugAISkip) {
            _controller.EndTurn();
            return;
        }

        foreach(var unit in units) {
            if(unit.unitInfo.unitType.attacks.Count == 0) {
                Debug.LogErrorFormat("Unit has no attacks: {0}", unit.unitInfo.unitType.name);
            }
            if(unit.unitInfo.ruler && _controller.map.GetTile(unit.loc).terrain.rules.keep) {
                bool recruited = ExecuteRecruit(aiState, unit);
                if(recruited) {
                    return;
                }
            }

            if(unit.unitInfo.movementRemaining < unit.unitInfo.movement || unit.unitInfo.hasAttacked) {
                continue;
            }

            s_profileFindPaths.Begin();

            var paths = Pathfind.FindPaths(_controller, unit.unitInfo, unit.unitInfo.movementRemaining, new Pathfind.PathOptions() { ignoreZocs = unit.unitInfo.isSkirmish, forbiddenLocs = aiState.forbiddenLocs, maxDepth = aiState.teamInfo.team.maxDepth });

            //Find any villages owned by allies and remove them from our list of possible moves.
            //We don't move into villages our allies control.
            RemoveForbiddenVillagesFromPaths(unit, paths);

            s_profileFindPaths.End();

            if(unit.aiStatus.thinkRound == GameController.instance.gameState.nround) {
                continue;
            }

            unit.aiStatus.thinkRound = GameController.instance.gameState.nround;

            if(unit.unitInfo.ruler) {
                if(RulerThink(unit, paths)) {
                    return;
                }

                continue;
            }

            if(paths.Count == 0) {
                continue;
            }

            using(s_profileUnitThink.Auto()) {
                if(UnitThink(unit, paths)) {
                    return;
                }
            }
        }

        _controller.EndTurn();
        aiThinkTime += Time.time - this._beginTurnTime;
        Debug.Log("PERF: AI TURN PLAYED IN " + (Time.time - this._beginTurnTime) + " / " + (GameCommandQueue.elapsedTime - _beginTurnCommandTime));
    }

    static public float aiThinkTime = 0f;

    static public float AttackExpectedDamage(AttackInfo attack)
    {
        return attack.damage*attack.nstrikes*(100+attack.accuracy);
    }


    static public float ScoreAttack(AttackInfo attack)
    {
        float damage = AttackExpectedDamage(attack);

        foreach(var ability in attack.abilities) {
            damage *= ability.AIMultiplier;
        }

        return damage;
    }

    public virtual float ScoreBattle(Unit attacker, Unit target)
    {
        float score = 100f;

        //prioritize fights that put us in desirable terrain.
        TerrainRules attackerTerrain = GameController.instance.map.GetTile(attacker.loc).terrain.rules;

        if(attackerTerrain.village) {
            score += 10.0f;
        }

        score += attackerTerrain.unitMod.evasion*0.1f + attackerTerrain.unitMod.armor*1.0f;

        float targetValue = (target.unitInfo.level+1);
        if(target.unitInfo.ruler) {
            targetValue *= 4.0f;
        }

        if(target.team.barbarian) {
            //attacking barbarians is much less interesting than attacking other powers.
            targetValue *= 0.5f;
        }


        float bestAttackScore = 0.0f;
        var attacks = attacker.unitInfo.GetAttacksForBattle(target.unitInfo, true);
        foreach(var attack in attacks) {
            float expectedDamage = AttackExpectedDamage(attack);
            float damageValue = targetValue*(expectedDamage/target.unitInfo.hitpointsRemaining);
            if(expectedDamage >= target.unitInfo.hitpointsRemaining) {
                //give a little bonus if we think we can get a kill.
                damageValue *= 1.5f;
            }

            if(damageValue > bestAttackScore) {
                bestAttackScore = damageValue;
            }
        }

        score += bestAttackScore;

        return score;
    }

    public virtual bool RulerThink(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        AIState aiState = GameController.instance.aiStates[unit.unitInfo.ncontroller];

        if(unit.hasEnoughExperienceToLevelUp && unit.canLevelUp) {
            var options = LevelUpDialog.GetLevelUpOptions(unit.unitInfo);
            int index = GameController.instance.rng.Next()%options.Count;
            _controller.ExecuteLevelUp(unit.loc, options[index]);
            return true;
        }


        if(unit.unitInfo.charmed) {
            return false;
        }

        float bestAttackScore = 0f;
        Loc bestAttackLoc;
        Unit bestAttackTarget = null;
        foreach(Tile tile in unit.tile.adjacentTiles) {
            if(tile != null && tile.unit != null && IsTeamAnEnemy(aiState, tile.unit.team) && tile.unit.unitInfo.isInvisible == false) {
                float score = ScoreBattle(unit, tile.unit);
                if(score > bestAttackScore) {
                    bestAttackScore = score;
                    bestAttackLoc = tile.loc;
                    bestAttackTarget = tile.unit;
                }
            }
        }

        if(bestAttackTarget != null) {
            return ExecuteAttack(unit, bestAttackTarget, paths, unit.loc);
        }


        return false;
    }

    public bool TryLevelUp(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        if(unit.hasEnoughExperienceToLevelUp) {
            bool canLevel = unit.canLevelUp;
            Loc loc = unit.loc;
            if(canLevel == false) {
                foreach(var p in paths) {
                    if(GameController.instance.map.GetTile(p.Key).terrain.rules.canLongRest) {
                        loc = p.Key;
                        _controller.SendMoveCommand(p.Value);
                        canLevel = true;
                        break;
                    }
                }
            }

            if(canLevel) {
                var options = LevelUpDialog.GetLevelUpOptions(unit.unitInfo);
                int index = GameController.instance.rng.Next()%options.Count;
                _controller.ExecuteLevelUp(loc, options[index]);
                return true;
            }
        }

        return false;
    }

    public bool TryAttack(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        AIState aiState = GameController.instance.aiStates[unit.unitInfo.ncontroller];

        //see if we want to attack.
        float bestAttackScore = -1f;
        Loc bestAttackLoc = Loc.invalid;
        Unit bestAttackTarget = null;

        if(unit.unitInfo.attacks.Count > 0 && unit.unitInfo.charmed == false) {
            foreach(Loc loc in _locsAdjacentToEnemy) {
                if(loc == unit.loc || paths.ContainsKey(loc)) {
                    Loc currentLoc = unit.unitInfo.loc;
                    unit.unitInfo.loc = loc;

                    foreach(Loc adj in Tile.AdjacentLocs(loc)) {
                        Unit target = GameController.instance.GetUnitAtLoc(adj);
                        if(target != null && IsTeamAnEnemyOrWantsContact(aiState, target.team)) {
                            float score = ScoreBattle(unit, target);
                            if(IsTeamWantsContact(aiState, target.team)) {
                                //heavily prioritize making contact with a new team.
                                score += 100000.0f;
                            }
                            if(score > bestAttackScore) {
                                bestAttackScore = score;
                                bestAttackLoc = loc;
                                bestAttackTarget = target;
                            }
                        }
                    }

                    unit.unitInfo.loc = currentLoc;
                }
            }

            if(bestAttackScore > 0f) {
                //We have an attack, execute it.
                ExecuteAttack(unit, bestAttackTarget, paths, bestAttackLoc);
                return true;
            }
        }

        return false;
    }

    public virtual bool UnitThink(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        AIState aiState = GameController.instance.aiStates[unit.unitInfo.ncontroller];

        if(TryLevelUp(unit,paths)) {
            return true;
        }

        if(TryAttack(unit,paths)) {
            return true;
        }

        AIUnitOrders orders = _aiState.GetUnitOrders(unit);
        if(orders.assignment == AIUnitAssignment.StaticGuard) {
            return false;
        }

        //see if we want to attack.
        float bestAttackScore = -1f;
        Loc bestAttackLoc = Loc.invalid;
        Unit bestAttackTarget = null;

        if(unit.unitInfo.attacks.Count > 0 && unit.unitInfo.charmed == false) {
            foreach(Loc loc in _locsAdjacentToEnemy) {
                if(loc == unit.loc || paths.ContainsKey(loc)) {
                    Loc currentLoc = unit.unitInfo.loc;
                    unit.unitInfo.loc = loc;

                    foreach(Loc adj in Tile.AdjacentLocs(loc)) {
                        Unit target = GameController.instance.GetUnitAtLoc(adj);
                        if(target != null && IsTeamAnEnemyOrWantsContact(aiState, target.team)) {
                            float score = ScoreBattle(unit, target);
                            if(IsTeamWantsContact(aiState, target.team)) {
                                //heavily prioritize making contact with a new team.
                                score += 100000.0f;
                            }
                            if(score > bestAttackScore) {
                                bestAttackScore = score;
                                bestAttackLoc = loc;
                                bestAttackTarget = target;
                            }
                        }
                    }

                    unit.unitInfo.loc = currentLoc;
                }
            }

            if(bestAttackScore > 0f) {
                //We have an attack, execute it.
                ExecuteAttack(unit, bestAttackTarget, paths, bestAttackLoc);
                return true;
            }
        }

        if(unit.team.barbarian) {
            //barbarians do nothing but attack.
            return false;
        }

        Loc village = _aiState.VillageAssignedToCapture(unit);
        if(village != Loc.invalid) {

            if(_aiState.IsVillageWeWantToCapture(village) == false) {
                _aiState.MarkVillageCaptured(village);
                village = Loc.invalid;
            } else {
                if(paths.ContainsKey(village)) {
                    _controller.SendMoveCommand(paths[village]);
                    _aiState.MarkVillageCaptured(village);
                    return true;
                }

                return UnitMoveToward(unit, paths, village);
            }
        }

        foreach(var p in paths) {
            if(_controller.map.GetTile(p.Value.dest).terrain.rules.village && _controller.gameState.GetOwnerOfLoc(p.Value.dest) != _controller.gameState.nturn && _aiState.territoryClaims.Contains(p.Value.dest)) {
                _controller.SendMoveCommand(p.Value);
                _aiState.MarkVillageCaptured(p.Key);
                return true;
            }
        }

        //scan for villages we claim but don't own and dispatch this unit to them.
        foreach(var villageLoc in _aiState.villageClaims) {
            if(_aiState.IsVillageWeWantToCapture(villageLoc) && _aiState.UnitAssignedToCaptureVillage(villageLoc) == null) {
                _aiState.CreateVillageCaptureOrder(unit, villageLoc);
                return UnitThink(unit, paths);
            }
        }

        if(_threatsAnalysis != null && _threatsAnalysis.Count > 0) {
            Loc biggestThreat = Loc.invalid;

            float biggest = -1f;

            //respond to threats.
            foreach(var p in _threatsAnalysis) {
                int dist = Tile.DistanceBetween(p.Key, unit.loc);
                float r = p.Value / (float)dist;
                if(r > biggest) {
                    biggest = r;
                    biggestThreat = p.Key;
                }
            }

            if(biggestThreat != Loc.invalid) {
                return UnitMoveToward(unit, paths, biggestThreat);
            }
        }

        if(orders.assignment == AIUnitAssignment.HomeGuard && _aiState.villageClaims.Count > 0) {
            Loc villageLoc = _aiState.villageClaims[Random.Range(0, _aiState.villageClaims.Count)];
            return UnitMoveToward(unit, paths, villageLoc);
        } else if(orders.assignment == AIUnitAssignment.AttackPlayer) {
            return UnitThinkGoAfterPlayer(unit, paths);
        } else if(orders.assignment == AIUnitAssignment.Messenger) {
            return UnitThinkMessengerToPlayer(unit, paths);
        } else {

            //pile on to capture an enemy village.
            int nbest = -1;
            Loc bestVillage = Loc.invalid;
            foreach(var villageLoc in _aiState.villageClaims) {
                if(_aiState.IsVillageWeWantToCapture(villageLoc) && _aiState.UnitAssignedToCaptureVillage(villageLoc) != null) {
                    int nassigned = _aiState.UnitAssignedToCaptureVillage(villageLoc).Count;
                    if(bestVillage == Loc.invalid || nassigned < nbest) {
                        nbest = nassigned;
                        bestVillage = villageLoc;
                    }
                }
            }

            if(bestVillage != Loc.invalid) {
                _aiState.CreateVillageCaptureOrder(unit, bestVillage);
                return UnitThink(unit, paths);
            }
        }

        Debug.Log("Unit at " + unit.loc.ToString() + " has nothing to do, resting");

        return false;
    }

    public bool ExecuteAttack(Unit unit, Unit bestAttackTarget, Dictionary<Loc, Pathfind.Path> paths, Loc bestAttackLoc)
    {
        Loc currentLoc = unit.unitInfo.loc;
        unit.unitInfo.loc = bestAttackLoc;

        var attacks = unit.unitInfo.GetAttacksForBattle(bestAttackTarget.unitInfo, true);
        if(attacks.Count == 0) {
            Debug.LogError("Unit " + unit.unitInfo.unitType.description + " has no attacks");
            return true;
        }

        if(unit.unitInfo.charmed) {
            return true;
        }

        int bestIndex = 0;
        float bestScore = 0f;
        for(int i = 0; i < attacks.Count; ++i) {
            float score = ScoreAttack(attacks[i]);
            if(score > bestScore) {
                bestScore = score;
                bestIndex = i;
            }
        }

        AttackInfo attack = attacks[bestIndex];
        AttackInfo? counter = bestAttackTarget.unitInfo.GetBestCounterattack(unit.unitInfo, attack);

        unit.unitInfo.loc = currentLoc;

        if(bestAttackLoc != unit.loc) {
            _controller.SendMoveCommand(paths[bestAttackLoc]);
        }

        if(IsTeamWantsContact(_aiState, bestAttackTarget.team)) {
            //We want contact with this team so talk. We'll still schedule the attack, but
            //if talks end on friendly terms it will abort upon execution.
            ShowDiplomacy(_aiState, unit, bestAttackTarget);
        }

        Debug.Log("Attack with best attack index = " + bestIndex + " id = " + attack.id);

        _controller.ExecuteAttack(unit, bestAttackTarget, attack.id, counter.HasValue ? counter.Value.id : null);
        return true;
    }

    public bool UnitMoveToward(Unit unit, Dictionary<Loc, Pathfind.Path> paths, Loc dst)
    {

        if(paths.ContainsKey(dst)) {
            _controller.SendMoveCommand(paths[dst]);
            return true;
        }

        Dictionary<Loc, Pathfind.Path> expandedPaths = null;

        //keep expanding our search radius trying to find a path to the destination.
        for(int i = 2; i < 4; ++i) {
            expandedPaths = Pathfind.FindPaths(_controller, unit.unitInfo, unit.unitInfo.movement*i, new Pathfind.PathOptions() { ignoreZocs = true, moveThroughEnemies = true, excludeOccupied = false, forbiddenLocs = _aiState.forbiddenLocs, maxDepth = unit.team.maxDepth });
            if(expandedPaths.ContainsKey(dst)) {
                break;
            }
        }

        if(expandedPaths.ContainsKey(dst) == false) {
            Loc moveTo = Loc.invalid;
            int closestDist = Tile.DistanceBetween(unit.loc, dst);
            foreach(var p in expandedPaths) {
                int dist = Tile.DistanceBetween(p.Key, dst);
                if(dist < closestDist) {
                    closestDist = dist;
                    moveTo = p.Key;
                }
            }

            if(moveTo.valid) {
                dst = moveTo;
            }
        }

        if(expandedPaths.ContainsKey(dst)) {
            var bestPath = expandedPaths[dst];
            Loc furthestStep = unit.loc;
            foreach(Loc step in bestPath.steps) {
                if(paths.ContainsKey(step)) {
                    furthestStep = step;
                } else {
                    //getting to a spot adjacent to somewhere further along
                    //the path is better than being at an earlier spot on the path.
                    //This stops a 'single file' effect.
                    var adj = Tile.AdjacentLocs(step);
                    foreach(Loc a in adj) {
                        if(paths.ContainsKey(a) && bestPath.steps.Contains(a) == false) {
                            furthestStep = a;
                            break;
                        }
                    }
                }
            }

            if(paths.ContainsKey(furthestStep)) {
                _controller.SendMoveCommand(paths[furthestStep]);
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }


    public virtual AIState CreateAIState(MapGenerator.KingdomSpawn info, TeamInfo teamInfo)
    {
        AIState result = new AIState() {
            keepLoc = info.keepLoc,
            villageClaims = info.spawnedVillages,
            coreVillages = new List<Loc>(info.spawnedVillages),
            totalVillagesGoal = info.villageConquestGoal + info.spawnedVillages.Count,
            newClaimSize = info.newClaimSize,
            exclaves = info.exclaveLocs,
            teamNumber = GameController.instance.aiStates.Count,
        };

        if(result.teamNumber >= GameController.instance.teams.Count) {
            Debug.LogErrorFormat("AIState has bad index: {0}/{1}", result.teamNumber, GameController.instance.teams.Count);
        }

        List<Loc> centers = new List<Loc>(info.spawnedVillages);
        centers.Add(info.keepLoc);

        foreach(Loc center in centers) {
            for(int i = 0; i <= info.villageHostileRadius; ++i) {
                foreach(Loc loc in Tile.GetTilesInRing(center, i)) {
                    result.territoryClaims.Add(loc);
                }
            }
        }

        result.coreTerritory = new HashSet<Loc>(result.territoryClaims);

        return result;
    }

    public bool UnitThinkMessengerToPlayer(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        Debug.Log("MESSENGER SEEKING PLAYER! " + unit.teamInfo.team.teamNameAsProperNoun);
        if(GameController.instance.playerTeamInfo.lastRecruitLoc.valid) {
            return UnitMoveToward(unit, paths, GameController.instance.playerTeamInfo.lastRecruitLoc);
        }

        Unit playerRuler = GameController.instance.playerTeamInfo.GetRuler();
        if(playerRuler != null) {
            return UnitMoveToward(unit, paths, playerRuler.loc);
        }

        return false;
    }

    public bool UnitThinkGoAfterPlayer(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        Debug.Log("Aggro UnitThink: " + unit.loc);

        //unit.debugHighlight = true;
        //unit.debugHighlightColor = new Color(0f, 0f, 1f, 0.5f);

        //capture player villages if we can.
        if(unit.unitInfo.level >= 1) {
            foreach(var p in paths) {
                Tile t = GameController.instance.map.GetTile(p.Key);
                if(t.terrain.terrain.rules.capturable) {
                    var owner = GameController.instance.gameState.GetTeamOwnerOfLoc(p.Key);
                    if(owner != null && owner.player) {
                        _controller.SendMoveCommand(p.Value);
                        return true;
                    }
                }
            }
        }

        if(_aiState.targetLoc.valid && _vision.Contains(_aiState.targetLoc) == false) {
            return UnitMoveToward(unit, paths, _aiState.targetLoc);
        }

        if(_visibleEnemies.Count > 0) {
            int closestDistance = 0;
            Unit closestUnit = null;
            foreach(Unit enemyUnit in _visibleEnemies) {
                if(enemyUnit.team.barbarian) {
                    continue;
                }

                int dist = Tile.DistanceBetween(unit.loc, enemyUnit.loc);
                if(closestUnit == null || dist < closestDistance) {
                    closestDistance = dist;
                    closestUnit = enemyUnit;
                }
            }

            if(closestUnit != null) {
                return UnitMoveToward(unit, paths, closestUnit.loc);
            }
        }

        if(_visibleEnemyVillages.Count > 0) {
            int closestDistance = 0;
            Loc closestVillage = Loc.invalid;
            foreach(Loc village in _visibleEnemyVillages) {
                int dist = Tile.DistanceBetween(unit.loc, village);
                if(closestVillage.valid == false || dist < closestDistance) {
                    closestDistance = dist;
                    closestVillage = village;
                }
            }

            return UnitMoveToward(unit, paths, closestVillage);
        }

        if(unit.team.barbarian == false) {
            //unit.debugHighlightColor = new Color(1f, 0f, 0f, 0.5f);

            Debug.Log("Aggro move to: " + GameController.instance.playerTeamInfo.lastRecruitLoc.ToString());
            if(GameController.instance.playerTeamInfo.lastRecruitLoc.valid) {
                return UnitMoveToward(unit, paths, GameController.instance.playerTeamInfo.lastRecruitLoc);
            }
        }

        //spread out and search the fog.
        Loc locClosestToFog = Loc.invalid;
        int locDistFromFog = 100;
        foreach(KeyValuePair<Loc, Pathfind.Path> p in paths) {
            Loc[] adj = Tile.AdjacentLocs(p.Key);
            bool atEdge = false;
            foreach(Loc a in adj) {
                if(paths.ContainsKey(a) == false) {
                    atEdge = true;
                    break;
                }
            }

            if(atEdge == false) {
                continue;
            }

            for(int i = 2; i < locDistFromFog; ++i) {
                List<Loc> ring = Tile.GetTilesInRadius(p.Key, i);
                foreach(Loc r in ring) {
                    if(_vision.Contains(r) == false) {
                        locClosestToFog = p.Key;
                        locDistFromFog = i;
                        break;
                    }
                }
            }
        }

        if(paths.ContainsKey(locClosestToFog)) {
            _controller.SendMoveCommand(paths[locClosestToFog]);
            return true;
        }

        return false;
    }
}
