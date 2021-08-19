using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileType : GWScriptableObject
{
    public float flightTime = 0.4f;

    //distance the projectile should travel. 100% = entire hex.
    public float distanceTravel = 1f;

    public AnimInfo animFail = null;
    public AnimInfo animImpact = null;
    public AnimInfo[] animations;

    public AnimInfo GetAnimation(Tile.Direction dir, out float rotation)
    {
        int n = (int)dir;
        if(n < animations.Length && AnimInfo.IsValid(animations[n])) {
            rotation = 0f;
            return animations[n];
        } else if(animations.Length >= 2 && AnimInfo.IsValid(animations[1]) && Tile.DirectionIsDiagonal(dir)) {
            rotation = -(n - 1)*60f;
            return animations[1];
        } else {
            rotation = -n*60f;
            return animations[0];
        }
    }
}
