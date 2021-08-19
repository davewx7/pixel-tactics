using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSpawnEvent : GWScriptableObject
{
    [SerializeField]
    protected int _round = 0;

    public virtual bool TrySpawn()
    {
        return false;
    }
}

[CreateAssetMenu(menuName = "Wesnoth/SpawnTeamEvent")]
public class SpawnTeamEvent : BaseSpawnEvent
{
    [SerializeField]
    int _startingGold = 0;

    [SerializeField]
    int _baseIncome = 0;

    [SerializeField]
    int _autoCaptureVillageRadius = 0;

    [SerializeField]
    Team _team = null;

    [SerializeField]
    Team _targetTeam = null;

    [SerializeField]
    int _minDistance = 8, _maxDistance = 12;

    [SerializeField]
    bool _preferShortDistance = false;

    [SerializeField]
    int _maxTravelCost = 20;

    [SerializeField]
    int _minDistanceFromOtherKeeps = 10;

    [SerializeField]
    Terrain _keepTerrain = null, _castleTerrain = null;

    [SerializeField]
    List<TerrainRules> possibleTerrain = null;

    [SerializeField]
    int _castleSize = 1;

    [SerializeField]
    bool _revertTerrainOnRulerDeath = false;

    [SerializeField]
    StoryEventInfo _defeatTeamStoryEvent = null;

    public override bool TrySpawn()
    {
        if(GameController.instance.gameState.nround < _round) {
            return false;
        }

        TeamInfo targetTeamInfo = GameController.instance.gameState.GetTeamInfo(_targetTeam);
        if(targetTeamInfo == null) {
            return false;
        }

        Unit targetRuler = targetTeamInfo.GetRuler();
        if(targetRuler == null) {
            return false;
        }

        List<Loc> possibleLocs = new List<Loc>();
        for(int i = _minDistance; i <= _maxDistance; ++i) {
            Loc[] ring = Tile.GetTilesInRing(targetRuler.loc, i);
            foreach(Loc loc in ring) {
                if(GameController.instance.map.LocOnBoard(loc)) {
                    possibleLocs.Add(loc);
                }
            }
        }

        Debug.Log("WHILE TRYING TO SPAWN...");
        while(possibleLocs.Count > 0) {
            Debug.Log("TRYING TO SPAWN WITH possibleLocs = " + possibleLocs.Count);

            int index = 0;

            if(_preferShortDistance == false) {
                index = GameController.instance.rng.Next(0, possibleLocs.Count);
            }

            List<Loc> validCastleLocs = new List<Loc>();
            if(IsLocLegal(possibleLocs[index], targetTeamInfo, targetRuler, ref validCastleLocs)) {

                List<StoryEventInfo.TerrainChange> terrainReversion = new List<StoryEventInfo.TerrainChange>();

                Tile tile = GameController.instance.map.GetTile(possibleLocs[index]);

                if(_keepTerrain != null) {
                    terrainReversion.Add(new StoryEventInfo.TerrainChange() {
                        terrain = tile.terrain,
                        loc = tile.loc,
                    });

                    tile.terrain = new TerrainInfo(_keepTerrain);
                    tile.CalculatePosition();
                }

                if(_castleTerrain != null) {
                    for(int i = 0; i < _castleSize; ++i) {
                        int castleIndex = GameController.instance.rng.Next(0, validCastleLocs.Count);
                        Tile castleTile = GameController.instance.map.GetTile(validCastleLocs[castleIndex]);
                        terrainReversion.Add(new StoryEventInfo.TerrainChange() {
                            terrain = castleTile.terrain,
                            loc = castleTile.loc,
                        });

                        validCastleLocs.RemoveAt(castleIndex);
                        castleTile.terrain = new TerrainInfo(_castleTerrain);
                        castleTile.CalculatePosition();
                    }
                }

                UnitInfo ruler = _team.rulerType.createUnit();
                ruler.ruler = true;
                ruler.nteam = GameController.instance.teams.Count;
                ruler.killedByPlayerEvent = _defeatTeamStoryEvent;

                if(_revertTerrainOnRulerDeath) {
                    ruler.killedByPlayerEvent.valid = true;
                    ruler.killedByPlayerEvent.terrainChanges = terrainReversion;
                }

                float[] goldMultByDifficulty = { 0.5f, 1.0f, 1.5f };
                float goldMultiplier = goldMultByDifficulty[GameController.instance.gameState.difficulty];

                TeamInfo teamInfo = new TeamInfo(_team);
                teamInfo.baseIncome = (int)(_baseIncome*goldMultiplier);
                teamInfo.gold = (int)(_startingGold*goldMultiplier);
                GameController.instance.gameState.teams.Add(teamInfo);

                if(_team.ai != null) {
                    Debug.Log("ADD AI STATE: " + GameController.instance.aiStates.Count + " WITH TEAM: " + teamInfo.team.teamName);
                    GameController.instance.aiStates.Add(new AIState() {
                        targetLoc = targetRuler.loc,
                        teamNumber = GameController.instance.aiStates.Count,
                    });

                    if(GameController.instance.gameState.teams.Count != GameController.instance.gameState.aiStates.Count) {
                        Debug.LogErrorFormat("AIState has bad index: {0}/{1}", GameController.instance.gameState.teams.Count, GameController.instance.gameState.aiStates.Count);
                    }
                } else {
                    GameController.instance.aiStates.Add(null);
                }

                foreach(Loc loc in Tile.GetTilesInRadius(possibleLocs[index], _autoCaptureVillageRadius)) {
                    if(GameController.instance.map.LocOnBoard(loc) && GameController.instance.gameState.GetOwnerOfLoc(loc) == -1) {
                        GameController.instance.ForceCaptureLoc(loc, ruler.ncontroller);
                    }
                }

                ruler.loc = possibleLocs[index];
                GameController.instance.ExecuteSpawnUnit(ruler);

                Debug.Log("Spawn loc: " + possibleLocs[index]);

                return true;
            }

            possibleLocs.RemoveAt(index);
        }

        return false;
    }

    bool IsLocLegal(Loc loc, TeamInfo targetTeam, Unit targetRuler, ref List<Loc> validAdj)
    {
        Tile tile = GameController.instance.map.GetTile(loc);
        if(tile == null || possibleTerrain.Contains(tile.terrain.rules) == false || tile.underworldGate) {
            return false;
        }

        if(tile.revealed) {
            return false;
        }

        int adjMatches = 0;
        foreach(Tile t in tile.adjacentTiles) {
            if(t == null || t.underworldGate) {
                return false;
            }

            if(t != null && t.revealed == false && t.loc.overworld == tile.loc.overworld && possibleTerrain.Contains(t.terrain.rules)) {
                validAdj.Add(t.loc);
                ++adjMatches;
            }
        }

        if(adjMatches < _castleSize) {
            return false;
        }

        List<Loc> locsNearKeep = Tile.GetTilesInRadius(loc, _minDistanceFromOtherKeeps);
        foreach(Loc possibleKeep in locsNearKeep) {
            Tile t = GameController.instance.map.GetTile(possibleKeep);
            if(t != null && t.terrain.rules.keep) {
                return false;
            }
        }

        Pathfind.Path path = Pathfind.FindPathTo(tile, targetRuler.tile,
            (Tile.Edge edge) => edge.dest.terrain.rules.moveCost, (Tile source, Tile dest) => Tile.DistanceBetween(source.loc, dest.loc));
        if(path == null || path.cost > _maxTravelCost) {
            return false;
        }

        return true;
    }
}
