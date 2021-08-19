using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGenerator : MonoBehaviour
{
    [SerializeField]
    GameController _controller = null;

    [SerializeField]
    Unit _unitPrefab = null;

    void Awake()
    {
        int index = 0;
        foreach(TeamInfo teamInfo in _controller.teams) {
            Team team = teamInfo.team;
            if(team.rulerType != null) {
                UnitInfo unitInfo = team.rulerType.createUnit();
                unitInfo.ruler = true;
                unitInfo.nteam = index;
                Unit unit = Instantiate(_unitPrefab, _controller.transform);
                unit.unitInfo = unitInfo;
                unit.loc = _controller.FindVacantTileNear(new Loc(2,2));

                _controller.AddUnit(unit);
            }

            ++index;
        }
    }
}
