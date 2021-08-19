using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG;
using DG.Tweening;

public enum Season
{
    Spring, Summer, Autumn, Winter
}

[System.Serializable]
public struct TileInfo
{
    public TerrainInfo baseTerrain, terrain;
    public bool underworldGate;
    public bool freshwater;
    public bool isvoid;

    public bool Equals(TileInfo o)
    {
        return baseTerrain.Equals(o.baseTerrain) && terrain.Equals(o.terrain) && underworldGate == o.underworldGate && freshwater == o.freshwater && isvoid == o.isvoid;
    }
}

public class Tile : MonoBehaviour
{
    public TileInfo tileInfo = new TileInfo();

    [SerializeField]
    TileLabel _tileLabelProto = null;

    TileLabel _tileLabel = null;

    string _labelText = null;

    public void SetLabel(string labelText, bool visible = true)
    {
        _labelText = labelText;
        if(visible) {
            if(_tileLabel == null) {
                _tileLabel = Instantiate(_tileLabelProto, transform);
                _tileLabel.tile = this;
            }

            _tileLabel.gameObject.SetActive(true);

            _tileLabel.SetText(_labelText);
        } else if(_tileLabel != null) {
            _tileLabel.gameObject.SetActive(false);
        }
    }

    public void RevealLabel()
    {
        if(hasHiddenLabel) {
            SetLabel(_labelText, true);
            _tileLabel.FadeIn();
        }
    }

    public string GetLabelText()
    {
        return _labelText;
    }

    public bool hasLabel {
        get {
            return string.IsNullOrEmpty(_labelText) == false;
        }
    }

    public bool hasHiddenLabel {
        get {
            return hasLabel && (_tileLabel == null || _tileLabel.gameObject.activeSelf == false);
        }
    }

    public void ClearLabel()
    {
        _labelText = "";
        if(_tileLabel != null) {
            _tileLabel.gameObject.SetActive(false);
        }
    }

    public bool hasBoon {
        get {
            return terrain.rules.village && (GameController.instance.gameState.GetLocOwnerInfo(loc).pastOwnersBitmap&(1 << GameController.instance.numPlayerTeam)) == 0;
        }
    }

    [SerializeField]
    SpriteRenderer _tileBoonIconPrefab = null;

    SpriteRenderer _tileBoonIcon = null;

    bool _buildingCompleted = false;
    VillageBuilding _building = null;

    VillageBuilding building {
        get { return _building; }
        set {
            if(_building != value) {
                _building = value;


                if(_building == null) {
                    if(_tileLabel != null) {
                        _tileLabel.SetBuildingIcon(null);
                    }
                } else {
                    if(_tileLabel == null) {
                        SetLabel("");
                    }
                    _tileLabel.SetBuildingIcon(_building, _fadeInBuildingIcon, _buildingCompleted);
                }

                _fadeInBuildingIcon = false;
            }
        }
    }

    bool _fadeInBuildingIcon = false;

    public void FadeInBuildingIcon()
    {
        _fadeInBuildingIcon = true;
    }

    private void Update()
    {
        bool shouldShowBoon = hasBoon;
        if(shouldShowBoon && _tileBoonIcon == null) {
            _tileBoonIcon = Instantiate(_tileBoonIconPrefab, transform);
            _tileBoonIcon.material = _currentMaterial;
        } else if(shouldShowBoon == false && _tileBoonIcon != null) {
            GameObject.Destroy(_tileBoonIcon.gameObject);
            _tileBoonIcon = null;
        }

        if(terrain.rules.village) {
            var info = GameController.instance.gameState.GetLocOwnerInfo(loc);
            if(info.building != null) {
                if(info.buildingCompleted != _buildingCompleted) {
                    building = null;
                }

                _buildingCompleted = info.buildingCompleted;
            }
            building = info.building;
        } else {
            building = null;
        }
    }

    [HideInInspector]
    public Pathfind.TilePathInfo pathInfo = new Pathfind.TilePathInfo();

    static Tile _mouseoverTile = null;
    public static Tile mouseoverTile {
        get {
            if(GamePanel.mouseoverPanel != null || GameConfig.modalDialog > 0) {
                return null;
            }

            return _mouseoverTile;
        }
        set {
            if(value != _mouseoverTile) {
                if(_mouseoverTile != null) {
                    _mouseoverTile.cursor.mouseover = false;
                }

                _mouseoverTile = value;

                GameController.instance.MouseoverTileChanged(_mouseoverTile);
            }
        }
    }

    static Tile _previewMoveTile = null;
    public static Tile previewMoveTile {
        get {
            return _previewMoveTile;
        }

        set {
            if(_previewMoveTile != value) {
                if(_previewMoveTile != null) {
                    _previewMoveTile.cursor.previewMove = false;
                }
                _previewMoveTile = value;

                if(_previewMoveTile != null) {
                    _previewMoveTile.cursor.previewMove = true;
                }
            }
        }
    }

    public static void UpdateMouseover()
    {
        //We update the mouse over tile in here so we can be sure
        //cursor.mouseover is only true if the cursor is over a tile
        //and not over any UI elements.
        if(mouseoverTile != null) {
            mouseoverTile.cursor.mouseover = true;
        } else if(_mouseoverTile != null) {
            _mouseoverTile.cursor.mouseover = false;
        }
    }

    [SerializeField]
    TileFog _fog;

    [SerializeField]
    TileFog _shroud;


    public TileFog fog { get { return _fog; } }
    public TileFog shroud { get { return _shroud; } }

    public bool fogged {
        get { return fog.hiddenFromPlayer || shroud.hiddenFromPlayer; }
    }

    public bool shrouded {
        get { return shroud.hiddenFromPlayer; }
    }

    public bool isvoid {
        get {
            return tileInfo.isvoid;
        }
        set {
            tileInfo.isvoid = value;
            RecalculateActive();
        }
    }

    bool _revealed = false;

    //A tile is revealed if it is unshrouded or any adjacent tiles are unshrouded.
    public bool revealed {
        get { return _revealed; }
        set {
            _revealed = value;
            RecalculateActive();
        }
    }

    bool _onScreen = false;
    public bool onScreen {
        get { return _onScreen; }
        set {
            _onScreen = value;
            RecalculateActive();
        }
    }

    public void RecalculateActive()
    {
        gameObject.SetActive(_isHiddenByUnderworld == false && isvoid == false && _onScreen && (revealed || map.underworld == false));
        renderer.gameObject.SetActive(_revealed);
    }

    [SerializeField]
    Canvas _debugCanvas;

    [SerializeField]
    TMPro.TextMeshProUGUI _debugTextField;

    string _debugText = null;
    public string debugText {
        get { return _debugText; }
        set {
            if(value != _debugText) {
                _debugText = value;
                _debugCanvas.gameObject.SetActive(string.IsNullOrEmpty(value) == false);
                if(value != null) {
                    _debugTextField.text = value;
                }
            }
        }
    }

    class RendererItor : IEnumerable<Renderer>
    {
        Tile _tile;
        public RendererItor(Tile tile)
        {
            _tile = tile;
        }

        public IEnumerator<Renderer> GetEnumerator()
        {
            yield return _tile._renderer;

            if(_tile._overlay != null) {
                yield return _tile._overlay.renderer;
            }

            if(_tile._adjOverlays != null) {
                foreach(var overlay in _tile._adjOverlays) {
                    yield return overlay.renderer;
                }
            }

            if(_tile._tileBoonIcon != null) {
                yield return _tile._tileBoonIcon;
            }

            if(_tile._castleWallsInstance != null) {
                foreach(var c in _tile._castleWallsInstance.castleWalls) {
                    yield return c.renderer;
                }
            }

            if(_tile._fog != null) {
                foreach(SpriteRenderer r in _tile._fog.renderers) {
                    yield return r;
                }
            }

            if(_tile.loot != null) {
                yield return _tile.loot.renderer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    RendererItor _renderers {
        get {
            return new RendererItor(this);
        }
    }

    [SerializeField]
    SpriteRenderer _underworldGatePrefab = null;

    SpriteRenderer _underworldGateRenderer = null;

    public bool underworldGate {
        get { return tileInfo.underworldGate; }
        set {
            tileInfo.underworldGate = value;
            CalculateNeedUnderworldGate();
        }
    }

    void CalculateNeedUnderworldGate()
    {
        if(tileInfo.underworldGate) {
            if(_underworldGateRenderer == null) {
                _underworldGateRenderer = Instantiate(_underworldGatePrefab, _renderer.transform);
                _underworldGateRenderer.gameObject.SetActive(true);
            }
        } else {
            if(_underworldGateRenderer != null) {
                GameObject.Destroy(_underworldGateRenderer.gameObject);
                _underworldGateRenderer = null;
            }
        }
    }

    [SerializeField]
    Flag _flagPrefab = null;

    Flag _flag = null;

    public Flag flag {
        get {
            if(_flag == null) {
                _flag = Instantiate(_flagPrefab, _renderer.transform);
            }

            return _flag;
        }
    }

    [SerializeField]
    Material _currentMaterial = null;

    public Material tileMaterial {
        get { return _currentMaterial; }
        set {
            if(value != _currentMaterial) {
                _currentMaterial = value;
                SetMaterialOnRenderers();
            }
        }
    }

    void SetMaterialOnRenderers()
    {
        foreach(SpriteRenderer renderer in _renderers) {
            renderer.material = _currentMaterial;
        }
    }

    bool _isHiddenByUnderworld = false;
    public bool isHiddenByUnderworld {
        get { return _isHiddenByUnderworld; }
        set {
            _isHiddenByUnderworld = value;
            RecalculateActive();
        }
    }

    bool _isOverUnderworld = false;
    public bool isOverUnderworld {
        get { return _isOverUnderworld; }
        set {
            if(value != _isOverUnderworld) {
                _isOverUnderworld = value;
                SetOverUnderworldRenderers();
            }
        }
    }

    void SetOverUnderworldRenderers()
    {
        foreach(var renderer in _renderers) {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetFloat("_IsUnderworld", _isOverUnderworld ? 1.0f : 0.0f);
            renderer.SetPropertyBlock(block);
        }
    }

    public GameMap map = null;

    [SerializeField]
    LootItem _lootPrefab = null;

    [HideInInspector]
    public LootItem loot = null;

    public void AddLoot(Loot treasure, LootInfo lootInfo=null)
    {
        if(loot == null) {
            loot = Instantiate(_lootPrefab, _renderer.transform);
        }

        loot.SetLoot(treasure);
        loot.lootInfo = lootInfo;
    }

    [SerializeField]
    TileOverlay _overlayPrefab = null;

    TileOverlay _overlay = null;

    public TileCursor cursor;

    [SerializeField]
    TileAdjacentOverlay _adjOverlayPrefab = null;

    List<TileAdjacentOverlay> _adjOverlays = null;

    TileAdjacentOverlay GetAdjOverlay(int index)
    {
        if(_adjOverlays == null) {
            _adjOverlays = new List<TileAdjacentOverlay>();
        }

        while(_adjOverlays.Count <= index) {
            _adjOverlays.Add(Instantiate(_adjOverlayPrefab, _renderer.transform));
        }

        return _adjOverlays[index];
    }

    void SetNumAdjOverlays(int ncount)
    {
        if(_adjOverlays == null) {
            return;
        }

        for(int i = ncount; i < _adjOverlays.Count; ++i) {
            GameObject.Destroy(_adjOverlays[i].gameObject);
        }

        _adjOverlays.RemoveRange(ncount, _adjOverlays.Count-ncount);
    }

    [SerializeField]
    TileCastleWallCollection _castleWallsPrefab = null;

    TileCastleWallCollection _castleWallsInstance = null;

    TileCastleWallCollection _castleWalls {
        get {
            if(_castleWallsInstance == null) {
                _castleWallsInstance = Instantiate(_castleWallsPrefab, _renderer.transform);
            }

            return _castleWallsInstance;
        }
    }

    void DestroyCastleWalls()
    {
        if(_castleWallsInstance != null) {
            GameObject.Destroy(_castleWallsInstance.gameObject);
            _castleWallsInstance = null;
        }
    }

    public SpriteRenderer renderer {
        get {
            return _renderer;
        }
    }

    [SerializeField]
    SpriteRenderer _renderer;

    public static float Height = 1f;
    public static float Width = 1f;

    public Vector3 unitPos {
        get { return LocToPos(loc) + new Vector3(0f, terrain.yadj, 0f); }
    }

    public static bool IsLocOnBoard(Loc loc, Loc dim)
    {
        return loc.x >= 0 && loc.y >= 0 && loc.x < dim.x && loc.y < dim.y;
    }

    public static Vector3 LocToPos(Loc loc)
    {
        float halfhex = loc.x%2 == 1 ? 0.5f : 0f;
        return new Vector3(((float)loc.x)*Width*0.75f, loc.y + halfhex, 0f);
    }

    public static int DistanceBetween(Loc a, Loc b)
    {
        //make it so a is on the left.
        if(b.x < a.x) {
            Loc c = a;
            a = b;
            b = c;
        }

        //the distance we have to move horizontally.
        int xdelta = Mathf.Abs(a.x - b.x);

        //by moving horizontally we get some vertical movement
        //for 'free'. Find out the min and max y values we will
        //have based on if we move north-east vs south-east.
        int miny = a.y - xdelta/2;
        int maxy = a.y + xdelta/2;

        if(xdelta%2 == 1) {
            if(a.x%2 == 1) {
                maxy++;
            } else {
                miny--;
            }
        }

        if(b.y >= miny && b.y <= maxy) {
            return xdelta;
        } else if(b.y < miny) {
            return xdelta + (miny - b.y);
        } else {
            return xdelta + (b.y - maxy);
        }
    }

    public static Loc LocInDirection(Loc loc, Direction dir)
    {
        switch(dir) {
            case Direction.North:
                return new Loc(loc.x, loc.y+1, loc.depth);
            case Direction.NorthEast:
                return new Loc(loc.x+1, loc.y + (loc.x%2 == 0 ? 0 : 1), loc.depth);
            case Direction.SouthEast:
                return new Loc(loc.x+1, loc.y + (loc.x%2 == 0 ? -1 : 0), loc.depth);
            case Direction.South:
                return new Loc(loc.x, loc.y-1, loc.depth);
            case Direction.SouthWest:
                return new Loc(loc.x-1, loc.y + (loc.x%2 == 0 ? -1 : 0), loc.depth);
            case Direction.NorthWest:
                return new Loc(loc.x-1, loc.y + (loc.x%2 == 0 ? 0 : 1), loc.depth);
        }

        return loc;
    }

    public static Loc[] AdjacentLocs(Loc loc)
    {
        Loc[] result = new Loc[6];
        for(int i = 0; i != 6; ++i) {
            result[i] = LocInDirection(loc, (Direction)i);
        }

        return result;
    }

    public static Loc[] SelfAndAdjacentLocs(Loc loc)
    {
        Loc[] result = new Loc[7];
        result[0] = loc;
        for(int i = 0; i != 6; ++i) {
            result[i+1] = LocInDirection(loc, (Direction)i);
        }

        return result;
    }


    public static HashSet<Loc> LocsAndAdjacent(HashSet<Loc> input)
    {
        HashSet<Loc> result = new HashSet<Loc>();
        foreach(Loc loc in input) {
            result.Add(loc);
            foreach(Loc adj in loc.adjacent) {
                result.Add(adj);
            }
        }

        return result;
    }

    public static Direction DirOfLoc(Loc src, Loc dst)
    {
        for(int i = 0; i != 6; ++i) {
            if(LocInDirection(src, (Direction)i) == dst) {
                return (Direction)i;
            }
        }

        return Direction.None;
    }

    public Tile[] adjacentTiles {
        get {
            Tile[] result = new Tile[6];
            Loc[] adj = Tile.AdjacentLocs(loc);
            if(map != null) {
                for(int i = 0; i != 6; ++i) {
                    result[i] = map.GetTile(adj[i]);
                }
            }

            return result;
        }
    }

    public static Loc[] GetTilesInRing(Loc center, int radius)
    {
        if(radius == 0) {
            return new Loc[] { center };
        }

        Loc[] result = new Loc[6*radius];
        int index = 0;
        Loc pos = center + new Loc(0, radius);
        Direction[] dirs = new Direction[] { Direction.SouthEast, Direction.South, Direction.SouthWest, Direction.NorthWest, Direction.North, Direction.NorthEast};
        foreach(Direction dir in dirs) {
            for(int i = 0; i < radius; ++i) {
                result[index++] = pos;
                pos = LocInDirection(pos, dir);
            }
        }

        return result;
    }

    public static List<Loc> GetTilesInRadius(Loc center, int radius)
    {
        List<Loc> result = new List<Loc>();
        for(int i = 0; i <= radius; ++i) {
            var ring = GetTilesInRing(center, i);
            foreach(Loc r in ring) {
                result.Add(r);
            }
        }

        return result;
    }

    public Loc loc;

    public Unit unit;

    public bool freshwater {
        get {
            return tileInfo.freshwater;
        }
        set {
            tileInfo.freshwater = value;
        }
    }

    TerrainInfo _baseTerrain {
        get {
            return tileInfo.baseTerrain;
        }
        set {
            tileInfo.baseTerrain = value;
        }
    }

    TerrainInfo _terrain {
        get {
            return tileInfo.terrain;
        }
        set {
            tileInfo.terrain = value;
        }
    }

    public TerrainInfo terrain {
        get {
            if(_terrain.ground != null) {
                return _terrain;
            }

            return _baseTerrain;
        }
        set {
            _baseTerrain = _terrain = value;
        }
    }

    bool _dirty = false;
    public void SetDirty()
    {
        _dirty = true;
        if(gameObject.activeSelf) {
            CalculatePosition();
        }
    }

    public void SetSeason(Season season)
    {
        _terrain = _baseTerrain.GetSeasonalTerrain(season, freshwater);
    }

    public float waterline { get { return _terrain.waterline; } }

    [System.Serializable]
    public enum Direction
    {
        North, NorthEast, SouthEast, South, SouthWest, NorthWest, None
    }

    public static string[] DirectionStr = new string[] { "n", "ne", "se", "s", "sw", "nw" };

    public static bool DirectionIsVertical(Direction dir)
    {
        return dir == Direction.North || dir == Direction.South;
    }

    public static bool DirectionIsDiagonal(Direction dir)
    {
        return !DirectionIsVertical(dir);
    }

    public static Direction GetDirectionEastify(Direction dir)
    {
        if(dir == Direction.NorthWest) {
            dir = Direction.NorthEast;
        } else if(dir == Direction.SouthWest) {
            dir = Direction.SouthEast;
        }

        return dir;
    }

    public static Direction GetDirectionNorthSouth(Direction dir)
    {
        switch(dir) {
            case Direction.NorthWest:
            case Direction.North:
            case Direction.NorthEast:
                return Direction.North;
            case Direction.SouthWest:
            case Direction.South:
            case Direction.SouthEast:
                return Direction.South;
            default:
                return Direction.None;
        }
    }

    public struct Edge
    {
        public Tile dest;
        public Direction direction;
    }

    public List<Edge> edges = new List<Edge>();

    private void OnEnable()
    {
        if(_dirty) {
            CalculatePosition();
        }
    }

    public void CalculatePosition()
    {
        if(_terrain.invalid || _renderer == null) {
            return;
        }

        _dirty = false;

        if(map.underworld) {
            _shroud.gameObject.SetActive(false);
        }

        System.Random rng = new System.Random(loc.y*1024 + loc.x);
        transform.position = LocToPos(loc);
        _renderer.sprite = _terrain.sprites[rng.Next(_terrain.sprites.Length)];
        _renderer.sortingOrder = _terrain.zorder + loc.y;

        if(_terrain.hasOverlay && _overlay == null) {
            _overlay = Instantiate(_overlayPrefab, _renderer.transform);
        }

        if(_overlay != null) {
            _overlay.gameObject.SetActive(_terrain.hasOverlay);
            if(_terrain.hasOverlay) {
                _overlay.renderer.sprite = _terrain.overlaySprites[rng.Next(_terrain.overlaySprites.Length)];
            }
        }

        TerrainInfo[] adjTerrain = new TerrainInfo[6];

        int dir = 0;
        foreach(Tile adj in adjacentTiles) {
            if(adj != null && adj._terrain.valid && adj._terrain.adjZorder > _terrain.zorder && adj._terrain.HasAdjacent(dir)) {
                adjTerrain[dir] = adj._terrain;
            }

            ++dir;
        }

        int nCurIndex = 0;
        if(adjTerrain[0].valid && adjTerrain[5].Equals(adjTerrain[0])) {
            ++nCurIndex;
            while(nCurIndex != 0 && adjTerrain[nCurIndex].Equals(adjTerrain[nCurIndex-1])) {
                nCurIndex = (nCurIndex+1)%6;
            }
        }

        int adjIndex = 0;

        for(int i = 0; i != 6; ++i, nCurIndex = (nCurIndex+1)%6) {

            TerrainInfo adjTerrainType = adjTerrain[nCurIndex];
            if(adjTerrainType.invalid) {
                continue;
            }

            int nCount = 1;
            while(nCount < 6 && adjTerrain[(nCurIndex+nCount)%6].Equals(adjTerrainType)) {
                ++nCount;
            }

            Sprite sprite = adjTerrainType.GetAdjacent(nCurIndex, nCount);
            while(sprite == null && nCount > 1) {
                --nCount;
                sprite = adjTerrainType.GetAdjacent(nCurIndex, nCount);
            }

            for(int j = 0; j != nCount; ++j) {
                adjTerrain[(nCurIndex+j)%6] = TerrainInfo.None;
            }

            if(sprite != null) {
                TileAdjacentOverlay adjOverlay = GetAdjOverlay(adjIndex);
                adjOverlay.gameObject.SetActive(true);
                adjOverlay.renderer.sprite = sprite;
                ++adjIndex;
            }
        }

        SetNumAdjOverlays(adjIndex);

        //castle wall logic.

        if(_terrain.terrain.hasCastleWalls == false) {
            DestroyCastleWalls();
        } else {
            _castleWalls.CalculatePosition();

            Tile[] adj = adjacentTiles;
            bool[] directions = new bool[6];
            for(int i = 0; i != 6; ++i) {
                directions[i] = adj[i] != null && adj[i]._terrain.valid && adj[i]._terrain.Equals(_terrain) == false && adj[i]._terrain.zorder <= _terrain.zorder;
            }

            if(directions[(int)Direction.South]) {
                _castleWalls.WallBL.gameObject.SetActive(true);
                _castleWalls.WallBR.gameObject.SetActive(true);
                _castleWalls.WallBL.renderer.sprite = directions[(int)Direction.SouthWest] ? _terrain.terrain.CastleConvexBL : _terrain.terrain.CastleConcaveBL;
                _castleWalls.WallBR.renderer.sprite = directions[(int)Direction.SouthEast] ? _terrain.terrain.CastleConvexBR : _terrain.terrain.CastleConcaveBR;
            }

            if(directions[(int)Direction.SouthWest]) {
                _castleWalls.WallL.gameObject.SetActive(true);
                _castleWalls.WallL.renderer.sprite = directions[(int)Direction.NorthWest] ? _terrain.terrain.CastleConvexL : _terrain.terrain.CastleConcaveL;
            }

            if(directions[(int)Direction.SouthEast]) {
                _castleWalls.WallR.gameObject.SetActive(true);
                _castleWalls.WallR.renderer.sprite = directions[(int)Direction.NorthEast] ? _terrain.terrain.CastleConvexR : _terrain.terrain.CastleConcaveR;
            }


            if(directions[(int)Direction.NorthWest] || directions[(int)Direction.North]) {
                _castleWalls.WallTL.gameObject.SetActive(true);

                //HACK the position due to different sized sprites.
                Vector3 pos = _castleWalls.WallTL.transform.localPosition;
                pos.y = 0.8f;
                if(directions[(int)Direction.North] && directions[(int)Direction.NorthWest]) {
                    _castleWalls.WallTL.renderer.sprite = _terrain.terrain.CastleConvexTL;
                } else if(directions[(int)Direction.North]) {
                    _castleWalls.WallTL.renderer.sprite = _terrain.terrain.CastleConcaveBL;
                    pos.y = 0.95f;
                } else {
                    _castleWalls.WallTL.renderer.sprite = _terrain.terrain.CastleConvexR;
                }

                _castleWalls.WallTL.transform.localPosition = pos;
            }

            if(directions[(int)Direction.NorthEast] || directions[(int)Direction.North]) {
                _castleWalls.WallTR.gameObject.SetActive(true);
                if(directions[(int)Direction.North] && directions[(int)Direction.NorthEast]) {
                    _castleWalls.WallTR.renderer.sprite = _terrain.terrain.CastleConvexTR;
                } else if(directions[(int)Direction.North]) {
                    _castleWalls.WallTR.renderer.sprite = _terrain.terrain.CastleConvexBR;
                } else {
                    _castleWalls.WallTR.renderer.sprite = _terrain.terrain.CastleConvexL;
                }
            }

        }

        CalculateNeedUnderworldGate();

        if(_isOverUnderworld) {
            SetOverUnderworldRenderers();
        }

        SetMaterialOnRenderers();
    }

    public void OnMouseOver()
    {
        if(GameConfig.modalDialog <= 0) {
            mouseoverTile = this;
        }
    }

    public void OnMouseEnter()
    {
    }

    public void OnMouseExit()
    {
        if(_mouseoverTile == this) {
            mouseoverTile = null;
        }
    }

    public void OnMouseUpAsButton()
    {
        if(mouseoverTile == this && GameConfig.modalDialog == 0) {
            GameController.instance.TileClicked(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        RecalculateActive();
    }
}
