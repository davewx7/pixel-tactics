using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHint : MonoBehaviour
{
    public string text;

    [SerializeField]
    bool _requireSelectedUnit = true;


    [SerializeField]
    bool _requireOurUnit = true;

    [SerializeField]
    bool _requireLevelUp = false;

    [SerializeField]
    bool _requireCanLevelUp = false;

    [SerializeField]
    bool _requireTargetUIActive = false;

    [SerializeField]
    bool _requireNoAttackWarnings = false;

    [SerializeField]
    bool _requireInjured = false;

    public bool recruitmentHint = false;

    public RectTransform targetUIElement;

    public GameCommand commandExpiresThisHint;

    public bool CanShow(Unit unit)
    {
        if(recruitmentHint) {
            Unit ruler = GameController.instance.playerTeamInfo.GetRuler();
            if(ruler == null || ruler.tile.terrain.rules.keep == false) {
                return false;
            }

            if(GameController.instance.playerTeamInfo.numUnits > 1) {
                return false;
            }

            Dictionary<Loc, Pathfind.Path> paths = Pathfind.FindPaths(GameController.instance, ruler.unitInfo, 1, new Pathfind.PathOptions() {
                recruit = true,
                excludeOccupied = true,
            });

            bool foundVacant = false;
            foreach(var p in paths) {
                if(GameController.instance.GetUnitAtLoc(p.Key) == null) {
                    foundVacant = true;
                    break;
                }
            }

            if(foundVacant == false) {
                return false;
            }
        }

        if(_requireTargetUIActive && (targetUIElement == null || targetUIElement.gameObject.activeSelf == false)) {
            return false;
        }

        if(_requireNoAttackWarnings && (GameController.instance.attackWarningsComplete == false || GameController.instance.attackWarnings.Count > 0)) {
            return false;
        }

        if(unit == null) {
            return _requireSelectedUnit == false;
        }

        if(_requireOurUnit && unit.team != GameController.instance.playerTeam) {
            return false;
        }

        if(_requireLevelUp && unit.hasEnoughExperienceToLevelUp == false) {
            return false;
        }

        if(_requireCanLevelUp && unit.canLevelUp == false) {
            return false;
        }

        if(_requireInjured && unit.unitInfo.hitpointsRemaining > unit.unitInfo.hitpointsMax/3) {
            return false;
        }

        return true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
