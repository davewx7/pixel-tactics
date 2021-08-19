using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TerrainInfo
{
    public TerrainInfo(Terrain t, Terrain g=null)
    {
        terrain = t;

        if(g != null && t.hasBase) {
            //We only allow setting the ground if this
            //terrain is set up to accept ground under it.
            groundOverride = g;
        } else {
            groundOverride = null;
        }
    }

    public bool Equals(TerrainInfo o)
    {
        return terrain == o.terrain && groundOverride == o.groundOverride;
    }

    public Terrain terrain;
    public Terrain groundOverride;

    public bool hasOverlay {
        get {
            return terrain.hasOverlay;
        }
    }

    public Sprite[] overlaySprites {
        get {
            return terrain.overlaySprites;
        }
    }


    public int zorder {
        get {
            return terrain.zorder;
        }
    }

    public int adjZorder {
        get {
            return terrain.adjZorder;
        }
    }

    public static TerrainInfo None {
        get {
            return new TerrainInfo();
        }
    }

    public bool valid {
        get {
            return terrain != null;
        }
    }

    public bool invalid {
        get {
            return terrain == null;
        }
    }

    public float yadj {
        get {
            return terrain.yadj;
        }
    }

    public float waterline {
        get {
            return terrain.waterline;
        }
    }


    public Terrain ground {
        get {
            return groundOverride != null ? groundOverride : terrain;
        }
    }

    public Sprite[] sprites {
        get {
            return ground.sprites;
        }
    }

    public bool HasAdjacent(int dir)
    {
        return terrain.HasAdjacent(dir) || (groundOverride != null && groundOverride.HasAdjacent(dir));
    }

    public Sprite GetAdjacent(int dir, int nadj)
    {
        if(terrain.adjacent != null && terrain.adjacent.Length > 0) {
            //if the primary terrain has adjacency directly, use it, as it
            //means it is important building adjacency.
            return terrain.GetAdjacent(dir, nadj);
        }

        return ground.GetAdjacent(dir, nadj);
    }

    public TerrainInfo GetSeasonalTerrain(Season season, bool freshwater)
    {
        TerrainInfo result = new TerrainInfo() {
            terrain = terrain.GetSeasonalTerrain(season, freshwater),
        };

        if(groundOverride != null) {
            result.groundOverride = groundOverride.GetSeasonalTerrain(season, freshwater);
        }

        return result;
    }

    public TerrainRules rules {
        get {
            return terrain.rules;
        }
    }

    public VillageInfo villageInfo {
        get {
            return terrain.villageInfo;
        }
    }

}

[CreateAssetMenu(menuName = "Wesnoth/Terrain")]
public class Terrain : GWScriptableObject
{

    public TerrainRules rules;
    public VillageInfo villageInfo;

    [SerializeField]
    Terrain _winterTerrain = null;

    [SerializeField]
    Terrain _autumnTerrain = null;

    public Terrain GetSeasonalTerrain(Season season, bool freshwater)
    {
        if(season == Season.Winter && _winterTerrain != null && (freshwater || _winterTerrainFreshwaterOnly == false)) {
            return _winterTerrain;
        } else if(season == Season.Autumn && _autumnTerrain != null) {
            return _autumnTerrain;
        }

        return this;
    }

    [SerializeField]
    bool _winterTerrainFreshwaterOnly = false;

    public string englishName;
    public Sprite[] sprites {
        get {
            if(_sprites.Length == 0 && _base != null) {
                return _base.sprites;
            }

            return _sprites;
        }
    }

    public void ClearSprites()
    {
        _sprites = new Sprite[0];
    }

    public void AddSprite(Sprite sprite)
    {
        var newSprites = new Sprite[_sprites.Length+1];
        for(int i = 0; i != _sprites.Length; ++i) {
            newSprites[i] = _sprites[i];
        }
        newSprites[_sprites.Length] = sprite;
        _sprites = newSprites;
    }

    [SerializeField]
    int _adjZorder = -1;

    public int adjZorder {
        get {
            if(_adjZorder != -1) {
                return _adjZorder;
            }

            return zorder;
        }
    }

    public int zorder = 0;
    public Sprite[] adjacent;

    public Sprite[] adjacent2;
    public Sprite[] adjacent3;
    public Sprite[] adjacent4;
    public Sprite[] adjacent5;
    public Sprite adjacent6;

    public bool HasAdjacent(int dir)
    {
        if(_base != null) {
            return _base.HasAdjacent(dir);
        }

        return adjacent != null && adjacent.Length > dir;
    }

    public Sprite GetAdjacent(int dir, int nadj)
    {
        Sprite[] adj = null;
        switch(nadj) {
            case 1: adj = adjacent; break;
            case 2: adj = adjacent2; break;
            case 3: adj = adjacent3; break;
            case 4: adj = adjacent4; break;
            case 5: adj = adjacent5; break;
            case 6: return adjacent6;
        }

        if(adj == null || dir >= adj.Length) {
            if(_base != null) {
                return _base.GetAdjacent(dir, nadj);
            }

            return null;
        }

        return adj[dir];
    }

    public void SetAdjacent(int dir, int nadj, Sprite sprite)
    {
        switch(nadj) {
            case 1: if(adjacent == null) adjacent = new Sprite[6]; adjacent[dir] = sprite; break;
            case 2: if(adjacent2 == null) adjacent2 = new Sprite[6]; adjacent2[dir] = sprite; break;
            case 3: if(adjacent3 == null) adjacent3 = new Sprite[6]; adjacent3[dir] = sprite; break;
            case 4: if(adjacent4 == null) adjacent4 = new Sprite[6]; adjacent4[dir] = sprite; break;
            case 5: if(adjacent5 == null) adjacent5 = new Sprite[6]; adjacent5[dir] = sprite; break;
            case 6: adjacent6 = sprite; break;
        }
    }


    [SerializeField]
    Sprite[] _sprites;

    public bool hasOverlay { get { return overlaySprites != null && overlaySprites.Length > 0; } }

    public Sprite[] overlaySprites;

    public void ClearOverlaySprites()
    {
        overlaySprites = new Sprite[0];
    }

    public void AddOverlaySprite(Sprite sprite)
    {
        var newSprites = new Sprite[overlaySprites.Length+1];
        for(int i = 0; i != overlaySprites.Length; ++i) {
            newSprites[i] = overlaySprites[i];
        }
        newSprites[overlaySprites.Length] = sprite;
        overlaySprites = newSprites;
    }


    public Sprite[] castleWalls;

    public void EnsureCastleWallsInit()
    {
        if(castleWalls == null || castleWalls.Length != 12) {
            castleWalls = new Sprite[12];
        }
    }

    public bool hasCastleWalls { get { return castleWalls != null && castleWalls.Length == 12; } }

    public Sprite CastleConcaveBL { get { return castleWalls[0]; } }
    public Sprite CastleConcaveBR { get { return castleWalls[1]; } }
    public Sprite CastleConcaveL { get { return castleWalls[2]; } }
    public Sprite CastleConcaveR { get { return castleWalls[3]; } }
    public Sprite CastleConcaveTL { get { return castleWalls[4]; } }
    public Sprite CastleConcaveTR { get { return castleWalls[5]; } }
    public Sprite CastleConvexBL { get { return castleWalls[6]; } }
    public Sprite CastleConvexBR { get { return castleWalls[7]; } }
    public Sprite CastleConvexL { get { return castleWalls[8]; } }
    public Sprite CastleConvexR { get { return castleWalls[9]; } }
    public Sprite CastleConvexTL { get { return castleWalls[10]; } }
    public Sprite CastleConvexTR { get { return castleWalls[11]; } }


    [SerializeField]
    Terrain _base = null;

    public bool hasBase {
        get {
            return _base != null;
        }
    }

    public void SetBase(Terrain t)
    {
        _base = t;
    }

    public float yadj = 0f;
    public float waterline = 0f;
}
