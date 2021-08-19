using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public int randomSeed = 100;

    [SerializeField]
    List<UnitType> _testUnitTypes = new List<UnitType>(), _testEnemyTypes = new List<UnitType>();

    [SerializeField]
    int _maxKingdoms = 1;

    [SerializeField]
    Team _barbarianTeam = null, _caveSpawnTeam = null;

    [SerializeField]
    Unit _unitPrefab = null;

    [SerializeField]
    Loot _goldLoot = null;

    [System.Serializable]
    public class TreasureCavernElement
    {
        public Loot loot;
        public UnitType unitType;
        public Terrain terrainOverride;
        public int minDepth = 0;
        public int minRadius = 0;
        public int maxRadius = 0;

        public bool spawnKeep = false;
    }

    [System.Serializable]
    public class TreasureCavern
    {
        public BoonUnderworld cave = null;

        public Terrain keepTerrain;
        public Terrain castleTerrain;
        public Team underworldTeam;

        public int numVillages;
        public Terrain villageTerrain;
    }

    public List<TreasureCavern> treasureCaverns = new List<TreasureCavern>();

    public List<TerrainRules> playerTerrainBackup = new List<TerrainRules>();

    [System.Serializable]
    public class KingdomSpawn
    {
        public string castleName;

        public int castleSize;
        public List<TerrainRules> preferredTerrain;
        public Terrain castleTerrain;
        public Terrain keepTerrain;
        public Terrain villageTerrain;
        public int numVillages;
        public int numVillagesStartingOwnership;
        public int maxVillageRadius = 20;
        public int villageClaim;
        public int villageHostileRadius;
        public int keepHostileRadius;

        //An exclave is a distant village of the same type as this kingdom, useful
        //as a quest for the player to help them reclaim.
        public int numExclaves = 0;

        public string exclaveName;

        public int exclaveRadius = 0;

        public List<UnitType> exclaveBarbarians = new List<UnitType>();

        public List<Loc> exclaveLocs = new List<Loc>();

        public float conquestRatio = 0f;

        //percent of units that will be dedicated to becoming aggressive with the player
        //if the player makes enemies with this side.
        public float aggroRatio = 0f;

        //Number of villages this kingdom would like to conquer in addition to its starting villages.
        public int villageConquestGoal = 0;

        //The number of villages claimed when making a new claim to land.
        public int newClaimSize = 5;

        public Team team;

        public List<UnitType> startingUnits = new List<UnitType>();

        [HideInInspector]
        public bool keepInit = false;

        [HideInInspector]
        public Loc keepLoc;

        [HideInInspector]
        public List<Pathfind.Region> possibleRegions = null;

        [HideInInspector]
        public int villageSearchRadius = 1;

        [HideInInspector]
        public List<Loc> spawnedVillages = new List<Loc>();

        [HideInInspector]
        public bool villageSpawnFailed = false;

        [System.NonSerialized]
        public List<KingdomSpawn> neighbors = new List<KingdomSpawn>();

        public float ScoreCastlePlacement(Loc loc, HashSet<Tile> tiles, int numNeighbors)
        {
            int matches = 0;
            foreach(Tile tile in tiles) {
                if(preferredTerrain.Contains(tile.terrain.rules)) {
                    ++matches;
                }
            }

            float multiplier = 1.0f;

            if(team.player) {
                multiplier = (float)Mathf.Min(4, numNeighbors);
            }

            return multiplier*((float)matches)/(float)tiles.Count;
        }

        public int scoreCastlePrecisePositioning(Loc loc)
        {
            int scanRadius = 3;
            int result = 0;
            List<Loc> locs = Tile.GetTilesInRadius(loc, scanRadius);
            foreach(Loc a in locs) {
                Tile t = GameController.instance.map.GetTile(a);
                if(t != null && preferredTerrain.Contains(t.terrain.rules)) {
                    result++;
                }
            }

            return result;
        }
    }

    [SerializeField]
    KingdomSpawn[] _kingdoms = null;

    [SerializeField]
    Terrain _dirtRoadTerrain = null, _pavedRoadTerrain = null;

    [SerializeField]
    Terrain[] _bridgeTerrain = null;

    [SerializeField]
    Terrain _oceanTerrain = null;

    [SerializeField]
    Terrain _coastTerrain = null;

    [SerializeField]
    Terrain _riverTerrain = null;

    [SerializeField]
    Terrain _grasslandTerrain = null;

    [SerializeField]
    Terrain _hillTerrain = null;

    [SerializeField]
    Terrain _mountainTerrain = null;

    [SerializeField]
    Terrain _forestTerrain = null;

    [SerializeField]
    Terrain _swampTerrain = null;

    [SerializeField]
    Terrain _caveFloorTerrain = null, _caveWallTerrain = null;

    HashSet<Loc> _freshWater = new HashSet<Loc>();


    class HeightMap
    {
        ConsistentRandom _rng;
        public float[] values;
        public Loc dim;

        public HeightMap(Loc dimensions, ConsistentRandom rng)
        {
            _rng = rng;
            dim = dimensions;
            values = new float[dim.x*dim.y];
        }

        public List<float> GenerateHistogram()
        {
            var result = new List<float>(values);
            result.Sort();
            return result;
        }

        public bool IsLocOnMap(Loc loc)
        {
            return loc.x >= 0 && loc.y >= 0 && loc.x < dim.x && loc.y < dim.y;
        }

        public int LocToIndex(Loc loc)
        {
            return loc.y*dim.x + loc.x;
        }

        public float GetValue(Loc loc)
        {
            return values[LocToIndex(loc)];
        }

        public void SetValue(Loc loc, float v)
        {
            values[LocToIndex(loc)] = v;
        }

        public void AddValue(Loc loc, float v)
        {
            if(IsLocOnMap(loc)) {
                SetValue(loc, GetValue(loc) + v);
            }
        }

        public void AddHill(Loc center, int minRadius, int maxRadius, float height)
        {
            var values = new Dictionary<Loc, float>();

            values[center] = height;
            AddValue(center, height);

            float minDelta = height/maxRadius;
            float maxDelta = height/minRadius;

            for(int i = 1; i < maxRadius; ++i) {
                bool keepGoing = false;
                var newValues = new List<float>();
                var ring = Tile.GetTilesInRing(center, i);
                foreach(Loc pos in ring) {

                    float sum = 0f;
                    int nadj = 0;
                    foreach(var adj in Tile.AdjacentLocs(pos)) {
                        if(values.ContainsKey(adj)) {
                            sum += values[adj];
                            ++nadj;
                        }
                    }

                    if(nadj > 1) {
                        sum /= nadj;
                    }

                    sum -= _rng.Range(minDelta, maxDelta);

                    if(sum > 0f) {
                        AddValue(pos, sum);
                        keepGoing = true;
                    }

                    newValues.Add(sum);
                }

                if(!keepGoing) {
                    break;
                }

                for(int j = 0; j != newValues.Count; ++j) {
                    values[ring[j]] = newValues[j];
                }
            }
        }
    }

    class Voronoi
    {
        ConsistentRandom _rng;
        List<Loc> _points = new List<Loc>();
        Loc _dim;

        int[] _regions;

        public Voronoi(ConsistentRandom rng, Loc dim, int npoints)
        {
            _rng = rng;
            _dim = dim;
            for(int i = 0; i != npoints; ++i) {
                Loc point = new Loc(_rng.Range(0, _dim.x), _rng.Range(0, _dim.y));
                _points.Add(point);
            }

            _regions = new int[_dim.x*_dim.y];
            for(int x = 0; x != _dim.x; ++x) {
                for(int y = 0; y != _dim.y; ++y) {
                    int region = -1;
                    int bestdist = -1;
                    for(int i = 0; i != _points.Count; ++i) {
                        int distsq = (x-_points[i].x)*(x-_points[i].x) + (y-_points[i].y)*(y-_points[i].y);
                        if(region == -1 || distsq < bestdist) {
                            bestdist = distsq;
                            region = i;
                        }
                    }

                    SetRegion(new Loc(x, y), region);
                }
            }
        }

        public void SetRegion(Loc pos, int region)
        {
            _regions[pos.y*_dim.x + pos.x] = region;
        }

        public int GetRegion(Loc pos)
        {
            return _regions[pos.y*_dim.x + pos.x];
        }

        public bool IsLocOnMap(Loc loc)
        {
            return loc.x >= 0 && loc.y >= 0 && loc.x < _dim.x && loc.y < _dim.y;
        }

        public List<Loc> CalculateHotspots()
        {
            var result = new List<Loc>();
            int[] directions = new int[_regions.Length];
            for(int i = 0; i != directions.Length; ++i) {
                directions[i] = _rng.Range(0, 6);
            }

            for(int x = 0; x != _dim.x; ++x) {
                for(int y = 0; y != _dim.y; ++y) {
                    Loc loc = new Loc(x, y);
                    int region = GetRegion(loc);
                    Tile.Direction direction = (Tile.Direction)directions[region];
                    Loc otherLoc = Tile.LocInDirection(loc, direction);
                    if(IsLocOnMap(otherLoc) && GetRegion(otherLoc) != region) {
                        result.Add(loc);
                    }
                }
            }

            return result;
        }
    }


    [SerializeField]
    Tile _tilePrefab = null;

    [SerializeField]
    GameController _controller = null;

    [SerializeField]
    GameMap _map = null, _underworldMap = null;

    [SerializeField]
    Loc _dim = Loc.invalid;

    [SerializeField]
    int _marginSize = 16;

    ConsistentRandom _rng;

    List<Loc> _treasureCavernLocs = new List<Loc>();

    void SetCave(Loc loc)
    {
        Tile tile = _underworldMap.GetTile(loc);
        tile.isvoid = false;
        tile.gameObject.SetActive(true);
        tile.terrain = new TerrainInfo(_caveFloorTerrain);

        foreach(Loc adj in Tile.AdjacentLocs(loc)) {
            if(_underworldMap.LocOnBoard(adj) == false) {
                continue;
            }

            Tile t = _underworldMap.GetTile(adj);
            if(adj.depth == loc.depth && t.isvoid) {
                t.isvoid = false;
                t.gameObject.SetActive(true);
                t.terrain = new TerrainInfo(_caveWallTerrain);

                Tile gateTile = _map.GetTile(t.loc.toOverworld);
                if(gateTile.underworldGate) {
                    gateTile.underworldGate = false;
                }

            }
        }

        //ensure the tile above this isn't a gate to the underworld,
        //since having a gate directly over an underworld area isn't legal.
        Tile overworldTile = _map.GetTile(loc.toOverworld);
        if(overworldTile.underworldGate) {
            overworldTile.underworldGate = false;
        }
    }

    void FindTreasureCavernLoc(HeightMap heightMap)
    {
        Debug.Log("TRYING TO SPAWN CAVE...");
        Loc bestLoc = Loc.invalid;
        float bestHeight = -1.0f;
        foreach(Loc loc in _map.dimensions.range) {
            float height = heightMap.GetValue(loc);
            if(height < bestHeight) {
                continue;
            }

            bool eligible = true;
            foreach(Loc existing in _treasureCavernLocs) {
                if(Tile.DistanceBetween(existing, loc) < 8) {
                    eligible = false;
                    break;
                }
            }

            if(eligible == false) {
                continue;
            }

            foreach(KingdomSpawn kingdom in _kingdoms) {
                if(Tile.DistanceBetween(kingdom.keepLoc, loc) < 8) {
                    eligible = false;
                    break;
                }
            }

            if(eligible == false) {
                continue;
            }

            bestHeight = height;
            bestLoc = loc;
        }

        if(bestLoc.valid) {
            _treasureCavernLocs.Add(bestLoc);
            Debug.Log("SPAWN CAVE: " + bestLoc);
        }
    }

    public void SetPlayerTeam(Team team, int difficulty)
    {
        KingdomSpawn[] newKingdoms = new KingdomSpawn[_kingdoms.Length];
        int outIndex = 2;
        for(int i = 0; i != _kingdoms.Length; ++i) {
            var k = _kingdoms[i];
            if(k.team == team.playerTeamReplaces) {
                k.team = team;
                newKingdoms[0] = k;
                newKingdoms[0].castleSize = 2;

                int numVillages = difficulty == 0 ? 6 : 4;

                newKingdoms[0].numVillages = 4;
                newKingdoms[0].numVillagesStartingOwnership = numVillages;

            } else if(k.team.primaryEnemy) {
                //primary enemy spawns right after the player.
                newKingdoms[1] = k;
            } else {
                if(outIndex == _kingdoms.Length) {
                    Debug.LogError("Could not find kingdom to place player team in");
                    return;
                }
                newKingdoms[outIndex++] = k;
            }
        }

        _kingdoms = newKingdoms;
    }

    public float progressPercent = 0f;
    public string progressDescription = "Generating Map...";
    public bool finished = false;

    void GenerationProgress(string description, float percent)
    {
        progressDescription = description;
        progressPercent = percent;
    }

    public IEnumerator GenerateMap()
    {
        GenerationProgress("Generating Heightmaps...", 0f);
        yield return null;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        if(_maxKingdoms > 0 && _maxKingdoms < _kingdoms.Length) {
            KingdomSpawn[] kingdoms = new KingdomSpawn[_maxKingdoms];
            for(int i = 0; i != _maxKingdoms; ++i) {
                kingdoms[i] = _kingdoms[i];
            }

            _kingdoms = kingdoms;
        }

        if(randomSeed < 0) {
            randomSeed = new ConsistentRandom().Next();
        }

        _rng = new ConsistentRandom(randomSeed);
        Debug.Log("RNG: GENERATE MAP WITH SEED = " + randomSeed);

        foreach(Tile t in _map.tiles) {
            GameObject.Destroy(t.gameObject);
        }

        foreach(Tile t in _underworldMap.tiles) {
            GameObject.Destroy(t.gameObject);
        }

        List<Tile> newTiles = new List<Tile>();
        List<Tile> newUnderworldTiles = new List<Tile>();

        HeightMap heightMap = new HeightMap(new Loc(_dim.x, _dim.y), _rng);

        for(int i = 0; i != 5; ++i) {
            heightMap.AddHill(new Loc(_rng.Range(_marginSize, _dim.x-_marginSize), _rng.Range(_marginSize, _dim.y-_marginSize)), 8, 18, 1f);
        }

        for(int i = 0; i != 50; ++i) {
            heightMap.AddHill(new Loc(_rng.Range(_marginSize, _dim.x-_marginSize), _rng.Range(_marginSize, _dim.y-_marginSize)), 3, 8, 0.5f);
        }

        List<float> heightHisto = heightMap.GenerateHistogram();
        float maxWater = heightHisto[(int)(heightHisto.Count*0.4f)] + 0.01f;
        float maxGrass = 1.2f; // heightHisto[(int)(heightHisto.Count*0.7f)];
        float maxHill = 2f; // heightHisto[(int)(heightHisto.Count*0.9f)];

        float minCave = 1.5f;

        Voronoi voronoi = new Voronoi(_rng, _dim, 20);
        var hotspots = voronoi.CalculateHotspots();
        foreach(Loc hotspot in hotspots) {
            if(heightMap.GetValue(hotspot) < maxWater) {
                continue;
            }

            if(_rng.Range(0,100) < 20) {
                heightMap.AddHill(hotspot, 2, 4, 2f);
            }
        }

        System.Diagnostics.Stopwatch innerWatch = new System.Diagnostics.Stopwatch();
        innerWatch.Start();


        for(int y = 0; y != _dim.y; ++y) {

            if(y%5 == 0) {
                GenerationProgress("Generating Tiles...", 0.1f + 0.3f*((float)y)/(float)_dim.y);
                yield return null;
            }

            for(int x = 0; x != _dim.x; ++x) {
                Loc loc = new Loc(x, y, 1);
                Tile newTile = Instantiate(_tilePrefab, _map.transform);
                newTile.map = _map;
                newTile.loc = loc;

                float height = 0f;
                if(heightMap.IsLocOnMap(loc)) {
                    //newTile.debugText = string.Format("{0:0.00}", heightMap.GetValue(loc));
                    height = heightMap.GetValue(loc);
                }

                if(height < maxWater) {
                    newTile.terrain = new TerrainInfo(_oceanTerrain);
                    Loc[] adj = Tile.AdjacentLocs(loc);
                    foreach(Loc a in adj) {
                        if(heightMap.IsLocOnMap(a) && heightMap.GetValue(a) >= maxWater) {
                            newTile.terrain = new TerrainInfo(_coastTerrain);
                            break;
                        }
                    }
                } else if(height < maxGrass) {
                    newTile.terrain = new TerrainInfo(_grasslandTerrain);
                } else if(height < maxHill) {
                    newTile.terrain = new TerrainInfo(_hillTerrain);
                } else {
                    newTile.terrain = new TerrainInfo(_mountainTerrain);
                }

                newTiles.Add(newTile);

                Tile underworldTile = Instantiate(_tilePrefab, _underworldMap.transform);
                underworldTile.map = _underworldMap;
                underworldTile.loc = new Loc(x, y, _underworldMap.depth);
                underworldTile.isvoid = true;
                underworldTile.gameObject.SetActive(false);

                newUnderworldTiles.Add(underworldTile);
            }
        }

        innerWatch.Stop();
        Debug.Log("INNER GENERATION: " + innerWatch.ElapsedMilliseconds);

        GenerationProgress("Generating Mountains...", 0.4f);
        yield return null;


        _map.tiles = newTiles;
        _map.dimensions = _dim;

        _map.SetupEdges();

        _underworldMap.tiles = newUnderworldTiles;
        _underworldMap.dimensions = _dim;

        bool[] isMountain = new bool[_dim.x*_dim.y];
        for(int y = 0; y != _dim.y; ++y) {
            for(int x = 0; x != _dim.x; ++x) {
                isMountain[y*_dim.x + x] = _map.GetTile(new Loc(x, y)).terrain.terrain == _mountainTerrain;
            }
        }

        List<Pathfind.Region> mountainRegions = Pathfind.FindRegions(isMountain, _dim);
        Debug.Log("MOUNTAIN REGIONS: " + mountainRegions.Count);

        foreach(var mountainRange in mountainRegions) {
            if(mountainRange.locs.Count < 6) {
                continue;
            }

            int lakeSize = mountainRange.locs.Count/3;
            float highest = 0f;
            Loc highestLoc = mountainRange.locs.First();
            foreach(var loc in mountainRange.locs) {
                float height = heightMap.GetValue(loc);
                if(height > highest) {
                    highest = height;
                    highestLoc = loc;
                }
            }

            List<Loc> lakeLocs = new List<Loc>();
            lakeLocs.Add(highestLoc);
            while(lakeLocs.Count < lakeSize) {
                Loc nextLoc = new Loc(0,0);
                float nextHighest = 0f;
                foreach(Loc currentLoc in lakeLocs) {
                    Loc[] adj = Tile.AdjacentLocs(currentLoc);
                    foreach(Loc a in adj) {
                        if(lakeLocs.Contains(a) || heightMap.IsLocOnMap(a) == false) {
                            continue;
                        }
                        float height = heightMap.GetValue(a);
                        if(height > nextHighest) {
                            nextHighest = height;
                            nextLoc = a;
                        }
                    }
                }

                lakeLocs.Add(nextLoc);
            }

            //now extend into a river, flowing downhill.
            while(_map.GetTile(lakeLocs[lakeLocs.Count-1]).terrain.terrain != _oceanTerrain) {
                Loc[] adj = Tile.AdjacentLocs(lakeLocs[lakeLocs.Count-1]);

                Loc nextLoc = Loc.invalid;
                float lowest = 1000f;
                foreach(Loc a in adj) {
                    if(_map.LocOnBoard(a) == false || lakeLocs.Contains(a)) {
                        continue;
                    }

                    float height = heightMap.GetValue(a);
                    if(height < lowest) {
                        lowest = height;
                        nextLoc = a;
                    }
                }

                if(lowest >= 1000f) {
                    break;
                }

                lakeLocs.Add(nextLoc);
            }

            foreach(Loc loc in lakeLocs) {
                Tile t = _map.GetTile(loc);
                t.terrain = new TerrainInfo(_riverTerrain);
                t.freshwater = true;
                _freshWater.Add(loc);
            }
        }

        GenerationProgress("Generating Forests...", 0.5f);
        yield return null;


        HeightMap vegetationMap = new HeightMap(new Loc(_dim.x, _dim.y), _rng);
        for(int i = 0; i != 16; ++i) {
            vegetationMap.AddHill(new Loc(_rng.Range(_marginSize, _dim.x-_marginSize), _rng.Range(_marginSize, _dim.y-_marginSize)), 5, 12, 1f);
        }

        for(int i = 0; i != 128; ++i) {
            vegetationMap.AddHill(new Loc(_rng.Range(_marginSize, _dim.x-_marginSize), _rng.Range(_marginSize, _dim.y-_marginSize)), 1, 3, 1f);
        }

        List<float> vegetationHisto = new List<float>();

        foreach(Loc loc in _dim.range) {
            Tile t = _map.GetTile(loc);
            if(t.terrain.terrain == _grasslandTerrain) {
                vegetationHisto.Add(vegetationMap.GetValue(loc));
            }
        }

        vegetationHisto.Sort();

        float forestEdgeThreshold = vegetationHisto[(int)(vegetationHisto.Count*0.7f)];
        float denseForestThreshold = vegetationHisto[(int)(vegetationHisto.Count*0.9f)];

        for(int y = 0; y != _dim.y; ++y) {
            for(int x = 0; x != _dim.x; ++x) {
                Loc loc = new Loc(x, y);
                Tile t = _map.GetTile(loc);
                //t.debugText = string.Format("{0:0.00}", vegetationMap.GetValue(loc));
                if(t.terrain.terrain == _grasslandTerrain) {
                    float height = vegetationMap.GetValue(loc);
                    float chanceForest = (height - forestEdgeThreshold)/(denseForestThreshold - forestEdgeThreshold);
                    if(_rng.Range(0f, 1f) <= chanceForest) {
                        t.terrain = new TerrainInfo(_forestTerrain);
                    }
                }

            }
        }

        GenerationProgress("Generating Swamps...", 0.6f);
        yield return null;

        HeightMap swampMap = new HeightMap(new Loc(_dim.x, _dim.y), _rng);
        for(int i = 0; i != 16; ++i) {
            swampMap.AddHill(new Loc(_rng.Range(_marginSize, _dim.x-_marginSize), _rng.Range(_marginSize, _dim.y-_marginSize)), 5, 12, 1f);
        }

        for(int i = 0; i != 128; ++i) {
            swampMap.AddHill(new Loc(_rng.Range(_marginSize, _dim.x-_marginSize), _rng.Range(_marginSize, _dim.y-_marginSize)), 1, 3, 1f);
        }

        List<float> swampHisto = new List<float>();

        foreach(Loc loc in _dim.range) {
            Tile t = _map.GetTile(loc);
            if(t.terrain.terrain == _grasslandTerrain) {
                swampHisto.Add(swampMap.GetValue(loc));
            }
        }

        swampHisto.Sort();

        float swampEdgeThreshold = vegetationHisto[(int)(vegetationHisto.Count*0.7f)];
        float denseSwampThreshold = vegetationHisto[(int)(vegetationHisto.Count*0.95f)];


        for(int y = 0; y != _dim.y; ++y) {
            for(int x = 0; x != _dim.x; ++x) {
                Loc loc = new Loc(x, y);
                Tile t = _map.GetTile(loc);
                //t.debugText = string.Format("{0:0.00}", vegetationMap.GetValue(loc));
                if(t.terrain.terrain == _grasslandTerrain) {
                    float height = swampMap.GetValue(loc);
                    float chanceSwamp = (height - swampEdgeThreshold)/(denseSwampThreshold - swampEdgeThreshold);
                    if(_rng.Range(0f, 1f) <= chanceSwamp) {
                        t.terrain = new TerrainInfo(_swampTerrain);
                    }
                }
            }
        }

        GenerationProgress("Generating Castles...", 0.7f);
        yield return null;


        SpawnKingdoms();

        GenerationProgress("Spawning Villages...", 0.8f);
        yield return null;


        SpawnNeutralVillages();

        foreach(KingdomSpawn kingdom in _kingdoms) {
            SpawnKingdomUnits(kingdom);
        }

        _controller.gameState.seed = randomSeed;

        //add the barbarian team.
        _controller.gameState.numBarbarianTeam = _controller.gameState.teams.Count;
        _controller.gameState.barbarianTeam = new TeamInfo(_barbarianTeam);
        _controller.gameState.teams.Add(_controller.gameState.barbarianTeam);

        GenerationProgress("Spawning Bandits...", 0.8f);
        yield return null;


        SpawnBarbarianUnits();

        if(_barbarianTeam.ai != null) {
            _controller.aiStates.Add(new AIState() {
                teamNumber = GameController.instance.aiStates.Count,
            });

            if(GameController.instance.gameState.teams.Count != GameController.instance.gameState.aiStates.Count) {
                Debug.LogErrorFormat("AIState has bad index: {0}/{1}", GameController.instance.gameState.teams.Count, GameController.instance.gameState.aiStates.Count);
            }

        } else {
            _controller.aiStates.Add(null);
        }

        var teamInfo = new TeamInfo(_caveSpawnTeam);
        _controller.gameState.teams.Add(teamInfo);
        _controller.aiStates.Add(new AIState() {
            targetLoc = _kingdoms[0].keepLoc,
            teamNumber = GameController.instance.aiStates.Count,
        });

        if(GameController.instance.gameState.teams.Count != GameController.instance.gameState.aiStates.Count) {
            Debug.LogErrorFormat("AIState has bad index: {0}/{1}", GameController.instance.gameState.teams.Count, GameController.instance.gameState.aiStates.Count);
        }

        GenerationProgress("Generating the Underworld...", 0.9f);
        yield return null;


        for(int i = 0; i != treasureCaverns.Count; ++i) {
            FindTreasureCavernLoc(heightMap);
        }

        for(int i = 0; i != _treasureCavernLocs.Count; ++i) {
            Debug.Log("Treasure cavern: " + i + " loc " + _treasureCavernLocs[i] + " distance from player " + _kingdoms[0].keepLoc + " is " + Tile.DistanceBetween(_treasureCavernLocs[i], _kingdoms[0].keepLoc));
        }

        for(int i = 0; i != treasureCaverns.Count; ++i) {
            if(i < _treasureCavernLocs.Count) {
                List<Loc> cavernLocs = null;

                for(int ntry = 0; cavernLocs == null && ntry != 3; ++ntry) {
                    cavernLocs = treasureCaverns[i].cave.GenerateCavernArea(_treasureCavernLocs[i], _rng);
                }

                if(cavernLocs == null) {
                    Debug.Log("FAILED TO GENERATE CAVE AT " + _treasureCavernLocs[i]);
                    continue;
                }

                if(treasureCaverns[i].underworldTeam != null) {

                }

                treasureCaverns[i].cave.SpawnCavern(cavernLocs, treasureCaverns[i]);
            }
        }

        _map.Init();

        GenerationProgress("Naming Villages...", 0.95f);
        yield return null;

        List<string> villageNamesUsed = new List<string>();

        foreach(Tile t in _map.tiles) {
            if(t.terrain.villageInfo != null) {
                List<Tile> region = Pathfind.FindRegionFromTile(t, (Tile tile) => tile.terrain.villageInfo != null || tile.terrain.rules.castle || tile.terrain.rules.keep);
                bool hasLabel = false;

                foreach(Tile tile in region) {
                    if(tile.hasLabel) {
                        hasLabel = true;
                        break;
                    }
                }

                if(hasLabel == false) {
                    string villageName = null;

                    int ntry = 0;
                    while(string.IsNullOrEmpty(villageName) || villageNamesUsed.Contains(villageName)) {
                        if(++ntry >= 12) {
                            break;
                        }
                        villageName = t.terrain.villageInfo.GenerateName(t, _rng);
                    }


                    if(string.IsNullOrEmpty(villageName) == false) {
                        villageNamesUsed.Add(villageName);

                        t.SetLabel(villageName, false);
                    }
                }
            }
        }

        //BuildRoadBetweenKingdoms(_kingdoms[0], _kingdoms[1]);

        for(int nkingdom = 0; nkingdom < _kingdoms.Length; ++nkingdom) {
            KingdomSpawn kingdom = _kingdoms[nkingdom];

            GameController.instance.ForceCaptureLoc(kingdom.keepLoc, nkingdom);

            kingdom.spawnedVillages.Sort((a, b) => Tile.DistanceBetween(kingdom.keepLoc, a).CompareTo(Tile.DistanceBetween(kingdom.keepLoc, b)));

            for(int i = 0; i < kingdom.spawnedVillages.Count && i < kingdom.numVillagesStartingOwnership; ++i) {
                GameController.instance.ForceCaptureLoc(kingdom.spawnedVillages[i], nkingdom);
            }
        }

        GenerationProgress("Generating Roads...", 0.97f);


        List<KingdomSpawn> kingdomsProcessed = new List<KingdomSpawn>();

        //road between the primary enemy and the player
        BuildRoadBetweenKingdoms(_kingdoms[0], _kingdoms[1]);

        foreach(KingdomSpawn kingdom in _kingdoms) {
            foreach(KingdomSpawn neighbor in kingdom.neighbors) {
                if(kingdomsProcessed.Contains(neighbor) == false) {
                    BuildRoadBetweenKingdoms(kingdom, neighbor);
                }
            }

            kingdomsProcessed.Add(kingdom);
        }

        foreach(UnitType unitType in _testUnitTypes) {
            UnitInfo unitInfo = unitType.createUnit();
            unitInfo.nteam = 0;
            Unit unit = Instantiate(_unitPrefab, _controller.transform);
            unit.unitInfo = unitInfo;
            unit.loc = _controller.FindVacantTileNear(_kingdoms[0].keepLoc);

            _controller.AddUnit(unit);
        }

        foreach(UnitType unitType in _testEnemyTypes) {
            UnitInfo unitInfo = unitType.createUnit();
            unitInfo.nteam = GameController.instance.gameState.numBarbarianTeam;
            Unit unit = Instantiate(_unitPrefab, _controller.transform);
            unit.unitInfo = unitInfo;
            unit.loc = _controller.FindVacantTileNear(_kingdoms[0].keepLoc);

            _controller.AddUnit(unit);
        }


        foreach(Unit unit in _controller.units) {
            unit.RefreshLocation();
            unit.hiddenInUnderworld = unit.loc.underworld;
        }

        watch.Stop();
        Debug.Log("MAP GENERATION TIME: " + watch.ElapsedMilliseconds);

        GenerationProgress("Finalizing map...", 1f);
        yield return null;

        finished = true;
    }

    [System.Serializable]
    public struct NeutralVillageTerrain
    {
        public Terrain terrain;

        public int resources;

        public Terrain villageType;
    }

    [SerializeField]
    NeutralVillageTerrain[] _neutralVillageTerrain = null;

    void SpawnNeutralVillages()
    {
        List<TerrainRules> terrainAllowed = new List<TerrainRules>();

        foreach(var info in _neutralVillageTerrain) {
            terrainAllowed.Add(info.terrain.rules);
        }

        HashSet<Loc> unclaimed = new HashSet<Loc>();

        for(int y = 0; y != _map.dimensions.y; ++y) {
            for(int x = 0; x != _map.dimensions.x; ++x) {
                Loc loc = new Loc(x, y);
                if(_claimedLocs.Contains(loc) || terrainAllowed.Contains(_map.GetTile(loc).terrain.rules) == false) {
                    continue;
                }

                unclaimed.Add(loc);
            }
        }

        Debug.Log("SPAWNING NEUTRAL VILLAGES: " + unclaimed.Count);

        while(unclaimed.Count > 0) {
            Loc villageLoc = Loc.invalid;
            foreach(var loc in unclaimed) {

                bool canSupportVillage = false;

                var terrainRules = _map.GetTile(loc).terrain.rules;
                foreach(var info in _neutralVillageTerrain) {
                    if(info.terrain.rules == terrainRules) {
                        canSupportVillage = info.villageType != null;
                    }
                }
                
                if(canSupportVillage) {
                    villageLoc = loc;
                    break;
                }
            }

            if(villageLoc == Loc.invalid) {
                //No remaining hexes can support villages, so abort.
                break;
            }

            int resources = 0;
            var villageClaim = new List<Loc>();
            var live = new List<Loc>();
            live.Add(villageLoc);

            villageClaim.Add(villageLoc);
            unclaimed.Remove(villageLoc);

            while(live.Count > 0 && resources < 100) {
                var nextLive = new List<Loc>();
                foreach(Loc loc in live) {
                    foreach(Loc adj in Tile.AdjacentLocs(loc)) {
                        if(unclaimed.Contains(adj)) {

                            TerrainRules terrainRules = _map.GetTile(adj).terrain.rules;
                            foreach(var info in _neutralVillageTerrain) {
                                if(info.terrain.rules == terrainRules) {
                                    resources += info.resources;
                                    break;
                                }
                            }

                            villageClaim.Add(adj);
                            unclaimed.Remove(adj);
                            nextLive.Add(adj);

                            if(resources >= 100) {
                                break;
                            }
                        }
                    }

                    if(resources >= 100) {
                        break;
                    }
                }

                live = nextLive;
            }

            if(resources >= 100) {
                Debug.Log("SPAWN NEUTRAL AT " + villageLoc.ToString());
                foreach(var info in _neutralVillageTerrain) {
                    if(info.terrain.rules == _map.GetTile(villageLoc).terrain.rules && info.villageType != null) {
                        _map.GetTile(villageLoc).terrain = new TerrainInfo(info.villageType);
                        _villageLocs.Add(villageLoc);
                        _controller.gameState.neutralVillages.Add(villageLoc);
                        Debug.Log("SPAWN SUCCESS");
                        break;
                    }
                }
            }
            else {
                Debug.Log("FAILED TO SPAWN NEUTRAL VILLAGE AT " + villageLoc.ToString() + ": " + resources + " REMAINING: " + unclaimed.Count);
            }
        }
    }

    void SpawnKingdoms()
    {
        List<TerrainRules> waterTerrain = new List<TerrainRules>() { _oceanTerrain.rules, _coastTerrain.rules };
        List<Pathfind.Region> islands = Pathfind.FindRegions((Loc loc) => waterTerrain.Contains(_map.GetTile(loc).terrain.rules) == false || _map.GetTile(loc).freshwater, _map.dimensions);
        Pathfind.Region oceanRegion = Pathfind.FindRegions((Loc loc) => _map.GetTile(loc).terrain.rules.aquatic, _map.dimensions)[0];

        HashSet<Loc> oceanAndAdjacent = Tile.LocsAndAdjacent(oceanRegion.locs);

        int maxRegionSize = 0;
        foreach(var island in islands) {
            if(island.locs.Count > maxRegionSize) {
                maxRegionSize = island.locs.Count;
            }
        }

        List<Loc> mainlandLocs = new List<Loc>();

        foreach(var island in islands) {
            if(island.locs.Count > maxRegionSize/8) {
                foreach(Loc loc in island.locs) {
                    mainlandLocs.Add(loc);
                }
            }
        }

        List<Loc> keepLocs = new List<Loc>();

        foreach(KingdomSpawn kingdom in _kingdoms) {
            keepLocs.Add(mainlandLocs[_rng.Next(mainlandLocs.Count)]);
        }

        HashSet<Loc> mainlandLocsHash = new HashSet<Loc>(mainlandLocs);

        bool changes = true;
        while(changes) {
            changes = false;
            for(int i = 0; i != keepLocs.Count; ++i) {
                List<int> distances = new List<int>();
                for(int j = 0; j != keepLocs.Count; ++j) {
                    if(j != i) {
                        int dist = Tile.DistanceBetween(keepLocs[i], keepLocs[j]);
                        distances.Add(dist);
                    }
                }

                distances.Sort();

                int score = distances[0]*1000000 + distances[1]*1000 + distances[2];

                //Debug.LogFormat("CURRENT SCORE: {0} = {1}*1000 + {2}", score, distances[0], distances[1]);

                Loc[] adj = Tile.AdjacentLocs(keepLocs[i]);
                foreach(Loc a in adj) {
                    if(mainlandLocsHash.Contains(a) == false) {
                        continue;
                    }

                    distances.Clear();
                    for(int j = 0; j != keepLocs.Count; ++j) {
                        if(j != i) {
                            int dist = Tile.DistanceBetween(a, keepLocs[j]);
                            distances.Add(dist);
                        }
                    }

                    distances.Sort();

                    int newScore = distances[0]*1000000 + distances[1]*1000 + distances[2];
                    //Debug.LogFormat("NEW SCORE: {0} = {1}*1000 + {2} FROM {3} -> {4}", newScore, distances[0], distances[1], keepLocs[i], a);

                    if(newScore > score) {
                        keepLocs[i] = a;
                        score = newScore;
                        changes = true;
                    }
                }
            }
        }

        List<HashSet<Tile>> areas = new List<HashSet<Tile>>();
        foreach(KingdomSpawn kingdom in _kingdoms) {
            areas.Add(new HashSet<Tile>());
        }

        foreach(Loc loc in mainlandLocs) {
            int bestDist = 1000;
            int bestIndex = -1;
            for(int i = 0; i != keepLocs.Count; ++i) {
                int dist = Tile.DistanceBetween(loc, keepLocs[i]);
                if(dist < bestDist) {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            areas[bestIndex].Add(_map.GetTile(loc));
        }

        List<List<int>> areaNeighbors = new List<List<int>>();
        foreach(var area in areas) {
            List<int> neighbors = new List<int>();
            for(int i = 0; i != areas.Count; ++i) {
                var otherArea = areas[i];
                if(otherArea == area) {
                    continue;
                }

                bool foundBoundary = false;
                foreach(var loca in area) {
                    foreach(var locb in otherArea) {
                        if(Tile.DistanceBetween(loca.loc, locb.loc) == 1) {
                            foundBoundary = true;
                            break;
                        }
                    }

                    if(foundBoundary) {
                        break;
                    }
                }

                if(foundBoundary) {
                    neighbors.Add(i);
                }
            }

            Debug.LogFormat("REGION CENTERED AT {0} HAS {1} NEIGHBORS", keepLocs[areaNeighbors.Count], neighbors.Count);
            areaNeighbors.Add(neighbors);
        }

        List<List<float>> kingdomScoresPerRegion = new List<List<float>>();
        foreach(KingdomSpawn kingdom in _kingdoms) {
            List<float> scores = new List<float>();
            for(int i = 0; i != areas.Count; ++i) {
                scores.Add(kingdom.ScoreCastlePlacement(keepLocs[i], areas[i], areaNeighbors[i].Count));
            }

            kingdomScoresPerRegion.Add(scores);
        }

        List<int> kingdomKeepAssignment = new List<int>();
        for(int i = 0; i != _kingdoms.Length; ++i) {
            kingdomKeepAssignment.Add(i);
        }

        bool betterScoreFound = true;
        float netScore = 0f;
        while(betterScoreFound) {
            betterScoreFound = false;

            int index1 = -1, index2 = -1;
            for(int i = 0; i != _kingdoms.Length; ++i) {
                for(int j = i+1; j < _kingdoms.Length; ++j) {
                    int tmp = kingdomKeepAssignment[i];
                    kingdomKeepAssignment[i] = kingdomKeepAssignment[j];
                    kingdomKeepAssignment[j] = tmp;

                    float score = 0f;
                    for(int n = 0; n < _kingdoms.Length; ++n) {
                        int areaIndex = kingdomKeepAssignment[n];
                        float kingdomScore = _kingdoms[n].ScoreCastlePlacement(keepLocs[areaIndex], areas[areaIndex], areaNeighbors[areaIndex].Count);
                        score += kingdomScore;
                    }

                    string penaltyInfo = "";
                    float penalty = 0f;

                    string msg = "";
                    for(int n = 0; n < _kingdoms.Length; ++n) {
                        msg += " [" + n + "] -> " + kingdomKeepAssignment[n] + " -> " + keepLocs[kingdomKeepAssignment[n]]+ "; ";
                    }

                    //suffer a penalty for enemy starting positions closer to 12 from the player.
                    for(int n = 1; n < _kingdoms.Length; ++n) {
                        int dist = Tile.DistanceBetween(keepLocs[kingdomKeepAssignment[0]], keepLocs[kingdomKeepAssignment[n]]);
                        if(dist < 12) {
                            score -= (12 - dist)*20f;
                            penalty += (12 - dist)*20f;
                        }
                    }

                    int playerDistFromPrimaryEnemy = Tile.DistanceBetween(keepLocs[kingdomKeepAssignment[0]], keepLocs[kingdomKeepAssignment[1]]);

                    if(playerDistFromPrimaryEnemy < 40) {
                        score += playerDistFromPrimaryEnemy*10f;
                    } else {
                        score += 40*10f;
                    }

                    if(score > netScore) {

                        netScore = score;
                        betterScoreFound = true;
                        index1 = i;
                        index2 = j;
                    }

                    tmp = kingdomKeepAssignment[i];
                    kingdomKeepAssignment[i] = kingdomKeepAssignment[j];
                    kingdomKeepAssignment[j] = tmp;
                }
            }

            if(betterScoreFound) {
                Debug.LogFormat("FOUND BETTER SCORE {0} {1}: {2}", index1, index2, netScore);

                int tmp = kingdomKeepAssignment[index1];
                kingdomKeepAssignment[index1] = kingdomKeepAssignment[index2];
                kingdomKeepAssignment[index2] = tmp;
            }
        }

        int minDistance = 1000;
        foreach(var keepa in keepLocs) {
            foreach(var keepb in keepLocs) {
                if(keepa != keepb) {
                    int dist = Tile.DistanceBetween(keepa, keepb);
                    if(dist < minDistance) {
                        minDistance = dist;
                    }
                }
            }
        }

        Debug.LogFormat("PLACEMENT: minDistance = {0}", minDistance);

        betterScoreFound = true;
        while(betterScoreFound) {
            betterScoreFound = false;

            for(int i = 0; i != kingdomKeepAssignment.Count; ++i) {
                Loc keepLoc = keepLocs[kingdomKeepAssignment[i]];
                int score = _kingdoms[i].scoreCastlePrecisePositioning(keepLoc);

                int distToPlayer = Tile.DistanceBetween(keepLoc, keepLocs[kingdomKeepAssignment[0]]);

                Debug.LogFormat("PLACEMENT: SCORE FOR {0} IS {1}", keepLoc, score);
                foreach(Loc adj in Tile.AdjacentLocs(keepLoc)) {
                    if(mainlandLocsHash.Contains(adj) == false) {
                        continue;
                    }

                    bool isValid = true;
                    for(int j = 0; j != keepLocs.Count; ++j) {
                        if(i != j && Tile.DistanceBetween(adj, keepLocs[kingdomKeepAssignment[j]]) < minDistance) {
                            isValid = false;
                            break;
                        }
                    }

                    if(isValid == false) {
                        continue;
                    }


                    //don't move closer to the player if we are less than 12 spaces away
                    if(i > 0) {
                        int newDistToPlayer = Tile.DistanceBetween(adj, keepLocs[kingdomKeepAssignment[0]]);
                        if(newDistToPlayer < 12 && newDistToPlayer < distToPlayer) {
                            continue;
                        }
                    }

                    int newScore = _kingdoms[i].scoreCastlePrecisePositioning(adj);
                    if(newScore > score) {
                        score = newScore;
                        keepLoc = adj;
                        betterScoreFound = true;
                    }
                }

                keepLocs[kingdomKeepAssignment[i]] = keepLoc;
            }
        }

        int index = 0;
        foreach(KingdomSpawn kingdom in _kingdoms) {

            //find neighbors.
            List<int> neighbors = areaNeighbors[kingdomKeepAssignment[index]];
            for(int i = 0; i != _kingdoms.Length; ++i) {
                if(neighbors.Contains(kingdomKeepAssignment[i])) {
                    kingdom.neighbors.Add(_kingdoms[i]);
                }
            }

            //build the castle.
            Loc keepLoc = keepLocs[kingdomKeepAssignment[index]];
            kingdom.keepLoc = keepLoc;

            Debug.Log("KEEP LOC: " + keepLoc.ToString());
            kingdom.keepInit = true;

            Tile keepTile = _map.GetTile(keepLoc);
            keepTile.terrain = new TerrainInfo(kingdom.keepTerrain);

            if(string.IsNullOrEmpty(kingdom.castleName) == false) {
                keepTile.SetLabel(kingdom.castleName);
            }

            bool needsConnectionToOcean = kingdom.team.aquatic;
            Loc[] castleLocs = Tile.AdjacentLocs(keepLoc);
            int castleIndex = _rng.Range(0, 6);
            if(needsConnectionToOcean) {
                for(int i = 0; i != 6; ++i) {
                    if(oceanAndAdjacent.Contains(castleLocs[(castleIndex+i)%6])) {
                        castleIndex = (castleIndex+i)%6;
                        needsConnectionToOcean = false;
                    }
                }

                //aquatic kingdoms should have their castle surrounded by a moat of water.
                foreach(Loc adj in Tile.AdjacentLocs(keepLoc)) {
                    Tile adjTile = _map.GetTile(adj);
                    if(adjTile != null && adjTile.terrain.rules.village == false && adjTile.terrain.rules.castle == false && adjTile.terrain.rules.aquatic == false) {
                        adjTile.terrain = new TerrainInfo(_riverTerrain);
                    }
                }
            }

            if(needsConnectionToOcean) {
                Loc nearestOcean = Loc.invalid;
                for(int i = 1; i != 20 && nearestOcean.valid == false; ++i) {
                    Loc[] ring = Tile.GetTilesInRing(keepLoc, i);
                    foreach(Loc r in ring) {
                        if(oceanRegion.locs.Contains(r)) {
                            nearestOcean = r;
                            Debug.LogFormat("Nearest ocean to {0} found at {1} which is {2} spaces", keepLoc, nearestOcean, i);
                            break;
                        }
                    }
                }

                Pathfind.Path path = Pathfind.FindPathTo(keepTile, _map.GetTile(nearestOcean), (Tile.Edge edge) => edge.dest.terrain.rules.aquatic ? 1 : 5, (Tile a, Tile b) => Tile.DistanceBetween(a.loc, b.loc));
                if(path != null) {
                    foreach(Loc step in path.steps) {
                        Tile t = _map.GetTile(step);
                        if(t.terrain.rules.aquatic == false && t.terrain.rules.castle == false) {
                            t.terrain = new TerrainInfo(_riverTerrain);
                        }
                    }
                } else {
                    Debug.LogFormat("Could not find ocean path from {0} to {1}", keepLoc, nearestOcean);
                }
            }

            for(int i = 0; i < kingdom.castleSize; ++i) {
                Loc castleLoc = castleLocs[(castleIndex+i)%6];
                if(_map.LocOnBoard(castleLoc)) {
                    _map.GetTile(castleLoc).terrain = new TerrainInfo(kingdom.castleTerrain);
                }
            }


            ++index;
        }
        
        SpawnKingdomVillages();
    }

    void SpawnKingdomVillages()
    {
        bool spawned = true;
        while(spawned) {
            spawned = false;
            bool playerSpawned = false;
            foreach(KingdomSpawn kingdom in _kingdoms) {
                if(playerSpawned && kingdom.team.player == false) {
                    //don't let non-players spawn their villages until players have spawned all theirs.
                    continue;
                }

                if(kingdom.spawnedVillages.Count < kingdom.numVillages) {
                    bool canSpawn = SpawnVillage(kingdom);
                    if(canSpawn) {
                        spawned = true;

                        if(kingdom.team.player) {
                            playerSpawned = true;
                        }
                    } else if(kingdom.team.player) {

                        //players should really get to spawn villages so if they can't
                        //on their preferred terrain we expand the terrain allowed.
                        bool useBackups = false;
                        foreach(var t in playerTerrainBackup) {
                            if(kingdom.preferredTerrain.Contains(t) == false) {
                                kingdom.preferredTerrain.Add(t);
                                useBackups = true;
                            }
                        }

                        if(useBackups) {
                            kingdom.villageSearchRadius = 1;
                            kingdom.villageSpawnFailed = false;

                            if(useBackups && SpawnVillage(kingdom)) {
                                playerSpawned = true;
                                spawned = true;
                            }
                        }
                    }
                }
            }
        }

        spawned = true;
        while(spawned) {
            spawned = false;
            foreach(KingdomSpawn kingdom in _kingdoms) {
                if(SpawnExclave(kingdom)) {
                    spawned = true;
                }
            }
        }

        for(int i = 0; i != _kingdoms.Length; ++i) {
            Debug.Log("Kingdom " + i + " spawned " + _kingdoms[i].spawnedVillages.Count + "/" + _kingdoms[i].numVillages);
        }
    }

    List<Loc> _villageLocs = new List<Loc>();
    HashSet<Loc> _claimedLocs = new HashSet<Loc>();

    bool SpawnVillage(KingdomSpawn kingdom)
    {
        if(kingdom.villageSearchRadius > kingdom.maxVillageRadius || kingdom.villageSpawnFailed) {
            kingdom.villageSpawnFailed = true;
            return false;
        }

        Loc[] ring = Tile.GetTilesInRing(kingdom.keepLoc, kingdom.villageSearchRadius);

        foreach(Loc loc in ring) {
            if(_map.LocOnBoard(loc) == false || !kingdom.preferredTerrain.Contains(_map.GetTile(loc).terrain.rules) || _claimedLocs.Contains(loc)) {
                continue;
            }

            List<Loc> claims = new List<Loc>();
            if(CanClaimVillage(kingdom, loc, claims)) {
                foreach(Loc claim in claims) {
                    _claimedLocs.Add(claim);
                }

                _map.GetTile(loc).terrain = new TerrainInfo(kingdom.villageTerrain);
                _villageLocs.Add(loc);
                kingdom.spawnedVillages.Add(loc);
                return true;
            }
        }

        ++kingdom.villageSearchRadius;
        return SpawnVillage(kingdom);
    }

    bool SpawnExclave(KingdomSpawn kingdom)
    {
        if(kingdom.numExclaves <= 0) {
            return false;
        }

        Loc[] ring = Tile.GetTilesInRing(kingdom.keepLoc, kingdom.exclaveRadius);
        foreach(Loc loc in ring) {
            if(_map.LocOnBoard(loc) == false || !kingdom.preferredTerrain.Contains(_map.GetTile(loc).terrain.rules) || _claimedLocs.Contains(loc)) {
                continue;
            }

            if(Tile.DistanceBetween(loc, _kingdoms[0].keepLoc) < 16) {
                //don't allow exclaves too near the player's spawn location.
                continue;
            }

            bool tooCloseToOtherKingdom = false;
            foreach(KingdomSpawn otherKingdom in _kingdoms) {
                if(Tile.DistanceBetween(loc, otherKingdom.keepLoc) < 10) {
                    tooCloseToOtherKingdom = true;
                    break;
                }
            }

            if(tooCloseToOtherKingdom) {
                continue;
            }


            List<Loc> claims = new List<Loc>();
            if(CanClaimVillage(kingdom, loc, claims)) {
                foreach(Loc claim in claims) {
                    _claimedLocs.Add(claim);
                }

                Tile villageTile = _map.GetTile(loc);
                villageTile.terrain = new TerrainInfo(kingdom.villageTerrain);

                if(string.IsNullOrEmpty(kingdom.exclaveName) == false) {
                    villageTile.SetLabel(kingdom.exclaveName);
                }

                _villageLocs.Add(loc);

                kingdom.exclaveLocs.Add(loc);

                --kingdom.numExclaves;

                Debug.Log("SPAWN EXCLAVE: " + loc + " FOR " + kingdom.team.teamName);
                return true;
            }
        }

        Debug.Log("FAILED TO SPAWN EXCLAVE FOR " + kingdom.team.teamName);

        return false;
    }

    bool CanClaimVillage(KingdomSpawn kingdom, Loc loc, List<Loc> claims)
    {
        foreach(KingdomSpawn rivalKingdom in _kingdoms) {
            if(rivalKingdom != kingdom) {
                foreach(Loc rivalVillage in rivalKingdom.spawnedVillages) {
                    if(Tile.DistanceBetween(loc, rivalVillage) < kingdom.villageHostileRadius + rivalKingdom.villageHostileRadius) {
                        return false;
                    }
                }

                if(rivalKingdom.team.player) {
                    if(Tile.DistanceBetween(loc, rivalKingdom.keepLoc) < 7) {
                        return false;
                    }
                }
            }
        }

        //A village has to be able to claim enough tiles nearby for it to be a viable placement.
        List<Loc> active = new List<Loc>();
        active.Add(loc);
        while(claims.Count < kingdom.villageClaim && active.Count > 0) {
            var nextActive = new List<Loc>();

            for(int i = 0; i < active.Count && claims.Count + active.Count + nextActive.Count < kingdom.villageClaim; ++i) {
                Loc[] adj = Tile.AdjacentLocs(active[i]);
                foreach(Loc a in adj) {
                    if(_map.LocOnBoard(a) == false || active.Contains(a) || nextActive.Contains(a) || claims.Contains(a) || _claimedLocs.Contains(a) || !kingdom.preferredTerrain.Contains(_map.GetTile(a).terrain.rules)) {
                        continue;
                    }

                    nextActive.Add(a);
                }
            }

            foreach(var a in active) {
                claims.Add(a);
            }

            active = nextActive;
        }

        return claims.Count >= kingdom.villageClaim;
    }

    void SpawnKingdomUnits(KingdomSpawn kingdom)
    {
        if(kingdom.team == null) {
            return;
        }

        int nteam = _controller.gameState.teams.Count;

        Team team = kingdom.team;
        var teamInfo = new TeamInfo(team) {
            keepLoc = kingdom.keepLoc,
        };
        _controller.gameState.teams.Add(teamInfo);


        if(team.ai != null) {
            _controller.aiStates.Add(team.ai.CreateAIState(kingdom, teamInfo));
        } else {
            _controller.aiStates.Add(null);
        }

        if(team.rulerType != null && kingdom.keepInit) {
            UnitInfo unitInfo = team.rulerType.createUnit();
            unitInfo.ruler = true;
            unitInfo.characterName = team.rulerName;
            unitInfo.nteam = nteam;
            Unit unit = Instantiate(_unitPrefab, _controller.transform);
            unit.unitInfo = unitInfo;
            unit.loc = _controller.FindVacantTileNear(kingdom.keepLoc);

            _controller.AddUnit(unit);
        }
    }

    void BuildRoadBetweenKingdoms(KingdomSpawn kingdomA, KingdomSpawn kingdomB)
    {
        Loc a = kingdomA.keepLoc;
        Loc b = kingdomB.keepLoc;
        if(a.valid == false || b.valid == false) {
            return;
        }

        Pathfind.Path path = Pathfind.FindPathTo(_map.GetTile(a), _map.GetTile(b), (Tile.Edge edge) => edge.dest.terrain.rules.roadBuildCost, (Tile source, Tile dest) => Tile.DistanceBetween(source.loc, dest.loc)*100);
        if(path != null) {
            Debug.Log("Setting road of " + path.steps.Count + " FROM " + a + " TO " + b);
            int index = 0;
            foreach(Loc step in path.steps) {
                Tile t = _map.GetTile(step);
                if(t.terrain.rules.aquatic && index > 0) {
                    if(index > 0) {
                        Loc prevStep = path.steps[index-1];
                        Tile.Direction dir = Tile.DirOfLoc(prevStep, step);
                        Terrain bridgeTerrain = _bridgeTerrain[((int)dir)%_bridgeTerrain.Length];
                        t.terrain = new TerrainInfo(bridgeTerrain);
                    }
                } else if(kingdomA.team.hasRoads && kingdomB.team.hasRoads) {
                    if(t.terrain.rules.canReplaceWithRoad) {
                        t.terrain = new TerrainInfo(_dirtRoadTerrain);
                    } else if(t.freshwater == false) {
                        t.terrain = new TerrainInfo(t.terrain.terrain, _dirtRoadTerrain);
                    }
                }

                ++index;
            }
        }
    }

    void SpawnBarbarianUnits()
    {
        //UnitInfo unitInfo = _controller.gameState.barbarianTeam.team.recruitmentOptions[0].createUnit();
        //unitInfo.nteam = _controller.gameState.numBarbarianTeam;
        //Unit unit = Instantiate(_unitPrefab, _controller.transform);
        //unit.unitInfo = unitInfo;
        //unit.loc = new Loc(_kingdoms[0].keepLoc.x+2, _kingdoms[0].keepLoc.y);

        //_controller.AddUnit(unit);

        foreach(KingdomSpawn kingdom in _kingdoms) {
            foreach(Loc exclaveLoc in kingdom.exclaveLocs) {
                foreach(UnitType unitType in kingdom.exclaveBarbarians) {
                    UnitInfo unitInfo = unitType.createUnit();
                    unitInfo.nteam = _controller.gameState.numBarbarianTeam;
                    Unit unit = Instantiate(_unitPrefab, _controller.transform);
                    unit.unitInfo = unitInfo;
                    unit.loc = _controller.FindVacantTileNear(exclaveLoc);

                    _controller.AddUnit(unit);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
