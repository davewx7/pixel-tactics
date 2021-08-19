using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class MapMetaInfo
{
}

[System.Serializable]
public struct SerializedLoot
{
    public Loc loc;
    public Loot loot;
    public LootInfo lootInfo;
}

[System.Serializable]
public class SerializedMap
{
    public MapMetaInfo metaInfo;

    public List<SerializedLoot> loot = new List<SerializedLoot>();

    public Loc dimensions;

    public List<TileInfo> tileIndex = new List<TileInfo>();

    //base64 encoding
    int CharToNum(char c)
    {
        if(c >= '0' && c <= '9') {
            return c - '0';
        }

        if(c >= 'a' && c <= 'z') {
            return 10 + (c - 'a');
        }

        if(c == ';') {
            return 36;
        } else if(c == '/') {
            return 37;
        }

        return 38 + (c - 'A');
    }

    static string _chars = "0123456789abcdefghijklmnopqrstuvwxyz;/ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    char NumToChar(int n)
    {
        return _chars[n];
    }

    public int GetIndex(TileInfo info)
    {
        int result = 0;
        foreach(var item in tileIndex) {
            if(item.Equals(info)) {
                return result;
            }
            ++result;
        }

        tileIndex.Add(info);
        return result;
    }

    public string tilesEncoded = "";

    public int[] tiles {
        get {
            int[] result = new int[tilesEncoded.Length/2];
            int i = 0;
            int index = 0;
            while(i < tilesEncoded.Length) {
                int n1 = CharToNum(tilesEncoded[i]);
                ++i;
                int n2 = CharToNum(tilesEncoded[i]);
                ++i;

                result[index] = n1*64 + n2;
                ++index;
            }
            return result;
        }
        set {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach(int n in value) {
                int n1 = n/64;
                int n2 = n%64;
                sb.Append(NumToChar(n1));
                sb.Append(NumToChar(n2));
            }

            tilesEncoded = sb.ToString();
        }
    }

    public SerializedMap underworld = null;

    public struct LabelInfo
    {
        public Loc loc;
        public string text;
        public bool visible;
    }

    public List<LabelInfo> labels = new List<LabelInfo>();
}

public class GameMap : MonoBehaviour
{
    public MapMetaInfo metaInfo = new MapMetaInfo();

    public SerializedMap Serialize()
    {
        SerializedMap result = new SerializedMap();

        result.metaInfo = this.metaInfo;

        foreach(Tile tile in tiles) {
            if(tile.hasLabel) {
                var labelInfo = new SerializedMap.LabelInfo() {
                    text = tile.GetLabelText(),
                    visible = !tile.hasHiddenLabel,
                    loc = tile.loc,
                };

                result.labels.Add(labelInfo);
            }

            if(tile.loot != null) {
                result.loot.Add(new SerializedLoot() {
                    loc = tile.loc,
                    loot = tile.loot.loot,
                    lootInfo = tile.loot.lootInfo,
                });
            }
        }

        result.dimensions = dimensions;

        Debug.Log("Serialize gamemap: " + dimensions);

        if(_underworld != null) {
            result.underworld = _underworld.Serialize();
        }

        int[] tileIndexes = new int[tiles.Count];
        int index = 0;
        foreach(Tile tile in tiles) {
            tileIndexes[index] = result.GetIndex(tile.tileInfo);
            ++index;
        }

        result.tiles = tileIndexes;

        return result;
    }

    public void RefreshFromSnapshot(SerializedMap data, bool underworld=false)
    {
        if(dimensions != data.dimensions || tiles.Count != data.tiles.Length) {
            Deserialize(data, underworld);
            return;
        }

        metaInfo = data.metaInfo;

        if(_underworld != null && data.underworld != null) {
            _underworld.RefreshFromSnapshot(data.underworld, true);
        }

        int index = 0;
        foreach(int tileIndex in data.tiles) {
            TileInfo t = data.tileIndex[tileIndex];
            Loc loc = new Loc(index%dimensions.x, index/dimensions.x, this.depth);

            Tile tile = tiles[index];
            tile.map = this;
            tile.tileInfo = t;
            tile.loc = loc;

            ++index;
        }

        SetupEdges();

        foreach(Tile tile in tiles) {
            tile.CalculatePosition();
        }

        foreach(var info in data.labels) {
            GetTile(info.loc).SetLabel(info.text, info.visible);
        }

        foreach(var info in data.loot) {
            if(info.loot != null) {
                GetTile(info.loc).AddLoot(info.loot, info.lootInfo);
            }
        }

    }

    public void Deserialize(SerializedMap data, bool underworld=false)
    {
        Clear();

        metaInfo = data.metaInfo;

        dimensions = data.dimensions;

        if(_underworld != null && data.underworld != null) {
            _underworld.Deserialize(data.underworld, true);
        }

        int index = 0;
        foreach(int tileIndex in data.tiles) {
            TileInfo t = data.tileIndex[tileIndex];
            Loc loc = new Loc(index%dimensions.x, index/dimensions.x, this.depth);

            Tile tile = Instantiate(GameConfig.instance.tilePrefab, transform);
            tile.map = this;
            tile.tileInfo = t;
            tile.loc = loc;

            tiles.Add(tile);

            ++index;
        }

        Init();

        foreach(Tile tile in tiles) {
            tile.CalculatePosition();
        }

        foreach(var info in data.labels) {
            GetTile(info.loc).SetLabel(info.text, info.visible);
        }

        foreach(var info in data.loot) {
            if(info.loot != null) {
                GetTile(info.loc).AddLoot(info.loot, info.lootInfo);
            }
        }
    }

    [SerializeField]
    Material _tileRegularMaterial = null, _tileFocusMaterial = null, _tileSeasonTransitionMaterial = null;

    [SerializeField]
    GameMap _underworld = null;

    public bool underworld = false;
    public int depth { get { return underworld ? 2 : 1; } }

    public void SeasonChange(Season season)
    {
        _tileSeasonTransitionMaterial.SetFloat("_Alpha", 1f);

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        List<SpriteRenderer> clones = new List<SpriteRenderer>();
        foreach(Tile tile in tiles) {
            if(GameController.instance.IsLocOnScreen(tile.loc) == false) {
                continue;
            }

            //Set the material to the transition material and then create the
            //clone so that we can be sure the material gets put on all sub-components.
            Material mat = tile.tileMaterial;
            tile.tileMaterial = _tileSeasonTransitionMaterial;

            SpriteRenderer clone = Instantiate(tile.renderer, transform);
            clone.transform.position = tile.renderer.transform.position;

            tile.tileMaterial = mat;

            clones.Add(clone);
        }

        watch.Stop();
        Debug.Log("Instanced clones: " + clones.Count + " in " + (int)watch.ElapsedMilliseconds);

        foreach(Tile tile in tiles) {
            tile.SetSeason(season);
        }

        foreach(Tile t in tiles) {
            t.SetDirty();
        }

        StartCoroutine(SeasonFadeOutCo(clones));

        _tileRegularMaterial.SetFloat("_Alpha", 0f);
        _tileFocusMaterial.SetFloat("_Alpha", 0f);

        _tileRegularMaterial.DOFloat(1f, "_Alpha", 1f);
        _tileFocusMaterial.DOFloat(1f, "_Alpha", 1f);
    }

    IEnumerator SeasonFadeOutCo(List<SpriteRenderer> clones)
    {
        _tileSeasonTransitionMaterial.DOFloat(0f, "_Alpha", 1f);

        yield return new WaitForSeconds(1f);

        foreach(var clone in clones) {
            GameObject.Destroy(clone.gameObject);
        }
    }

    List<Tile> _highlights = new List<Tile>();

    public void ClearHighlights()
    {
        if(_underworld != null) {
            _underworld.ClearHighlights();
        }

        if(_highlights.Count == 0) {
            return;
        }
        Debug.Log("Clear highlights");
        _tileRegularMaterial.DOFloat(1f, "_Saturation", 0.2f);
        _tileRegularMaterial.DOFloat(1f, "_Luminance", 0.2f);

        foreach(var t in _highlights) {
            t.tileMaterial = _tileRegularMaterial;
            t.cursor.highlight = TileCursor.Highlight.None;
        }
        _highlights.Clear();
    }

    public void HighlightTile(Tile t, TileCursor.Highlight highlight)
    {
        if(t.loc.underworld && _underworld != null) {
            _underworld.HighlightTile(t, highlight);
            return;
        }

        if(_highlights.Count == 0) {
            Debug.Log("Set highlights");
            _tileRegularMaterial.DOFloat(0.6f, "_Luminance", 0.2f);
            _tileRegularMaterial.DOFloat(0.4f, "_Saturation", 0.2f);
        }

        t.tileMaterial = _tileFocusMaterial;
        t.cursor.highlight = highlight;
        _highlights.Add(t);
    }

    public List<Tile> allTilesInUnderworldAndOverworld {
        get {
            List<Tile> result = new List<Tile>();
            foreach(Tile t in tiles) {
                result.Add(t);
            }

            if(_underworld != null) {
                foreach(Tile t in _underworld.tiles) {
                    result.Add(t);
                }
            }

            return result;
        }
    }

    public List<Tile> tiles = new List<Tile>();
    public Loc dimensions;

    public int DistanceToEdgeOfBoard(Loc loc)
    {
        int distLeft = loc.x;
        int distBot = loc.y;
        int distRight = dimensions.x - loc.x;
        int distTop = dimensions.y - loc.y;

        return Mathf.Min(distLeft, distBot, distRight, distTop);
    }

    public bool LocOnBoard(Loc loc)
    {
        if(loc.underworld && _underworld != null) {
            return _underworld.LocOnBoard(loc);
        }

        if(loc.x < 0 || loc.x >= dimensions.x || loc.y < 0 || loc.y >= dimensions.y) {
            return false;
        }

        var tile = tiles[loc.y*dimensions.x + loc.x];
        return tile != null;
    }

    public Tile GetTile(Loc loc)
    {
        if(loc.underworld && _underworld != null) {
            return _underworld.GetTile(loc);
        }

        if(LocOnBoard(loc) == false) {
            return null;
        }

        return tiles[loc.y*dimensions.x + loc.x];
    }

    public Tile GetTileAssert(Loc loc)
    {
        if(loc.underworld && _underworld != null) {
            return _underworld.GetTileAssert(loc);
        }

        return tiles[loc.y*dimensions.x + loc.x];
    }


    public void ClearEdges()
    {
        foreach(Tile t in tiles) {
            t.edges.Clear();
        }
    }

    public void SetupEdges()
    {
        ClearEdges();

        foreach(Tile t in tiles) {
            int index = 0;
            foreach(Loc adj in Tile.AdjacentLocs(t.loc)) {
                Tile adjTile = GetTile(adj);
                if(adjTile != null && adjTile.isvoid == false) {
                    Tile.Edge edge = new Tile.Edge() {
                        dest = adjTile,
                        direction = (Tile.Direction)index,
                    };

                    t.edges.Add(edge);
                }

                if(_underworld != null && t.underworldGate) {
                    adjTile = _underworld.GetTile(adj);
                    if(adjTile != null && adjTile.isvoid == false) {
                        Tile.Edge edge = new Tile.Edge() {
                            dest = adjTile,
                            direction = (Tile.Direction)index,
                        };

                        t.edges.Add(edge);

                        //setup edge in the opposite direction, up from the underworld.
                        edge = new Tile.Edge() {
                            dest = t,
                            direction = (Tile.Direction)((index+3)%6),
                        };

                        adjTile.edges.Add(edge);
                    }
                }

                ++index;
            }
        }
    }

    public bool IsTileHiddenInUnderworld(Loc loc)
    {
        if(loc.underworld) {
            return _underworldShown == false;
        } else if(loc.overworld) {
            return _underworldShown && _underworld.GetTile(loc).isvoid == false;
        } else {
            return false;
        }
    }

    bool _underworldShown = false;

    public void ShowUnderworld(GameMap underworld, bool underworldShown)
    {
        _underworldShown = underworldShown;

        foreach(Tile t in tiles) {
            Tile underTile = _underworld.GetTile(t.loc);

            if(underworldShown && underTile.isvoid == false && underTile.revealed) {
                t.isOverUnderworld = true;
            }

            underTile.isHiddenByUnderworld = false;
            t.isHiddenByUnderworld = false;
        }

        StartCoroutine(ShowUnderworldCo(underworld, underworldShown));
    }

    IEnumerator ShowUnderworldCo(GameMap underworld, bool underworldShown)
    {
        underworld.gameObject.SetActive(true);

        var tween = _tileRegularMaterial.DOFloat(underworldShown ? 1f : 0f, "_ShowingUnderworld", 0.4f);
        var tween2 = _tileFocusMaterial.DOFloat(underworldShown ? 1f : 0f, "_ShowingUnderworld", 0.4f);

        underworld._tileRegularMaterial.DOFloat(underworldShown ? 0f : 1f, "_ShowingUnderworld", 0.4f);
        underworld._tileFocusMaterial.DOFloat(underworldShown ? 0f : 1f, "_ShowingUnderworld", 0.4f);

        yield return tween.WaitForCompletion();
        yield return tween2.WaitForCompletion();

        foreach(Tile t in tiles) {
            Tile underTile = underworld.GetTile(t.loc);

            underTile.isHiddenByUnderworld = (underworldShown == false);
            t.isHiddenByUnderworld = underworldShown && underTile.isvoid == false && underTile.revealed;
        }

        underworld.gameObject.SetActive(underworldShown);
    }

    private void Awake()
    {
        _tileRegularMaterial.SetFloat("_Alpha", 1f);
        _tileRegularMaterial.SetFloat("_Saturation", 1f);
        _tileRegularMaterial.SetFloat("_Luminance", 1f);
    }

    bool _init = false;

    public void Init()
    {
        if(_init) {
            return;
        }

        _init = true;

        foreach(var t in tiles) {
            t.tileMaterial = _tileRegularMaterial;
        }

        foreach(var tile in tiles) {
            tile.shroud.SetFog(true, null);
            tile.fog.SetFog(true, null);
        }

        if(_underworld != null) {
            _underworld.Init();
        }

        SetupEdges();
    }

    public void Clear()
    {
        ClearHighlights();

        _init = false;
        foreach(Tile tile in tiles) {
            GameObject.Destroy(tile.gameObject);
        }

        tiles.Clear();

        if(_underworld != null) {
            _underworld.Clear();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        _tileFocusMaterial.SetFloat("_Luminance", 1.1f + 0.1f*Mathf.Sin(Time.time*3f));
        _tileFocusMaterial.SetFloat("_Saturation", 1.1f + 0.1f*Mathf.Sin(Time.time*3f));
    }

    public bool AdjacentToOcean(Loc loc)
    {
        HashSet<Loc> oceanLocs = ocean;
        Loc[] adj = Tile.AdjacentLocs(loc);
        foreach(Loc a in adj) {
            if(oceanLocs.Contains(a)) {
                return true;
            }
        }

        return false;
    }

    //the ocean is a set of locs containing all hexes that are in a navigable waterway with
    //a connection of navigable waterways to the edge of the board.
    HashSet<Loc> _ocean = null;
    public HashSet<Loc> ocean {
        get {
            if(_ocean == null) {
                _ocean = new HashSet<Loc>();
                List<Pathfind.Region> waterRegions = Pathfind.FindRegions((Loc loc) => this.GetTile(loc).terrain.rules.navigableWaterway, this.dimensions);

                foreach(Pathfind.Region region in waterRegions) {
                    bool hasConnectionToEdgeOfBoard = false;
                    foreach(Loc loc in region.locs) {
                        if(loc.x == 0 || loc.y == 0 || loc.x == dimensions.x-1 || loc.y == dimensions.y-1) {
                            hasConnectionToEdgeOfBoard = true;
                        }
                    }

                    if(hasConnectionToEdgeOfBoard) {
                        foreach(Loc loc in region.locs) {
                            _ocean.Add(loc);
                        }
                    }
                }
            }

            return _ocean;
        }
    }
}
