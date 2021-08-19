using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/CaveSpawn")]
public class CaveSpawn : GWScriptableObject
{
    public string rumorDescription;

    public List<UnitType> unitTypes;
    public List<int> budgetByYear;
    public List<CalendarMonth> spawnMonths;

    public int currentBudget {
        get {
            if(budgetByYear.Count == 0) {
                return 0;
            }

            int year = GameController.instance.currentYear;
            if(year >= budgetByYear.Count) {
                year = budgetByYear.Count-1;
            }

            float[] difficultyMultiplier = new float[] { 0.6f, 1.0f, 1.5f };

            return (int)(budgetByYear[year]*difficultyMultiplier[GameController.instance.gameState.difficulty]);
        }
    }
}

[System.Serializable]
public class CaveSpawnInfo
{
    public CaveSpawn caveSpawn;

    public int goldSpentThisTurn = 0;

    public Loc caveEntrance;

    public bool playerEntered = false;

    //if the player has a unit in the interior of the cave, no spawn takes place.
    public List<Loc> caveInteriorLocs = new List<Loc>();

    //If the player reaches this loc, the spawn is permanently deactivated.
    public Loc caveLootLoc;

    //The ruler of the cave. When killed the player has 'cleared' this cave.
    public string caveRulerGuid;
}