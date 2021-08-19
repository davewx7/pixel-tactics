using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/BarbarianTeamSpawn")]
public class BarbarianTeamSpawn : BaseSpawnEvent
{
    [System.Serializable]
    struct Entry
    {
        public UnitType unitType;
        public List<TerrainRules> terrainRules;
        public int duplicates;
    }

    [SerializeField]
    List<Entry> _entries = null;

    public override bool TrySpawn()
    {
        Debug.Log("BarbarianTeamSpawn...");
        if(GameController.instance.gameState.nround < _round) {
            return false;
        }

        int nteam = 0;
        HashSet<Loc> teamCombinedVision = new HashSet<Loc>();
        foreach(TeamInfo team in GameController.instance.teams) {
            if(team.team.barbarian == false) {
                HashSet<Tile> vision = GameController.instance.CalculateVision(nteam);
                foreach(Tile t in vision) {
                    teamCombinedVision.Add(t.loc);
                }
            }

            ++nteam;
        }

        Debug.Log("BarbarianTeamSpawn on " + GameController.instance.gameState.neutralVillages.Count + " villages");

        foreach(Loc villageLoc in GameController.instance.gameState.neutralVillages) {
            List<Loc> possibleTiles = Tile.GetTilesInRadius(villageLoc, 2);
            int tileIndex = GameController.instance.rng.Next(possibleTiles.Count);
            Loc spawnLoc = possibleTiles[tileIndex];
            Loc vacantLoc = GameController.instance.FindVacantTileNear(spawnLoc);
            if(teamCombinedVision.Contains(vacantLoc) || GameController.instance.map.LocOnBoard(vacantLoc) == false) {
                continue;
            }

            Tile spawnTile = GameController.instance.map.GetTile(vacantLoc);
            if(spawnTile.terrain.rules.moveCost > 3) {
                continue;
            }

            List<Entry> unitTypes = new List<Entry>();

            foreach(var entry in _entries) {
                if(entry.terrainRules.Contains(spawnTile.terrain.rules)) {
                    unitTypes.Add(entry);
                }
            }

            if(unitTypes.Count == 0) {
                continue;
            }

            int unitIndex = GameController.instance.rng.Next(unitTypes.Count);

            for(int i = 0; i < 1 + unitTypes[unitIndex].duplicates; ++i) {
                Loc finalLoc = GameController.instance.FindVacantTileNear(vacantLoc);
                if(teamCombinedVision.Contains(finalLoc) || GameController.instance.map.LocOnBoard(finalLoc) == false) {
                    continue;
                }
                UnitInfo unitInfo = unitTypes[unitIndex].unitType.createUnit();
                unitInfo.nteam = GameController.instance.gameState.numBarbarianTeam;
                unitInfo.loc = finalLoc;
                GameController.instance.ExecuteSpawnUnit(unitInfo);
            }
        }

        return true;
    }
}
