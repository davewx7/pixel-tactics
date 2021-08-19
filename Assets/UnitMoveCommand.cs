using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using DG.Tweening;

[System.Serializable]
public class UnitMoveCommandInfo
{
    public Pathfind.Path path;
}

public class UnitMoveCommand : GameCommand
{
    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<UnitMoveCommandInfo>(data); }


    public UnitMoveCommandInfo info = new UnitMoveCommandInfo();

    static public Unit mostRecentlyMoved = null;

    Unit _unit = null;

    public bool visible {
        get {
            foreach(Loc loc in info.path.steps) {
                Tile tile = GameController.instance.map.GetTile(loc);
                if(tile.fogged == false) {
                    return true;
                }
            }

            return false;
        }
    }

    IEnumerator Execute()
    {
        PreMoveLogic();

        Vector3[] path = new Vector3[info.path.steps.Count-1];
        Tile.Direction[] directions = new Tile.Direction[info.path.steps.Count-1]; 

        for(int i = 1; i != info.path.steps.Count; ++i) {
            path[i-1] = GameController.instance.map.GetTile(info.path.steps[i]).unitPos;
            directions[i-1] = Tile.DirOfLoc(info.path.steps[i-1], info.path.steps[i]);
        }

        GameController.instance.RefreshUnitDisplayed();

        bool isVisible = visible;

        float durationPerStep = isVisible ? 0.25f : 0f;

        var tween = _unit.transform.DOPath(path, path.Length*durationPerStep, PathType.CatmullRom).SetEase(Ease.Linear);

        for(int i = 0; i != directions.Length; ++i) {
            var dir = directions[i];
            _unit.facing = dir;

            var loc = info.path.steps[i+1];
            _unit.hiddenInUnderworld = GameController.instance.map.IsTileHiddenInUnderworld(loc);

            GameController.instance.UnitMoveThroughLoc(_unit, loc, directions.Length-i);

            if(isVisible) {
                Tile srcTile = GameController.instance.map.GetTile(info.path.steps[i]);
                Tile dstTile = GameController.instance.map.GetTile(info.path.steps[i+1]);

                _unit.isOnVisibleLoc = srcTile.fog.atLeastPartlyVisibleToPlayer || dstTile.fog.atLeastPartlyVisibleToPlayer;

                yield return new WaitForSeconds(durationPerStep/GameController.timeScale);
            }
        }

        if(isVisible == false) {
            tween.Complete();
        }

        if(tween != null && tween.active) {
            yield return tween.WaitForCompletion();
        }

        PostMoveLogic();

        finished = true;

        //Trigger reselection of the unit at this point.
        if(GameController.instance.localPlayerTurn && (_unit.PossibleAttacks().Count > 0 || Pathfind.FindPaths(GameController.instance, _unit.unitInfo, _unit.unitInfo.movementRemaining).Count > 0)) {
            GameController.instance.TileClicked(GameController.instance.map.GetTile(_unit.loc));
        }

        GameController.instance.RefreshUnitDisplayed(_unit);
    }

    public void PreMoveLogic()
    {
        _unit = GameController.instance.GetUnitAtLoc(info.path.source);
        if(_unit == null) {
            Debug.LogErrorFormat("No known unit at {0}", info.path.source);
        }

        if(info.path.source.underworld != info.path.dest.underworld || GameController.instance.map.GetTile(info.path.dest).underworldGate) {
            //force a refresh of vision if going from overworld to underworld of vice versa.
            _unit.unitInfo.expendedVision = false;
        }

        DOTween.To(() => _unit.waterline, x => _unit.waterline = x, 0f, 0.2f);

        mostRecentlyMoved = _unit;

        _unit.unitInfo.ExpendMovement(info.path.cost);
    }

    public void PostMoveLogic()
    {
        _unit.loc = info.path.dest;
        _unit.isOnVisibleLoc = info.path.destTile.fog.atLeastPartlyVisibleToPlayer;
        GameController.instance.QueueUnitArriveAtDestination(_unit);
    }

    public override bool RunImmediately()
    {
        if(visible) {
            //This move is visible so it should get the entire co-routine treatment.
            return false;
        }

        PreMoveLogic();
        PostMoveLogic();
        return true;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(GameController.instance.GetUnitAtLoc(info.path.dest)) {
            Debug.Log("Unit cannot move to " + info.path.dest + " because it's occupied");
            finished = true;
            return;
        }
        StartCoroutine(Execute());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
