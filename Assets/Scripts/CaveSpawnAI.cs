using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/CaveSpawnAI")]
public class CaveSpawnAI : AggroAI
{
    bool winter {
        get {
            return GameController.instance.currentSeason == Season.Winter;
        }
    }

    public override void NewTurn(AIState state)
    {
        base.NewTurn(state);
    }

    public override void Play(AIState aiState)
    {
        base.Play(aiState);
    }

    public override bool UnitThink(Unit unit, Dictionary<Loc, Pathfind.Path> paths)
    {
        if(winter == false && unit.loc.underworld) {
            //not winter, just move off the castle, then try to move and attack
            //and stay in the underworld.

            List<Loc> removeLocs = new List<Loc>();
            foreach(var p in paths) {
                if(p.Key.overworld || Tile.DistanceBetween(p.Key, _aiState.gateToOverworld) <= 1) {
                    removeLocs.Add(p.Key);
                }
            }

            foreach(var loc in removeLocs) {
                paths.Remove(loc);
            }

            if(TryLevelUp(unit, paths)) {
                return true;
            }

            if(TryAttack(unit, paths)) {
                return true;
            }

            if(unit.tile.terrain.rules.castle == false) {
                return false;
            }

            Loc dest = Loc.invalid;
            int nindex = GameController.instance.rng.Range(0, paths.Count);

            foreach(var p in paths) {
                if(nindex == 0) {
                    dest = p.Key;
                    break;
                }

                --nindex;
            }

            if(dest.valid) {
                GameController.instance.SendMoveCommand(paths[dest]);
                return true;
            }

            return false;
        } else if(winter && unit.loc.underworld) {
            //Move toward the gate to the overworld, or out into the overworld if we can.
            List<Loc> underworldLocs = new List<Loc>();
            foreach(var p in paths) {
                if(p.Key.underworld) {
                    underworldLocs.Add(p.Key);
                }
            }

            if(underworldLocs.Count < paths.Count) {
                //we can get to the overworld, so remove all the underworld options and have the unit move out onto the overworld.
                foreach(var loc in underworldLocs) {
                    paths.Remove(loc);
                }

                return base.UnitThink(unit, paths);
            } else {
                //Move toward the overworld now.
                return UnitMoveToward(unit, paths, _aiState.gateToOverworld);
            }
        }

        return base.UnitThink(unit, paths);
    }
}
