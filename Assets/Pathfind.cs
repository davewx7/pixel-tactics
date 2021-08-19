using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;

public class Pathfind : MonoBehaviour
{
    [System.Serializable]
    public class Path
    {
        public List<Loc> steps = new List<Loc>();
        public int cost = 0;
        public Tile destTile {
            get {
                return GameController.instance.map.GetTile(steps[steps.Count-1]);
            }
        }

        public Path Clone()
        {
            Path result = new Path();
            foreach(Loc step in steps) {
                result.steps.Add(step);
            }
            result.cost = cost;
            return result;
        }

        public Loc source {
            get { return steps[0]; }
        }

        public Loc dest {
            get { return steps[steps.Count-1]; }
        }
    }

    public class PathOptions
    {
        public Path start = null;
        public bool excludeSelf = false;
        public bool excludeOccupied = true;
        public bool recruit = false;
        public bool ignoreZocs = false;
        public bool moveThroughEnemies = false;
        public bool vision = false;
        public bool ignoreTerrainCosts = false;
        public int maxDepth = 2;
        public HashSet<Loc> forbiddenLocs = null;
    }

    public static PathOptions defaultOptions = new PathOptions();

    static ProfilerMarker s_profilePathfind = new ProfilerMarker("Pathfind.FindPaths");
    static ProfilerMarker s_profilePathfindZocs = new ProfilerMarker("Pathfind.FindZocs");
    static ProfilerMarker s_profilePathfindStep = new ProfilerMarker("Pathfind.Step");




    public static Dictionary<Loc, Path> FindPaths(GameController game, UnitInfo unit, int movementAllowance, PathOptions options = null)
    {
        ++_searchID;

        s_profilePathfind.Begin();

        if(options == null) {
            options = defaultOptions;
        }

        HashSet<Tile> zonesOfControl = null;


        if(options.ignoreZocs == false) {
            zonesOfControl = new HashSet<Tile>();
            s_profilePathfindZocs.Begin();
            foreach(Unit enemy in game.units) {
                if(enemy.UsesZocAgainst(unit) && options.recruit == false && options.ignoreZocs == false) {
                    foreach(Tile a in enemy.tile.adjacentTiles) {
                        if(zonesOfControl.Contains(a) == false) {
                            zonesOfControl.Add(a);
                        }
                    }
                }
            }
            s_profilePathfindZocs.End();
        }

        Dictionary<Loc, Path> result = new Dictionary<Loc, Path>();

        List<Path> live = new List<Path>();

        Path start = options.start;

        if(start == null) {
            start = new Path();
            start.steps.Add(unit.loc);
        }
        live.Add(start);

        result[start.dest] = start;

        //maximum depth we can search for in terms of underworld/overworld.
        //A unit in the overworld can't move into the underworld unless it's on a gate already.
        int maxDepth = 1;

        if(start.destTile.underworldGate || start.destTile.loc.underworld || options.vision == false) {
            maxDepth = 2;
        }

        if(maxDepth > options.maxDepth) {
            maxDepth = options.maxDepth;
        }

        while(live.Count > 0) {
            s_profilePathfindStep.Begin();
            List<Path> nextLive = new List<Path>();
            foreach(Path p in live) {
                Tile srcTile = p.destTile;
                foreach(Tile.Edge edge in srcTile.edges) {
                    if(game.IsPathable(unit, edge) == false || edge.dest.loc.depth > maxDepth) {
                        continue;
                    }

                    if(options.forbiddenLocs != null && options.forbiddenLocs.Contains(edge.dest.loc)) {
                        continue;
                    }

                    if(edge.dest.pathInfo.searchID != _searchID) {
                        edge.dest.pathInfo.searchID = _searchID;
                        edge.dest.pathInfo.cost = unit.MoveCost(edge.dest, options.vision);
                    }

                    int moveCost = edge.dest.pathInfo.cost;

                    if(options.ignoreTerrainCosts) {
                        moveCost = 1;
                    }

                    int cost = p.cost + moveCost;
                    if(options.recruit) {
                        if(edge.dest.terrain.rules.castle == false) {
                            continue;
                        }

                        cost = 0;
                    }

                    if(cost > movementAllowance) {
                        continue;
                    }

                    if(zonesOfControl != null && zonesOfControl.Contains(edge.dest)) {
                        cost = movementAllowance;
                    }

                    Path existing;
                    if(result.TryGetValue(edge.dest.loc, out existing) && existing.cost <= cost) {
                        continue;
                    }

                    Unit otherUnit = edge.dest.unit;
                    if(otherUnit != null && otherUnit.unitInfo.IsAlly(unit) == false && options.moveThroughEnemies == false) {
                        continue;
                    }

                    Path newPath = p.Clone();
                    newPath.cost = cost;
                    newPath.steps.Add(edge.dest.loc);
                    result[edge.dest.loc] = newPath;

                    nextLive.Add(newPath);
                }
            }

            live = nextLive;

            s_profilePathfindStep.End();
        }

        if(options.excludeOccupied) {
            Dictionary<Loc, Path> filteredResult = new Dictionary<Loc, Path>();
            foreach(KeyValuePair<Loc, Path> p in result) {
                Tile dest = game.map.GetTile(p.Value.dest);
                if(dest.unit == null || dest.unit.unitInfo == unit) {
                    filteredResult.Add(p.Key, p.Value);
                }
            }

            result = filteredResult;
        }

        if(options.excludeSelf || options.recruit) {
            result.Remove(unit.loc);
        }

        s_profilePathfind.End();

        return result;
    }

    public struct TilePathInfo
    {
        public TilePathInfo(int existingCost, Tile previousTile, int heuristicCostValue)
        {
            searchID = _searchID;
            cost = existingCost;
            heuristicCost = heuristicCostValue;
            prevTile = previousTile;
        }
        public int searchID;
        public int heuristicCost;
        public int cost;
        public Tile prevTile;
    }

    static int _searchID = 1;

    struct QueueItem
    {
        public QueueItem(Tile t)
        {
            tile = t;
            cost = t.pathInfo.cost;
        }
        public bool IsStillValid()
        {
            return cost == tile.pathInfo.cost;
        }
        public Tile tile;
        public int cost;
    }

    public static Path FindPathTo(Tile source, Tile goal, System.Func<Tile.Edge, int> costFunc, System.Func<Tile,Tile,int> heuristicFunc)
    {
        ++_searchID;

        source.pathInfo = new TilePathInfo(0, null, heuristicFunc(source, goal));

        Glowwave.PriorityQueue<QueueItem> q = new Glowwave.PriorityQueue<QueueItem>((QueueItem a, QueueItem b) => (a.cost + a.tile.pathInfo.heuristicCost) - (b.cost + b.tile.pathInfo.heuristicCost));
        q.Push(new QueueItem(source));

        //Debug.Log("Pathfinding from " + source.loc + " to " + goal.loc);

        while(q.Count > 0) {
            QueueItem item = q.Pop();
            if(item.IsStillValid() == false) {
                continue;
            }

            //Debug.Log("Processing valid item at " + item.tile.loc);

            foreach(Tile.Edge edge in item.tile.edges) {
                int cost = item.cost + costFunc(edge);
                Tile t = edge.dest;

                if(t == goal) {
                    //arrived at the destination, so return path.
                    Path path = new Path() {
                        cost = cost,
                    };

                    path.steps.Add(t.loc);

                    for(Tile tile = item.tile; tile != null; tile = tile.pathInfo.prevTile) {
                        path.steps.Add(tile.loc);
                    }

                    path.steps.Reverse();
                    return path;
                }

                if(t.pathInfo.searchID == _searchID && t.pathInfo.cost <= cost) {
                    //existing way of getting to this tile is cheaper, so not interesting.
                    continue;
                }

                t.pathInfo = new TilePathInfo(cost, item.tile, heuristicFunc(t, goal));
                q.Push(new QueueItem(t));
            }
        }

        return null;
    }

    public class Region
    {
        public HashSet<Loc> locs = new HashSet<Loc>();
    }

    public delegate bool IsRegionDelegate(Loc loc);

    static public List<Region> FindRegions(IsRegionDelegate pred, Loc dim)
    {
        bool[] items = new bool[dim.x*dim.y];
        for(int y = 0; y != dim.y; ++y) {
            for(int x = 0; x != dim.x; ++x) {
                Loc loc = new Loc(x, y);
                items[y*dim.x + x] = pred(loc);
            }
        }

        return FindRegions(items, dim);
    }

    static public List<Tile> FindRegionFromTile(Tile tile, System.Func<Tile,bool> pred)
    {
        List<Tile> result = new List<Tile>();
        result.Add(tile);

        List<Tile> live = new List<Tile>();
        live.Add(tile);

        while(live.Count != 0) {
            List<Tile> current = live;
            live = new List<Tile>();

            foreach(Tile t in current) {
                foreach(Tile adj in t.adjacentTiles) {
                    if(adj != null && result.Contains(adj) == false && pred(adj)) {
                        live.Add(adj);
                        result.Add(adj);
                    }
                }
            }
        }

        return result;
    }

    static public List<Region> FindRegions(bool[] inregion, Loc dim)
    {
        var result = new List<Region>();
        var seen = new Dictionary<Loc, bool>();

        for(int y = 0; y != dim.y; ++y) {
            for(int x = 0; x != dim.x; ++x) {
                Loc loc = new Loc(x, y);
                if(!inregion[y*dim.x + x] || seen.ContainsKey(loc)) {
                    continue;
                }

                Region region = new Region();
                HashSet<Loc> active = new HashSet<Loc>();
                active.Add(loc);

                while(active.Count > 0) {
                    HashSet<Loc> nextActive = new HashSet<Loc>();

                    foreach(Loc activeLoc in active) {
                        Loc[] adj = Tile.AdjacentLocs(activeLoc);
                        foreach(Loc a in adj) {
                            if(Tile.IsLocOnBoard(a, dim) == false || !inregion[a.y*dim.x + a.x] || region.locs.Contains(a) || active.Contains(a) || nextActive.Contains(a)) {
                                continue;
                            }

                            nextActive.Add(a);
                        }
                    }

                    foreach(Loc activeLoc in active) {
                        seen[activeLoc] = true;
                        region.locs.Add(activeLoc);
                    }

                    active = nextActive;
                }

                result.Add(region);
            }
        }

        result.Sort((a, b) => (b.locs.Count.CompareTo(a.locs.Count)));

        return result;
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
