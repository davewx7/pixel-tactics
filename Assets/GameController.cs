using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using Unity.Profiling;
using DG.Tweening;


[System.Serializable]
public class GameState
{
    public string guid;
    public int seed;

    public int stateid = 1;

    public int seq = 1; //message sequencing, updated every time we upload a new move.

    public int difficulty = 0;

    public List<string> hintsExpired = new List<string>();
    public void AddHintExpired(string id)
    {
        if(hintsExpired.Contains(id) == false) {
            hintsExpired.Add(id);
        }
    }

    public List<SpeechPrompt> embargoedSpeechPrompts = new List<SpeechPrompt>();

    public List<SpeechPromptInstance> globalSpeechPrompts = new List<SpeechPromptInstance>();

    //keep track of equipment that has been awarded this game. Do not give
    //out the same equipment multiple times, because that's boring.
    public List<Equipment> equipmentAwarded = new List<Equipment>();

    public List<DiplomacyNode> diplomacyNodesExecuted = new List<DiplomacyNode>();

    public List<BaseSpawnEvent> spawnEvents = new List<BaseSpawnEvent>();

    public List<DungeonInfo> dungeonInfo = new List<DungeonInfo>();

    public DungeonInfo GetDungeon(string guid)
    {
        foreach(DungeonInfo dungeon in dungeonInfo) {
            if(dungeon.guid == guid) {
                return dungeon;
            }
        }

        return null;
    }

    public bool underworldUnlocked = false;

    public TeamInfo barbarianTeam = null;
    public int numBarbarianTeam = -1;
    public List<TeamInfo> teams;

    public HashSet<Loc> neutralVillages = new HashSet<Loc>();

    public List<AIState> aiStates = new List<AIState>();

    public bool IsTeamInGame(Team team)
    {
        foreach(var t in teams) {
            if(t.team == team) {
                return true;
            }
        }

        return false;
    }

    public TeamInfo GetTeamInfo(Team team)
    {
        foreach(var t in teams) {
            if(t.team == team) {
                return t;
            }
        }

        return null;
    }

    [System.Serializable]
    public struct BoonOfferRecord
    {
        public Boon boon;
        public int nround;
    }

    [SerializeField]
    List<BoonOfferRecord> _boonOfferRecords = new List<BoonOfferRecord>();

    public List<BoonOfferRecord> boonOfferHistory {
        get {
            return _boonOfferRecords;
        }
    }

    public int NumRoundsSinceBoonWasLastOffered(Boon boon)
    {
        for(int i = _boonOfferRecords.Count-1; i >= 0; --i) {
            if(_boonOfferRecords[i].boon == boon) {
                return nround - _boonOfferRecords[i].nround;
            }
        }

        return -1;
    }

    public void RecordBoonOffer(Boon boon)
    {
        _boonOfferRecords.Add(new BoonOfferRecord() { boon = boon, nround = nround });
    }

    public int GetLastBoonOfferRound(Boon boon)
    {
        for(int n = _boonOfferRecords.Count-1; n >= 0; --n) {
            var record = _boonOfferRecords[n];
            if(record.boon == boon) {
                return nround;
            }
        }

        return -1;
    }

    public int nround = 0;
    public int nturn = 0;

    [System.Serializable]
    public struct VillageOwnerInfo
    {
        public int currentOwner;
        public int pastOwnersBitmap;
        public int roundLastCaptured;
        public VillageBuilding building;
        public int buildingCreateRound;

        public int roundsUntilBuildingComplete {
            get {
                if(building == null) {
                    return -1;
                }

                if(buildingCreateRound == 0) {
                    return 0;
                }

                return building.timeCost - (GameController.instance.gameState.nround - buildingCreateRound);
            }
        }


        public bool buildingCompleted {
            get {
                return roundsUntilBuildingComplete <= 0;
            }
        }
    }

    public Dictionary<Loc, VillageOwnerInfo> owners = new Dictionary<Loc, VillageOwnerInfo>();

    public int GetBuildingGoldIncome(int nteam)
    {
        int result = 0;
        foreach(var p in owners) {
            if(p.Value.currentOwner == nteam && p.Value.building != null && p.Value.buildingCompleted) {
                result += p.Value.building.goldIncome;
            }
        }

        return result;
    }

    public int GetBuildingDevelopmentIncome(int nteam)
    {
        int result = 0;
        foreach(var p in owners) {
            if(p.Value.currentOwner == nteam && p.Value.building != null && p.Value.buildingCompleted) {
                result += p.Value.building.buildingIncome;
            }
        }

        return result;
    }

    public VillageOwnerInfo GetLocOwnerInfo(Loc loc)
    {
        VillageOwnerInfo info;
        if(owners.TryGetValue(loc, out info)) {
            return info;
        }

        return new VillageOwnerInfo() {
            currentOwner = -1,
            pastOwnersBitmap = 0,
            roundLastCaptured = -1,
            building = null,
        };
    }

    public Team GetTeamOwnerOfLoc(Loc loc)
    {
        int index = GetOwnerOfLoc(loc);
        if(index < 0) {
            return null;
        }

        return teams[index].team;
    }

    public int GetOwnerOfLoc(Loc loc)
    {
        VillageOwnerInfo info;
        if(owners.TryGetValue(loc, out info)) {
            return info.currentOwner;
        } else {
            return -1;
        }
    }

    public void SetVillageBuilding(Loc loc, VillageBuilding building, bool buildingAlreadyComplete=true)
    {
        var info = GetLocOwnerInfo(loc);
        info.building = building;
        info.buildingCreateRound = buildingAlreadyComplete ? 0 : nround;
        owners[loc] = info;
    }

    public VillageBuilding GetVillageBuildingBase(Loc loc)
    {
        VillageOwnerInfo info;
        if(owners.TryGetValue(loc, out info)) {
            return info.building;
        }

        return null;
    }

    public VillageBuilding GetVillageBuilding(Loc loc)
    {
        VillageOwnerInfo info;
        if(owners.TryGetValue(loc, out info)) {
            if(info.building != null && info.currentOwner >= 0) {
                TeamInfo teamInfo = teams[info.currentOwner];
                return info.building.GetUpgradedVersion(teamInfo);
            }
        }

        return null;
    }


    //returns true if this is a new capture of the village.
    public bool SetOwnerOfLoc(Loc loc, int nowner)
    {
        if(nowner >= 0) {
            neutralVillages.Remove(loc);
        }

        if(owners.ContainsKey(loc) && owners[loc].currentOwner == nowner) {
            return false;
        }

        bool firstTimeCapture = true;

        VillageOwnerInfo ownerInfo;
        
        if(owners.TryGetValue(loc, out ownerInfo)) {
            firstTimeCapture = (ownerInfo.pastOwnersBitmap&(1 << nowner)) == 0;

            if(ownerInfo.currentOwner >= 0 && GameController.instance.map.GetTile(loc).terrain.rules.village) {
                teams[ownerInfo.currentOwner].numVillages--;
            }
        } else {
            ownerInfo = new VillageOwnerInfo();
        }

        ownerInfo.currentOwner = nowner;
        if(nowner >= 0) {
            ownerInfo.pastOwnersBitmap = ownerInfo.pastOwnersBitmap | (1 << nowner);

            if(GameController.instance.map.GetTile(loc).terrain.rules.village) {
                teams[nowner].numVillages++;
            }
        }

        ownerInfo.roundLastCaptured = this.nround;

        owners[loc] = ownerInfo;
        return firstTimeCapture;
    }
}

public class GameController : MonoBehaviour
{
    [SerializeField]
    GameHarness _gameHarness = null;

    [SerializeField]
    GameLogPanel _gameLogPanel = null;

    [SerializeField]
    bool _autoSaveGame = true;

    [SerializeField]
    AttackWarning _attackWarningPrefab = null;

    [SerializeField]
    AttackPanel _attackPanelPrefab = null;

    AttackPanel _attackPanelInstance = null;

    public SpeechPromptQueue speechQueue = new SpeechPromptQueue();

    public bool uiHasFocus {
        get {
            return _gameLogPanel.isFocused;
        }
    }

    public bool GetButton(string buttonName)
    {
        if(uiHasFocus) {
            return false;
        }

        return Input.GetButton(buttonName);
    }

    public bool GetButtonDown(string buttonName)
    {
        if(uiHasFocus) {
            return false;
        }

        return Input.GetButtonDown(buttonName);
    }

    public void ShowAttackPreview(UnitInfo attacker, UnitInfo defender, AttackInfo attack, AttackInfo? counter)
    {
        ClearAttackPreview();
        _attackPanelInstance = Instantiate(_attackPanelPrefab, _attackPanelPrefab.transform.parent);
        if(localPlayerTurn == false) {
            _attackPanelInstance.SwapDisplays();
        }
        _attackPanelInstance.attacker = attacker;
        _attackPanelInstance.defender = defender;
        _attackPanelInstance.attackInfo = attack;
        _attackPanelInstance.counterattackInfo = counter;
        _attackPanelInstance.Init();
        _attackPanelInstance.gameObject.SetActive(true);
        _attackPanelInstance.FadeIn();
    }

    public void ClearAttackPreview()
    {
        if(_attackPanelInstance != null) {
            GameObject.Destroy(_attackPanelInstance.gameObject);
            _attackPanelInstance = null;
        }
    }

    public void FadeOutAttackPreview()
    {
        if(_attackPanelInstance != null) {
            _attackPanelInstance.FadeOut();
        }
    }

    static GameController _instance = null;
    public static GameController instance {
        get {
            if(_instance == null) {
                _instance = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
            }

            return _instance;
        }
        set {
            _instance = value;
        }
    }

    public MapGenerator mapGenerator;

    [SerializeField]
    UserErrorMessage _errorMessage = null;

    [SerializeField]
    Minimap _minimap = null;

    [SerializeField]
    GameCanvas _gameCanvas = null;

    [SerializeField]
    GameContextMenu _contextMenu = null;

    public void ShowContextMenu(List<GameContextMenu.Entry> entries)
    {
        _contextMenu.Show(entries);
    }

    [SerializeField]
    TurnBanner _turnBannerPrefab = null;

    [SerializeField]
    int _rngSeed = 0;
    ConsistentRandom _rng = null;

    public ConsistentRandom rng {
        get {
            if(_rng == null) {
                _rng = new ConsistentRandom(_rngSeed);
            }

            return _rng;
        }
    }

    public TeamInfo visionPerspective {
        get {
            if(overridePerspective != null) {
                return overridePerspective.teamInfo;
            }

            return playerTeamInfo;
        }
    }

    public int visionPerspectiveIndex {
        get {
            if(overridePerspective != null) {
                for(int n = 0; n != teams.Count; ++n) {
                    if(teams[n].team == overridePerspective) {
                        return n;
                    }
                }
            }

            return numPlayerTeam;
        }
    }

    public Team overridePerspective = null;

    bool _prevRevealEntireMap = false;
    public bool revealEntireMap = false;
    public bool debugAllTilesActive = false;
    public bool playerInvisible = false;
    public bool playerSkipTurn = false;
    public bool debugAllItemsAvailable = false;

    [Sirenix.OdinInspector.Button]
    public void RevealCoast()
    {
        ClearHighlights();
        foreach(Loc loc in map.dimensions.range) {
            if(map.AdjacentToOcean(loc)) {
                map.HighlightTile(map.GetTile(loc), TileCursor.Highlight.DebugMark);
            }
        }
    }

    public int wastesDist = 12;

    [Sirenix.OdinInspector.Button]
    public void RevealWastes()
    {
        ClearHighlights();

        foreach(Loc loc in map.dimensions.range) {
            if(map.AdjacentToOcean(loc)) {
                continue;
            }

            bool claimed = false;
            foreach(TeamInfo teamInfo in gameState.teams) {
                if(teamInfo.team.barbarian) {
                    continue;
                }

                if(teamInfo.aiState == null) {
                    continue;
                }

                if(teamInfo.aiState.coreTerritory.Contains(loc)) {
                    claimed = true;
                }
            }

            if(claimed == false) {
                map.HighlightTile(map.GetTile(loc), TileCursor.Highlight.DebugMark);
            }
        }
    }


    public int currentYear {
        get {
            return gameState.nround / GameConfig.instance.months.Length;
        }
    }

    public CalendarMonth currentMonth {
        get {
            return GameConfig.instance.months[gameState.nround%GameConfig.instance.months.Length];
        }
    }

    [SerializeField]
    bool _seasonOverrideUsed = false;

    [SerializeField]
    Season _seasonOverride = Season.Summer;

    public Season currentSeason {
        get {
            if(_seasonOverrideUsed)
                return _seasonOverride;
            return currentMonth.season.season;
        }
    }

    Season _renderedSeason = Season.Spring;

    void UpdateSeason()
    {
        if(currentSeason == _renderedSeason) {
            return;
        }

        //TurnBanner turnBanner = Instantiate(_turnBannerPrefab, _gameCanvas.transform);
        //turnBanner.Display(currentMonth);

        _renderedSeason = currentSeason;
        map.SeasonChange(currentSeason);
    }

    public FloatingLabel floatingLabelPrefab = null;

    [SerializeField]
    Transform _underworldButton = null, _underworldButtonHighlight = null;

    public bool underworldUnlocked {
        get { return gameState.underworldUnlocked; }
        set {
            if(value != gameState.underworldUnlocked) {
                gameState.underworldUnlocked = value;
                _underworldButton.gameObject.SetActive(underworldUnlocked);

                ShowDialogMessage("The Underworld", "You have stumbled into a gate to the <color=#FFFFFF>Underworld</color>! It is dark and dangerous in the world beneath, yet also filled with riches and treasures from worlds past. Press <color=#FFFFFF>f</color> to toggle the Underworld view or use the Underworld button in the top left.");
                StartCoroutine(HighlightUnderworldButtonCo());
            }
        }
    }

    IEnumerator HighlightUnderworldButtonCo()
    {
        bool firstTime = true;
        while(firstTime || (_conversationDialogInstance != null && _conversationDialogInstance.gameObject.activeSelf)) {
            firstTime = false;
            _underworldButtonHighlight.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            _underworldButtonHighlight.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    [SerializeField]
    EscapeMenu _escapeMenu = null;

    [SerializeField]
    SpellsDialog _spellsDialog = null;

    SpellsDialog _spellsDialogInstance = null;

    public void PrepareSpells()
    {
        if(_spellsDialogInstance != null) {
            ClosePrepareSpells();
            return;
        }

        if(GameConfig.modalDialog != 0 || localPlayerTurn == false) {
            return;
        }

        _spellsDialogInstance = Instantiate(_spellsDialog, transform);
        _spellsDialogInstance.unit = _unitStatusPanel.displayedUnit;
        _spellsDialogInstance.gameObject.SetActive(true);
    }

    public void ClosePrepareSpells()
    {
        GameObject.Destroy(_spellsDialogInstance.gameObject);
        _spellsDialogInstance = null;
    }

    [SerializeField]
    VillageDialog _villageDialog = null;

    VillageDialog _villageDialogInstance = null;

    public void EnterVillage()
    {
        CloseVillage();

        _villageDialogInstance = Instantiate(_villageDialog, transform);
        _villageDialogInstance.gameObject.SetActive(true);
        _villageDialogInstance.Init(_unitStatusPanel.displayedUnit);
    }

    public void CloseVillage()
    {
        if(_villageDialogInstance != null) {
            GameObject.Destroy(_villageDialogInstance.gameObject);
            _villageDialogInstance = null;
        }
    }


    [SerializeField]
    ShopDialog _shopDialog = null;

    ShopDialog _shopDialogInstance = null;

    public void VisitShop()
    {
        if(GameConfig.modalDialog > 0) {
            if(_shopDialogInstance != null) {
                CloseShop();
            }
            return;
        }

        if(localPlayerTurn == false) {
            return;
        }

        Unit unit = _unitStatusPanel.displayedUnit;
        VillageBuilding building = unit.buildingAtLoc;

        if(building != null) {
            building.UnitVisitingMarket(unit);
        }

        var market = currentTeamInfo.GetMarketUnitHasAccessTo(unit);
        if(market == null) {
            return;
        }

        _shopDialogInstance = Instantiate(_shopDialog, transform);
        _shopDialogInstance.priceMultiplier = market.priceMultiplier;
        _shopDialogInstance.equipment = market.equipment;
        _shopDialogInstance.unit = unit;
        _shopDialogInstance.gameObject.SetActive(true);
    }

    public void CloseShop()
    {
        GameObject.Destroy(_shopDialogInstance.gameObject);
        _shopDialogInstance = null;
    }

    [SerializeField]
    ConversationDialog _conversationDialog = null;

    ConversationDialog _conversationDialogInstance = null;

    public void CreateConversationDialog()
    {
        if(_conversationDialogInstance != null) {
            GameObject.Destroy(_conversationDialogInstance.gameObject);
        }

        _conversationDialogInstance = Instantiate(_conversationDialog, transform);
    }

    public void CloseConversationDialog()
    {
        if(_conversationDialogInstance != null) {
            _conversationDialogInstance.gameObject.SetActive(false);
        }
    }

    [SerializeField]
    DiplomacyDialog _diplomacyDialog = null;

    DiplomacyDialog _diplomacyDialogInstance = null;

    [SerializeField]
    EconomicDevelopmentDialog _economyDialog = null;

    EconomicDevelopmentDialog _economyDialogInstance = null;

    [SerializeField]
    ScoreDialog _scoreDialog = null;

    ScoreDialog _scoreDialogInstance = null;

    public void ShowScoreDialog(bool show)
    {
        if(_scoreDialogInstance != null) {
            GameObject.Destroy(_scoreDialogInstance.gameObject);
            _scoreDialogInstance = null;
        }

        if(show) {
            _scoreDialogInstance = Instantiate(_scoreDialog, _scoreDialog.transform.parent.transform);
            _scoreDialogInstance.gameObject.SetActive(show);
            _scoreDialogInstance.UpdateScores(teams[0]);
            _scoreDialogInstance.StartCoroutine(_scoreDialogInstance.AnimateScores(0f));
        }
    }

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    public UnitStatusPanel statusPanel {
        get {
            return _unitStatusPanel;
        }
    }

    [SerializeField]
    HintsPanel _hintsPanel = null;

    public void ClearCommandHints(GameCommand cmd)
    {
        _hintsPanel.OnCommandExecution(cmd);
    }

    [SerializeField]
    TMPro.TextMeshProUGUI _locText = null, _goldText = null, _villageText = null, _upkeepText = null;

    [SerializeField]
    List<GameCommand> _networkableCommands = null;

    public List<GameCommand> networkableCommands {
        get {
            return _networkableCommands;
        }
    }

    public GameCommand InstantiateNetworkCommand(int index)
    {
        if(index < 0 || index >= _networkableCommands.Count) {
            Debug.LogErrorFormat("Unknown network command: {0}", index);
        }
        return Instantiate(_networkableCommands[index], transform);
    }

    [SerializeField]
    GrantEquipmentCommand _grantEquipmentCommand = null;

    [SerializeField]
    SpeechPromptCommand _speechPromptCommand = null;

    [SerializeField]
    ScrollCameraCommand _scrollCameraProto = null;

    public ScrollCameraCommand CreateScrollCameraCmd(Loc loc)
    {
        ScrollCameraCommand cmd = Instantiate(_scrollCameraProto, transform);
        cmd.info = new ScrollCameraCommandInfo() {
            target = loc,
        };
        return cmd;
    }

    public IEnumerator ScrollCameraToCo(Loc loc, bool scrollToFog=false, Vector3 offset=default(Vector3))
    {
        var scrollCmd = CreateScrollCameraCmd(loc);
        scrollCmd.scrollToFog = scrollToFog;
        scrollCmd.offset = offset;
        scrollCmd.gameObject.SetActive(true);

        while(scrollCmd.finished == false) {
            yield return null;
        }

        GameObject.Destroy(scrollCmd.gameObject);
    }

    [SerializeField]
    CreateVillageBuildingCommand _createVillageBuildingCommand = null;

    [SerializeField]
    EndTurnCommand _endTurnCommand = null;

    [SerializeField]
    PrepareSpellsCommand _prepareSpellsCommand = null;

    [SerializeField]
    UnitMoveCommand _unitMoveCommand = null;

    [SerializeField]
    UnitAttackCommand _unitAttackCommand = null;

    [SerializeField]
    RecruitCommand _recruitCommand = null;

    [SerializeField]
    LevelUpCommand _levelupCommand = null;

    [SerializeField]
    RestCommand _restCommand = null;

    [SerializeField]
    CastSpellCommand _castSpellCommand = null;

    [SerializeField]
    DiscardEquipmentCommand _discardEquipmentCommand = null;

    [SerializeField]
    AwardBoonCommand _awardBoonCommand = null;

    [SerializeField]
    OfferQuestCommand _offerQuestCommand = null;

    [SerializeField]
    CreateBuildingCommand _createBuildingCommand = null;

    [SerializeField]
    BeginTurnEffectCommand _beginTurnEffectCommand = null;

    [SerializeField]
    UnitArriveAtDestinationCommand _unitArriveAtDestinationCommand = null;

    [SerializeField]
    StoryEventCommand _storyEventCommand = null;

    [SerializeField]
    PurchaseCommand _purchaseCommand = null;

    [SerializeField]
    GenericCommand _genericCommandProto = null;

    [SerializeField]
    DiplomacyCommand _diplomacyCommandProto = null;

    [SerializeField]
    Button _endTurnButton = null;

    List<Loc> _villageLocs = null;
    public List<Loc> villageLocs {
        get {
            if(_villageLocs == null) {
                _villageLocs = new List<Loc>();
                foreach(Tile t in map.tiles) {
                    if(t.terrain.rules.village) {
                        _villageLocs.Add(t.loc);
                    }
                }
            }

            return _villageLocs;
        }
    }

    public List<AIState> aiStates {
        get {
            return gameState.aiStates;
        }
    }

    public List<TeamInfo> teams { get { return gameState.teams; } }

    public GameState gameState = new GameState();

    public int numPlayerTeam = 0;
    public Team playerTeam { get { return teams[numPlayerTeam].team; } }
    public Team currentTeam { get { return teams[gameState.nturn].team; } }

    public TeamInfo playerTeamInfo { get { return teams[numPlayerTeam]; } }
    public TeamInfo currentTeamInfo { get { return teams[gameState.nturn]; } }

    public TeamInfo primaryEnemyTeamInfo {
        get {
            foreach(var t in teams) {
                if(t.team.primaryEnemy) {
                    return t;
                }
            }

            return null;
        }
    }

    [SerializeField]
    Unit _unitPrefab = null;

    [SerializeField]
    AttackDialog _attackDialogPrefab = null;

    AttackDialog _attackDialogInstance = null;

    [SerializeField]
    RecruitDialog _recruitDialogPrefab = null;

    RecruitDialog _recruitDialogInstance = null;

    [SerializeField]
    LevelUpDialog _levelUpDialogPrefab = null;

    LevelUpDialog _levelUpDialogInstance = null;

    [SerializeField]
    Image _turnIcon = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _roundNumberText = null;

    public GameCommandQueue commandQueue;

    public int currentTeamNumber {
        get { return gameState.nturn; }
    }

    public bool localPlayerTurn {
        get { return gameState.nturn == numPlayerTeam && spectating == false; }
    }

    public bool animating {
        get { return commandQueue.empty == false || GameConfig.modalDialog != 0; }
    }

    public List<Unit> units;
    public Dictionary<string, Unit> unitsByGuid = new Dictionary<string, Unit>();

    public GameMap map;
    public GameMap underworldMap;

    public void OnToggleDiplomacyDialog()
    {
        if(_diplomacyDialogInstance != null) {
            GameObject.Destroy(_diplomacyDialogInstance.gameObject);
            _diplomacyDialogInstance = null;
        } else {
            _diplomacyDialogInstance = Instantiate(_diplomacyDialog, transform);
            _diplomacyDialogInstance.gameObject.SetActive(true);
        }
    }

    public void OnToggleEconomyDialog(bool levelingUp=false)
    {
        if(_economyDialogInstance != null) {
            GameObject.Destroy(_economyDialogInstance.gameObject);
            _economyDialogInstance = null;
        } else {
            _economyDialogInstance = Instantiate(_economyDialog, transform);
            _economyDialogInstance.levelingUp = levelingUp;
            _economyDialogInstance.gameObject.SetActive(true);
        }
    }

    public void AddUnit(Unit u)
    {
        units.Add(u);
        unitsByGuid[u.unitInfo.guid] = u;
    }

    public void OnRecruit(Unit u)
    {
        Unit ruler = u.teamInfo.GetRuler();
        if(ruler != null) {
            ruler.teamInfo.lastRecruitLoc = ruler.loc;
        }

        if(u.team.ai != null && spectating == false) {
            u.team.ai.OnRecruit(u);
        }
    }

    public bool IsLocOccupied(Loc loc)
    {
        foreach(Unit u in units) {
            if(u.loc == loc) {
                return true;
            }
        }

        return false;
    }

    public Loc FindVacantTileNear(Loc loc, UnitInfo unit)
    {
        return FindVacantTileNear(loc, (Loc a) => 4f - unit.MoveCost(map.GetTile(a)) + (unit.isAquatic ? (map.AdjacentToOcean(a) ? 0f : -10000f) : 0f));
    }


    public Loc FindVacantTileNear(Loc loc, System.Func<Loc, float> predicate=null, List<Loc> exclude=null)
    {
        if(IsLocOccupied(loc) == false && (exclude == null || exclude.Contains(loc) == false)) {
            return loc;
        }

        int startIndex = rng.Next();

        for(int i = 1; i != 100; ++i) {
            Loc[] ring = Tile.GetTilesInRing(loc, i);

            Loc result = new Loc();
            float bestScore = 0f;
            for(int j = 0; j != ring.Length; ++j) {
                int index = (startIndex+j)%ring.Length;
                Loc r = ring[index];
                if(map.LocOnBoard(r) && !IsLocOccupied(r) && (exclude == null || exclude.Contains(r) == false)) {
                    if(predicate == null) {
                        return r;
                    }

                    float score = predicate(r);
                    if(score <= bestScore) {
                        continue;
                    } else {
                        bestScore = score;
                        result = r;
                    }
                }
            }

            if(result.valid) {
                return result;
            }
        }

        Debug.LogError("Could not find a vacant tile");
        return Loc.invalid;
    }

    public List<Unit> unitsOnCurrentTeam {
        get { return GetUnitsOnTeam(gameState.nturn); }
    }

    public List<Unit> GetUnitsOnTeam(int nteam)
    {
        List<Unit> result = new List<Unit>();
        foreach(var u in units) {
            if(u.unitInfo.ncontroller == nteam) {
                result.Add(u);
            }
        }
        return result;
    }

    public List<Unit> GetUnitsAndAlliesOfTeam(int nteam)
    {
        bool playerTeam = gameState.teams[nteam].team.player;

        List<Unit> result = new List<Unit>();
        foreach(var u in units) {
            if(u.unitInfo.ncontroller == nteam || (playerTeam && (u.teamInfo.playerDiplomacyStatus == Team.DiplomacyStatus.Ally || u.team.debugForceShareVisionWithPlayer))) {
                result.Add(u);
            }
        }
        return result;
    }


    public Unit GetUnitByGuid(string guid)
    {
        Unit result = null;
        unitsByGuid.TryGetValue(guid, out result);
        return result;
    }

    Unit _unitMouseover;
    public Unit unitMouseover {
        get { return _unitMouseover; }
        set {
            if(_unitMouseover != value) {
                _unitMouseover = value;
                RecalculateUnitDisplayedPaths();
            }
        }
    }

    Unit _unitDisplayed;
    public Unit unitDisplayed {
        get { return _unitDisplayed; }
        set {
            if(_unitDisplayed != value) {
                if(unitMoving == null) {
                    ClearHighlights();
                }

                _unitDisplayed = value;
                RecalculateUnitDisplayedPaths();
            }
        }
    }

    bool _unitDisplayedPathsDirty = false;

    void RecalculateUnitDisplayedPaths()
    {
        _unitDisplayedPathsDirty = true;
    }

    void UpdateUnitDisplayedPaths()
    {
        if(_unitDisplayedPathsDirty == false) {
            return;
        }

        _unitDisplayedPathsDirty = false;
        if(_unitDisplayed != null) {
            _unitStatusPanel.Init(_unitDisplayed);
            _unitStatusPanel.gameObject.SetActive(true);

            if(unitMoving == null && _unitDisplayed.team != playerTeam) {
                _unitPaths = Pathfind.FindPaths(this, _unitDisplayed.unitInfo, _unitDisplayed.unitInfo.movement, new Pathfind.PathOptions() {
                    excludeOccupied = false,
                    ignoreZocs = _unitDisplayed.unitInfo.isSkirmish,
                });
                foreach(var p in _unitPaths) {
                    Tile tile = map.GetTile(p.Value.dest);
                    if(tile != null) {
                        map.HighlightTile(tile, TileCursor.Highlight.Move);
                    }
                }
            }

            if(unitMoving == null && _unitDisplayed.team == playerTeam) {
                CalculateWhichEnemiesCanReach(_unitDisplayed, _unitDisplayed.tile);
            }
        }
    }

    public void RefreshUnitDisplayed(Unit unit=null, UnitInfo baselineUnit=null)
    {
        if(unit != null && unit != _unitDisplayed) {
            return;
        }

        if(_unitDisplayed != null) {
            _unitStatusPanel.Init(_unitDisplayed, baselineUnit);
        } else if(_unitStatusPanel.gameObject.activeSelf) {

            _unitStatusPanel.Refresh();
        }
    }

    struct AttackAfterMovingInfo
    {
        public List<Loc> sourceLocs;
        public Unit targetUnit;
    }

    Dictionary<Loc, Pathfind.Path> _unitPaths = null;
    Dictionary<Loc,Unit> _unitAttacks = null;
    Dictionary<Loc, AttackAfterMovingInfo> _unitAttacksAfterMoving = null;
    UnitSpell _unitSpell = null;
    HashSet<Loc> _unitSpellTargets = null;

    Equipment _unitDiscarding = null;
    HashSet<Loc> _unitDiscardTargets = null;

    [SerializeField]
    MapArrow _mapArrow = null;

    Unit _lastUnitClicked = null;

    public Unit lastUnitClicked {
        get { return _lastUnitClicked; }
        set {
            if(value != _lastUnitClicked) {
                if(_lastUnitClicked != null) {
                    _lastUnitClicked.lastClickedUnit = false;
                }

                _lastUnitClicked = value;

                if(value != null) {
                    value.lastClickedUnit = true;
                }
            }
        }
    }

    Unit _unitMoving = null;
    Unit _lastUnitMoving = null;
    public Unit unitMoving {
        get { return _unitMoving; }
        set {
            if(_unitMoving != value) {

                unitDisplayed = value;

                ClearHighlights();

                _unitMoving = value;

                if(_unitMoving == null) {
                    return;
                }

                _lastUnitMoving = value;
                
                _unitPaths = Pathfind.FindPaths(this, _unitMoving.unitInfo, _unitMoving.unitInfo.movementRemaining,
                      new Pathfind.PathOptions() {
                          ignoreZocs = _unitMoving.unitInfo.isSkirmish,
                          excludeOccupied = false,
                      });
                foreach(var p in _unitPaths) {
                    Tile tile = map.GetTile(p.Value.dest);
                    if(tile != null) {
                        map.HighlightTile(tile, TileCursor.Highlight.Move);
                    }
                }

                _unitAttacks = new Dictionary<Loc, Unit>();
                foreach(Unit target in value.PossibleAttacks()) {
                    if(target.ShouldEnterDiplomacy(_unitMoving)) {
                        map.HighlightTile(target.tile, TileCursor.Highlight.Diplomacy);
                    } else {
                        map.HighlightTile(target.tile, TileCursor.Highlight.Attack);
                    }
                    _unitAttacks[target.loc] = target;
                }

                _unitAttacksAfterMoving = new Dictionary<Loc, AttackAfterMovingInfo>();
                foreach(KeyValuePair<Loc, Pathfind.Path> p in _unitPaths) {
                    if(map.GetTile(p.Key).unit != null) {
                        continue;
                    }

                    foreach(Unit target in value.PossibleAttacks(p.Key)) {
                        if(target.ShouldEnterDiplomacy(_unitMoving)) {
                            map.HighlightTile(target.tile, TileCursor.Highlight.Diplomacy);
                        } else {
                            map.HighlightTile(target.tile, TileCursor.Highlight.Attack);
                        }

                        if(_unitAttacksAfterMoving.ContainsKey(target.loc) == false) {
                            _unitAttacksAfterMoving[target.loc] = new AttackAfterMovingInfo() {
                                sourceLocs = new List<Loc>(),
                                targetUnit = target,
                            };
                        }

                        _unitAttacksAfterMoving[target.loc].sourceLocs.Add(p.Key);
                    }
                }
            }
        }
    }

    public void SetCastingSpell(Unit caster, UnitSpell spell, HashSet<Loc> validTargets)
    {
        ClearHighlights();
        _unitMoving = caster;
        _unitSpell = spell;
        _unitSpellTargets = validTargets;
        foreach(Loc loc in validTargets) {
            map.HighlightTile(map.GetTile(loc), TileCursor.Highlight.Spell);
        }
    }

    public void SetDiscardingEquipment(Unit caster, Equipment equipment, HashSet<Loc> validTargets)
    {
        ClearHighlights();
        _unitMoving = caster;
        _unitDiscarding = equipment;
        _unitDiscardTargets = validTargets;
        foreach(Loc loc in validTargets) {
            map.HighlightTile(map.GetTile(loc), TileCursor.Highlight.Spell);
        }
    }

    public void ShowUserErrorMessage(string errorMessage)
    {
        _errorMessage.Show(errorMessage);
        Debug.Log("User error: " + errorMessage);
    }



    void ClearHighlights()
    {
        map.ClearHighlights();

        _unitMoving = null;
        _unitAttacksAfterMoving = null;
        _unitPaths = null;
        _unitAttacks = null;
        _unitSpell = null;
        _unitSpellTargets = null;
        _unitDiscarding = null;
        _unitDiscardTargets = null;

        _mapArrow.gameObject.SetActive(false);

        ClearAttackWarnings();

        RefreshUnitDisplayed();
    }

    

    public bool IsPathable(UnitInfo unit, Tile.Edge edge)
    {
        return true;
    }

    public void GameStartSetup()
    {
        for(int i = 0; i != _networkableCommands.Count; ++i) {
            _networkableCommands[i].id = i;
        }
    }

    private void Awake()
    {
    }

    Pathfind.Path FindPathToAttack(Tile t)
    {
        if(_unitAttacksAfterMoving == null || _unitAttacksAfterMoving.ContainsKey(t.loc) == false || t.unit == null || (_unitAttacks != null && _unitAttacks.ContainsKey(t.loc))) {
            return null;
        }

        AttackAfterMovingInfo info = _unitAttacksAfterMoving[t.loc];
        Loc bestSource = Loc.invalid;
        float bestDist = 0f;
        Vector2 mousePos = mousePointInWorld;
        foreach(Loc loc in info.sourceLocs) {
            Vector3 pos = Tile.LocToPos(loc);
            Vector2 delta = new Vector2(pos.x - mousePos.x, pos.y - mousePos.y);
            if(delta.magnitude < bestDist || bestSource.valid == false) {
                bestSource = loc;
                bestDist = delta.magnitude;
            }
        }

        if(_unitPaths != null && _unitPaths.ContainsKey(bestSource)) {
            return _unitPaths[bestSource];
        }

        return null;
    }

    public void BeginDiplomacy(Unit playerUnit, Unit aiUnit)
    {
        aiUnit.team.ai.ShowDiplomacy(aiStates[aiUnit.unitInfo.ncontroller], aiUnit, playerUnit);
    }

    public void PlayerMoveRequest(Pathfind.Path path, bool force=false)
    {
        if(force == false) {
            Team owner = gameState.GetTeamOwnerOfLoc(path.dest);

            if(owner != null && owner != currentTeam && currentTeam.IsEnemy(owner) == false && currentTeam.IsAlly(owner) == false) {
                StartCoroutine(MoveIntoFriendlyVillageWarning(path, owner));
                return;
            }
        }

        SendMoveCommand(path);
        ClearHighlights();
    }

    IEnumerator MoveIntoFriendlyVillageWarning(Pathfind.Path path, Team owningTeam)
    {
        string message = string.Format("This village is controlled by {0}, who we are currently on good terms with. Occupying it is likely to antagonize them. Are you sure you want to do this?", owningTeam.teamNameAsProperNoun);
        if(owningTeam.teamInfo.hasPlayerContact == false) {
            message = string.Format("A strange banner flies above this village. Occupying it without diplomacy first may antagonize them. Are you sure you want to do this?");
        }

        List<string> options = new List<string>();
        options.Add("We need this village. Occupy it.");
        options.Add("Oh wait, let me think about this.");
        ShowDialogMessage(new ConversationDialog.Info() {
            title = "Are you sure?",
            text = message,
            options = options,
            teletype = false,
        });
        yield return new WaitUntil(() => GameConfig.modalDialog == 0);

        bool occupy = (_conversationDialogInstance.optionChosen == 0);

        CloseConversationDialog();

        if(occupy) {
            PlayerMoveRequest(path, true);
        }
    }


    public void TileClicked(Tile t)
    {
        if(t != null && t.unit == null && lastUnitClicked != null && lastUnitClicked.unitInfo.nteam != numPlayerTeam) {
            lastUnitClicked = null;
        }

        if(animating || localPlayerTurn == false) {
            if(t.unit != null) {
                lastUnitClicked = t.unit;
            }
        }

        if(animating || localPlayerTurn == false || GameConfig.modalDialog > 0) {
            return;
        }

        if(_unitPaths != null && _unitMoving != null && _unitPaths.ContainsKey(t.loc) && GetUnitAtLoc(t.loc) == null) {
            PlayerMoveRequest(_unitPaths[t.loc]);
        } else if(_unitAttacks != null && _unitAttacks.ContainsKey(t.loc)) {
            Unit targetUnit = _unitAttacks[t.loc];
            if(targetUnit != null && targetUnit.ShouldEnterDiplomacy(_unitMoving)) {
                BeginDiplomacy(_unitMoving, targetUnit);
                ClearHighlights();
            } else {
                StartAttack(_unitMoving, targetUnit);
            }
        } else if(_unitSpell != null && _unitSpellTargets.Contains(t.loc)) {
            CastSpellCommand(_unitMoving, _unitSpell, t.loc);
            ClearHighlights();
        } else if(_unitDiscarding != null && _unitDiscardTargets.Contains(t.loc)) {
            DiscardEquipmentCommand(_unitMoving, _unitDiscarding, t.loc);
            ClearHighlights();
        } else if(FindPathToAttack(t) != null) {
            if(t.unit != null && t.unit.ShouldEnterDiplomacy(_unitMoving)) {
                SendMoveCommand(FindPathToAttack(t));
                BeginDiplomacy(_unitMoving, t.unit);
                ClearHighlights();
            } else {
                StartAttack(_unitMoving, t.unit, FindPathToAttack(t));
            }
        } else if(t.unit != null && t.unit.unitInfo.ncontroller == gameState.nturn) {
            unitMoving = t.unit;
            lastUnitClicked = t.unit;
        } else if(t.unit != null) {
            lastUnitClicked = t.unit;
        } else {

            StartRecruit(Tile.mouseoverTile.loc);
        }
    }

    public void SendMoveCommand(Pathfind.Path path)
    {
        QueuePlayerLookAt(path.dest, false);

        Debug.Log("SEND MOVE COMMAND FROM " + path.source.ToString() + " TO " + path.dest.ToString());

        UnitMoveCommand cmd = Instantiate(_unitMoveCommand, transform);
        cmd.info = new UnitMoveCommandInfo() {
            path = path,
        };

        cmd.Upload();

        commandQueue.QueueCommand(cmd);
    }

    public void ExecuteGrantEquipment(Unit unit, Equipment equip)
    {
        ExecuteGrantEquipment(unit, new List<Equipment>() { equip });
    }

    public void ExecuteGrantEquipment(Unit unit, List<Equipment> equip)
    {
        GrantEquipmentCommand cmd = Instantiate(_grantEquipmentCommand, transform);
        cmd.info = new GrantEquipmentInfo() {
            unitGuid = unit.unitInfo.guid,
            equipment = equip,
        };

        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public void ExecuteSpeechPrompt(Unit unit, SpeechPromptInstance instance)
    {
        SpeechPromptCommand cmd = Instantiate(_speechPromptCommand, transform);
        cmd.info = new SpeechPromptInfo() {
            prompt = instance,
            unitGuid = unit.unitInfo.guid,
        };

        commandQueue.QueueCommand(cmd);
    }

    public void ExecutePrepareSpells(Unit unit, List<UnitSpell> spells)
    {
        PrepareSpellsCommand cmd = Instantiate(_prepareSpellsCommand, transform);
        cmd.info = new PrepareSpellsCommandInfo() {
            unitGuid = unit.unitInfo.guid,
            spells = spells,
        };

        cmd.Upload();

        commandQueue.QueueCommand(cmd);
    }

    public void CastSpellCommand(Unit caster, UnitSpell spell, Loc target)
    {
        CastSpellCommand cmd = Instantiate(_castSpellCommand, transform);
        cmd.info = new CastSpellCommandInfo() {
            unitGuid = caster.unitInfo.guid,
            spell = spell,
            target = target,
        };

        cmd.Upload();

        commandQueue.QueueCommand(cmd);
    }

    public void DiscardEquipmentCommand(Unit caster, Equipment equip, Loc target)
    {
        DiscardEquipmentCommand cmd = Instantiate(_discardEquipmentCommand, transform);
        cmd.info = new DiscardEquipmentInfo() {
            unitGuid = caster.unitInfo.guid,
            equipment = equip,
            target = target,
        };

        cmd.Upload();

        commandQueue.QueueCommand(cmd);
    }

    public void TrashEquipmentCommand(Unit caster, Equipment equip)
    {
        StartCoroutine(TrashEquipmentCommandCo(caster, equip));
    }


    IEnumerator TrashEquipmentCommandCo(Unit caster, Equipment equip)
    {
        List<string> options = new List<string>();
        options.Add("Yes, discard this item.");
        options.Add("Oh wait, let me think about this");
        ShowDialogMessage(new ConversationDialog.Info() {
            title = "Are you sure?",
            text = "Do you really want to discard this item? It will be gone forever.",
            options = options,
            teletype = false,
        });

        yield return new WaitUntil(() => GameConfig.modalDialog == 0);

        bool trash = (_conversationDialogInstance.optionChosen == 0);

        CloseConversationDialog();

        if(trash) {
            DiscardEquipmentCommand cmd = Instantiate(_discardEquipmentCommand, transform);
            cmd.info = new DiscardEquipmentInfo() {
                unitGuid = caster.unitInfo.guid,
                equipment = equip,
                target = new Loc(),
            };

            cmd.Upload();
            commandQueue.QueueCommand(cmd);
        }
    }


    public bool StartRecruit(Loc loc)
    {
        bool locValid = false;
        Team rulerTeam = null;
        foreach(Unit unit in units) {
            bool isPlayerOrAlly = (unit.unitInfo.ncontroller == gameState.nturn || (currentTeam.player && unit.teamInfo.playerDiplomacyStatus == Team.DiplomacyStatus.Ally));
            if(unit.unitInfo.ruler && isPlayerOrAlly && map.GetTile(unit.loc).terrain.rules.keep) {
                Dictionary<Loc, Pathfind.Path> paths = Pathfind.FindPaths(this, unit.unitInfo, 1, new Pathfind.PathOptions() {
                    recruit = true,
                });

                Debug.Log("PATHS: " + paths.Count);

                if(paths.ContainsKey(loc)) {
                    rulerTeam = unit.team;
                    locValid = true;
                    break;
                }
            }
        }

        if(locValid == false) {
            return false;
        }

        ClearHighlights();

        TeamInfo teamInfo = gameState.GetTeamInfo(rulerTeam);

        List<UnitInfo> recruitOptions = teamInfo.availableRecruits;

        if(teamInfo.availableRecruitsInit == false) {
            recruitOptions = new List<UnitInfo>();

            if(rulerTeam.recruitmentSlots == -1) {
                foreach(UnitType unitType in rulerTeam.recruitmentOptions) {
                    recruitOptions.Add(unitType.createUnit(rng.Next(1, 65536), playerTeamInfo));
                }
            } else {
                List<UnitType> optionsRemaining = new List<UnitType>();
                int nslots = rulerTeam.recruitmentSlots;
                while(nslots > 0) {
                    if(optionsRemaining.Count == 0) {
                        optionsRemaining = new List<UnitType>(rulerTeam.recruitmentOptions);
                    }

                    int nseed = rng.Next(1, 65536);
                    int index = nseed%optionsRemaining.Count;
                    recruitOptions.Add(optionsRemaining[index].createUnit(nseed, playerTeamInfo));
                    optionsRemaining.RemoveAt(index);
                    --nslots;
                }
            }

            foreach(EconomyBuilding building in teamInfo.buildingsCompleted) {
                for(int i = 0; i < building.recruitSlots; ++i) {
                    UnitType unitType = rulerTeam.recruitmentOptions[rng.Next(0, rulerTeam.recruitmentOptions.Count)];
                    if(building.unitRecruits.Count > 0) {
                        unitType = building.unitRecruits[rng.Next(0, building.unitRecruits.Count)];
                    }

                    recruitOptions.Add(unitType.createUnit(rng.Next(1, 65536), playerTeamInfo));
                }
            }

            rulerTeam.teamInfo.availableRecruits = recruitOptions;
            rulerTeam.teamInfo.availableRecruitsInit = true;
        }

        if(recruitOptions.Count == 0) {
            ShowDialogMessage(new ConversationDialog.Info() {
                title = "No recruits available",
                text = "There are no more recruits available at this castle this turn.",
                teletype = false,
            });

            return true;
        }
        
        _recruitDialogInstance = Instantiate(_recruitDialogPrefab, transform);

        _recruitDialogInstance.units = recruitOptions;
        _recruitDialogInstance.recruitingTeam = rulerTeam;

        _recruitDialogInstance.gameObject.SetActive(true);
        _recruitDialogInstance.targetLoc = loc;

        return true;
    }

    public void OkayRecruit()
    {
        ExecuteRecruit(new RecruitCommandInfo() {
            payCost = true,
            loc = _recruitDialogInstance.targetLoc,
            unitType = _recruitDialogInstance.chosenUnitInfo.unitType,
            seed = _recruitDialogInstance.chosenUnitInfo.seed,
            haveHaste = GameController.instance.currentTeamInfo.unitsHaveHaste,
        });

        _recruitDialogInstance.recruitingTeam.teamInfo.availableRecruits.Remove(_recruitDialogInstance.chosenUnitInfo);

        CancelRecruit();
    }

    public void CancelRecruit()
    {
        GameObject.Destroy(_recruitDialogInstance.gameObject);
        _recruitDialogInstance = null;
    }

    public void UnitRest()
    {
        StartRest(_unitStatusPanel.displayedUnit);
        ClearHighlights();
    }

    public void StartRest(Unit unit, bool force=false)
    {
        if(force == false && unit.unitInfo.damageTaken == 0 && unit.unitInfo.HasSpellsOnCooldown == false && unit.unitInfo.status.Count == 0) {
            StartCoroutine(LongRestWarning(unit));
            return;
        }

        RestCommandInfo info = new RestCommandInfo() {
            guid = unit.unitInfo.guid,
        };

        RestCommand cmd = Instantiate(_restCommand, transform);
        cmd.info = info;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    IEnumerator LongRestWarning(Unit unit)
    {
        List<string> options = new List<string>();
        options.Add("Yes, I really want this unit to rest");
        options.Add("Oh wait, let me think about this");
        ShowDialogMessage(new ConversationDialog.Info() {
            title = "Are you sure?",
            text = "Resting allows a unit to fully recover hitpoints and restore access to any spells it may have. However, this unit doesn't seem to be injured or have expended spells. Resting requires the unit to sleep for a full turn during which it is vulnerable to attack. Are you sure you want to rest?",
            options = options,
            teletype = false,
        });

        yield return new WaitUntil(() => GameConfig.modalDialog == 0);

        bool rest = (_conversationDialogInstance.optionChosen == 0);

        CloseConversationDialog();

        if(rest) {
            StartRest(unit, true);
        }
    }

    public void LevelUp()
    {
        StartLevelUp(_unitStatusPanel.displayedUnit);
    }

    public void StartLevelUp(Unit unit)
    {
        _levelUpDialogInstance = Instantiate(_levelUpDialogPrefab, transform);
        _levelUpDialogInstance.targetUnit = unit;
        _levelUpDialogInstance.gameObject.SetActive(true);
    }

    public void OkayLevelUp()
    {
        ClearHighlights();
        ExecuteLevelUp(_levelUpDialogInstance.targetUnit.loc, _levelUpDialogInstance.chosenUnitInfo);
        GameObject.Destroy(_levelUpDialogInstance.gameObject);
    }

    public void ExecuteLevelUp(Loc loc, UnitInfo unitInfoLevelUp)
    {
        LevelUpCommandInfo info = new LevelUpCommandInfo() {
            loc = loc,
            unitInfo = unitInfoLevelUp,
        };

        LevelUpCommand cmd = Instantiate(_levelupCommand, transform);
        cmd.info = info;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);

    }

    public void CancelLevelUp()
    {
        GameObject.Destroy(_levelUpDialogInstance.gameObject);
    }

    public void OnLevelUp(Unit unit)
    {
        foreach(TeamInfo teamInfo in teams) {
            foreach(QuestInProgress quest in teamInfo.currentQuests) {
                quest.quest.OnUnitLeveled(unit, quest);
            }
        }
    }

    public void QueuePlayerLookAt(Loc loc, bool waitForCompletion=true)
    {
            ScrollCameraTo(loc, waitForCompletion);
    }

    //Safely spawn a unit, transferring to network.
    public void ExecuteSpawnUnit(UnitInfo unitInfo)
    {
        ExecuteRecruit(new RecruitCommandInfo() {
            unitGuid = unitInfo.guid,
            unitOverride = unitInfo,
            loc = unitInfo.loc,
            team = teams[unitInfo.nteam].team,
        });
    }

    public void ExecuteRecruit(RecruitCommandInfo info)
    {
        if(spectating) {
            return;
        }

        if(info.loc.underworld != _showUnderworld && map.GetTile(info.loc).fogged == false) {
            ToggleUnderworld();
        }

        QueuePlayerLookAt(info.loc);

        RecruitCommand cmd = Instantiate(_recruitCommand, transform);
        cmd.info = info;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public void StartAttack(Unit attacker, Unit target, Pathfind.Path attackerMove = null)
    {
        Loc attackerLocOverride = new Loc();
        if(attackerMove != null) {
            attackerLocOverride = attackerMove.dest;
        }

        ClearHighlights();

        _attackDialogInstance = Instantiate(_attackDialogPrefab, transform);
        _attackDialogInstance.attacker = attacker;
        _attackDialogInstance.defender = target;
        _attackDialogInstance.attackerLocOverride = attackerLocOverride;
        _attackDialogInstance.attackerPath = attackerMove;
        _attackDialogInstance.gameObject.SetActive(true);
    }

    public void OkayAttack()
    {
        Unit attacker = _attackDialogInstance.attacker;
        Unit target = _attackDialogInstance.defender;

        AttackInfo attack = _attackDialogInstance.chosenAttack;
        AttackInfo? counter = _attackDialogInstance.chosenCounter;

        string counterStr = null;
        if(counter.HasValue) {
            counterStr = counter.Value.id;
        }

        if(_attackDialogInstance.attackerPath != null) {
            //attacker must move into position first.
            SendMoveCommand(_attackDialogInstance.attackerPath);
        }

        GameObject.Destroy(_attackDialogInstance.gameObject);
        _attackDialogInstance = null;

        ExecuteAttack(attacker, target, attack.id, counterStr);
    }

    public void ExecuteAttack(Unit attacker, Unit target, string attackId, string counterId)
    {
        if(target.loc.underworld != _showUnderworld && target.tile.fogged == false) {
            ToggleUnderworld();
        }

        QueuePlayerLookAt(target.loc);

        ConsistentRandom rng = new ConsistentRandom();
        UnitAttackCommandInfo info = new UnitAttackCommandInfo() {
            attackerGuid = attacker.unitInfo.guid,
            targetGuid = target.unitInfo.guid,
            attackId = attackId,
            counterId = counterId,
            seed = rng.Next(),
        };

        UnitAttackCommand cmd = Instantiate(_unitAttackCommand, transform);
        cmd.info = info;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);

        if(attacker.team.player && _autoSaveGame) {
            _saveQueued = true;
        }
    }

    public void CancelAttack()
    {
        Debug.Log("Cancel attack");
        GameObject.Destroy(_attackDialogInstance.gameObject);
        _attackDialogInstance = null;
    }

    public void UnitMoveThroughLoc(Unit unit, Loc loc, int nstepsRemaining)
    {
        if(nstepsRemaining <= 1) {
            Tile tile = map.GetTile(loc);

            if(tile.loot != null) {
                tile.loot.AnimEnter(unit);
            }
        }
    }

    public void QueueUnitArriveAtDestination(Unit unit)
    {
        UnitArriveAtDestinationCommand cmd = Instantiate(_unitArriveAtDestinationCommand, transform);
        cmd.unitGuid = unit.unitInfo.guid;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public void QueueStoryEventCommand(StoryEventInfo eventInfo)
    {
        if(eventInfo == null || eventInfo.valid == false) {
            return;
        }

        StoryEventCommand cmd = Instantiate(_storyEventCommand, transform);
        cmd.info = eventInfo;
        commandQueue.QueueCommand(cmd);
    }

    public void QueuePurchaseCommand(PurchaseInfo info)
    {
        PurchaseCommand cmd = Instantiate(_purchaseCommand, transform);
        cmd.info = info;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public void QueueDiplomacyCommand(DiplomacyNodeInfo info)
    {
        DiplomacyCommand cmd = Instantiate(_diplomacyCommandProto, transform);
        cmd.info = info;
        commandQueue.QueueCommand(cmd);
    }

    public GenericCommandInfo QueueGenericCommand()
    {
        GenericCommandInfo result = new GenericCommandInfo();

        GenericCommand cmd = Instantiate(_genericCommandProto, transform);
        cmd.commandId = result.id;
        commandQueue.QueueCommand(cmd);

        return result;
    }

    public bool ForceCaptureLoc(Loc loc, int nteam)
    {
        Tile tile = map.GetTile(loc);
        if(tile.terrain.rules.capturable) {
            tile.flag.gameObject.SetActive(true);
            tile.flag.team = nteam;

            return gameState.SetOwnerOfLoc(loc, nteam);
        }

        return false;
    }

    public void UnitArriveAtDestination(Unit unit)
    {
        if(unit == null || unit.unitInfo.dead) {
            return;
        }

        Tile tile = map.GetTile(unit.loc);
        if(unit.unitInfo.level >= 1 && tile.terrain.rules.capturable && gameState.GetOwnerOfLoc(unit.loc) != unit.unitInfo.ncontroller) {
            int previousOwner = gameState.GetOwnerOfLoc(unit.loc);
            Team previousOwnerTeam = null;
            if(previousOwner >= 0) {
                previousOwnerTeam = gameState.teams[previousOwner].team;
            }

            if(previousOwnerTeam == null || previousOwnerTeam.IsAlly(unit.team) == false || previousOwnerTeam.teamInfo.GetRuler() == null) {

                bool firstTimeCapture = ForceCaptureLoc(unit.loc, unit.unitInfo.ncontroller);
                unit.unitInfo.movementExpended = 99;

                if(tile.terrain.rules.village && firstTimeCapture && unit.team.barbarian == false) {
                    //If the village has a name, display it.
                    tile.RevealLabel();
                }

                if(currentTeam.player && tile.terrain.rules.village && firstTimeCapture) {

                    unit.teamInfo.scoreInfo.villagesCaptured++;

                    if(spectating == false) {
                        //Get offered a boon when capturing a village.
                        GenericCommandInfo cmd = QueueGenericCommand();
                        StartCoroutine(OfferBoon(unit, tile, cmd, previousOwnerTeam));
                    }
                }
            }
        }

        foreach(TeamInfo teamInfo in teams) {
            foreach(QuestInProgress quest in teamInfo.currentQuests) {
                quest.quest.OnUnitArrivesAtLoc(unit, quest);
            }
        }

        if(unit.team.player) {
            if(unit.loc.underworld) {
                //remove any cave spawn points that are looted by a player moving here.
                List<DungeonInfo> newDungeons = new List<DungeonInfo>();
                foreach(var info in gameState.dungeonInfo) {
                    if(info.clearedLoc != unit.loc) {
                        newDungeons.Add(info);

                        if(info.interiorLocs.Contains(unit.loc)) {
                            info.playerEntered = true;
                        }
                    } else {
                        unit.teamInfo.scoreInfo.dungeonsLooted++;
                        foreach(QuestInProgress quest in playerTeamInfo.currentQuests) {
                            quest.quest.OnDungeonCleared(quest.dungeonGuid, quest);
                        }
                    }
                }

                gameState.dungeonInfo = newDungeons;
            }
        }

        if(tile.loot != null) {
            tile.loot.Loot(unit);
        }

        RecalculateVision();

        if(unit.playerControlled && tile.underworldGate) {
            ForceUnderworldShown();
            underworldUnlocked = true;
        }

        GameController.instance.RefreshUnitDisplayed();

        if(unit.gameObject.activeSelf && unit.teamInfo != playerTeamInfo) {
            foreach(Unit sighter in playerTeamInfo.GetUnits()) {
                if(sighter.lastCalculatedVision != null && sighter.lastCalculatedVision.ContainsKey(unit.loc)) {
                    SpeechPrompt.UnitSighted(sighter, unit);
                }
            }
        }
    }

    IEnumerator OfferBoon(Unit unit, Tile tile, GenericCommandInfo genericCmd, Team previousOwner)
    {
        Debug.Log("OFFERING BOON...");
        while(genericCmd.running == false) {
            yield return null;
        }

        string debugEligibleBoons = "";
        string debugIneligibleBoons = "";

        int nseed = rng.Next();

        int primaryBoonPriority = 0;
        int secondaryBoonPriority = 0;
        List<Boon> primaryBoons = new List<Boon>(), secondaryBoons = new List<Boon>();
        foreach(Boon boonItem in tile.terrain.villageInfo.boons) {
            Boon boon = boonItem.GetBoon(unit.team);

            if(boon.embargoAfterOffering > 0) {
                int nroundsSinceLastTime = gameState.NumRoundsSinceBoonWasLastOffered(boon);
                if(nroundsSinceLastTime >= 0 && nroundsSinceLastTime < boon.embargoAfterOffering) {
                    Debug.Log("Boon embargoed: " + boon.name);
                    continue;
                }
            }

            if(boon.allowOnlyInNeutralVillages && previousOwner != null && previousOwner.barbarian == false) {
                continue;
            }

            if(boon.CanOfferBoon(unit)) {
                debugEligibleBoons += boon.name + " ";
                if(boon.isPrimary) {
                    if(boon.priority < primaryBoonPriority) {
                        continue;
                    } else if(boon.priority > primaryBoonPriority) {
                        primaryBoonPriority = boon.priority;
                        primaryBoons.Clear();
                    }

                    primaryBoons.Add(boon);
                } else {
                    if(boon.priority < secondaryBoonPriority) {
                        continue;
                    } else if(boon.priority > secondaryBoonPriority) {
                        secondaryBoonPriority = boon.priority;
                        secondaryBoons.Clear();
                    }

                    secondaryBoons.Add(boon);
                }
            }
            else {
                debugIneligibleBoons += boon.name + " ";
            }
        }

        string debugPrimaryBoons = "";
        foreach(Boon p in primaryBoons) {
            debugPrimaryBoons += p.name + " ";
        }

        Debug.LogFormat("BOONS: Have {0} primary boons of priority {1}; eligible: {2}; ineligible: {3}; primary: {4}", primaryBoons.Count, primaryBoonPriority, debugEligibleBoons, debugIneligibleBoons, debugPrimaryBoons);

        if(secondaryBoons.Count < 2 && primaryBoons.Count == 0) {
            yield break;
        }

        //cull out primary boons offered more recently to have a jukeboxing type effect.
        var boonHistory = gameState.boonOfferHistory;
        int historyIndex = gameState.boonOfferHistory.Count-1;
        int numBoonsPreferred = Mathf.Max(2, (int)(primaryBoons.Count*0.3f));
        while(historyIndex >= 0 && primaryBoons.Count > numBoonsPreferred) {
            var entry = boonHistory[historyIndex];
            primaryBoons.Remove(entry.boon);
            --historyIndex;
        }

        List<Boon> boons = new List<Boon>();

        Boon primaryBoon = null;

        if(primaryBoons.Count > 0) {
            primaryBoon = primaryBoons[rng.Range(0, primaryBoons.Count)];
            boons.Add(primaryBoon);
        } else {
            int index = rng.Range(0, secondaryBoons.Count);
            boons.Add(secondaryBoons[index]);
            secondaryBoons.RemoveAt(index);
        }

        if(primaryBoon != null && primaryBoon.noSecondaryBoon) {
            secondaryBoons.Clear();
        }

        if(primaryBoon != null && primaryBoon.forcedSecondaryBoon != null) {
            boons.Add(primaryBoon.forcedSecondaryBoon);
        } else if(secondaryBoons.Count > 0) {
            boons.Add(secondaryBoons[rng.Range(0, secondaryBoons.Count)]);
        }

        CreateConversationDialog();

        ConversationDialog.Info conversationInfo = new ConversationDialog.Info() {
            title = "Villager's Greeting",
        };

        if(string.IsNullOrEmpty(tile.GetLabelText()) == false) {
            conversationInfo.title = string.Format("Entering {0}", tile.GetLabelText());
        }

        if(primaryBoon != null) {
            conversationInfo.text = primaryBoon.GetDialogStoryline(unit, nseed);
        } else {
            conversationInfo.text = "The villagers welcome you. They want to know what they can do to support you in your conquests.";
        }

        bool haveOptions = false;
        if(primaryBoon == null || primaryBoon.AllowOptions(unit)) {
            haveOptions = true;
            conversationInfo.options = new List<string>();
            foreach(Boon boon in boons) {
                if(primaryBoon != null && primaryBoon != boon) {
                    conversationInfo.options.Add(primaryBoon.DeclineSummaryText(unit, boon));
                } else {
                    conversationInfo.options.Add(boon.GetSummaryText(unit, nseed));
                }

                conversationInfo.optionTooltips.Add(boon.GetTooltipText(unit));
            }
        }

        if(primaryBoon && primaryBoon.GetAvatarSprite(unit, nseed) != null) {
            conversationInfo.primarySprite = primaryBoon.GetAvatarSprite(unit, nseed);
        } else {
            conversationInfo.primarySprite = tile.terrain.villageInfo.villagerPortrait;
        }

        _conversationDialogInstance.info = conversationInfo;

        _conversationDialogInstance.gameObject.SetActive(true);

        yield return new WaitUntil(() => _conversationDialogInstance.gameObject.activeSelf == false);

        Boon chosenBoon = haveOptions ? boons[_conversationDialogInstance.optionChosen] : primaryBoon;

        CloseConversationDialog();


        AwardBoonCommand cmd = Instantiate(_awardBoonCommand, transform);
        cmd.info = new AwardBoonInfo() {
            boon = chosenBoon,
            seed = nseed,
            unitGuid = tile.unit.unitInfo.guid,
            choices = boons,
            interactable = false,
        };

        cmd.Upload();

        cmd.info.interactable = true;

        cmd.gameObject.SetActive(true);
        while(cmd.finished == false || (_conversationDialogInstance != null && _conversationDialogInstance.gameObject.activeInHierarchy)) {
            yield return null;
        }

        //commandQueue.QueueCommand(cmd);

        genericCmd.Finish();
    }

    public void ShowDialogMessage(string title, string text, Sprite avatarSprite=null, List<string> options=null, ConversationDialog.Result result=null)
    {
        ConversationDialog.Info info = new ConversationDialog.Info() {
            title = title,
            text = text,
            primarySprite = avatarSprite,
            options = options,
        };
        ShowDialogMessage(info, result);
    }

    public void ShowDialogMessage(ConversationDialog.Info info, ConversationDialog.Result result=null)
    {
        GameConfig.modalDialog++;
        StartCoroutine(ShowDialogMessageCo(info, result));
    }

    IEnumerator ShowDialogMessageCo(ConversationDialog.Info info, ConversationDialog.Result result)
    {
        CreateConversationDialog();
        _conversationDialogInstance.info = info;

        GameConfig.modalDialog--;
        _conversationDialogInstance.gameObject.SetActive(true);

        yield return new WaitUntil(() => _conversationDialogInstance.gameObject.activeSelf == false);

        if(info.options != null && result != null) {
            result.optionChosen = _conversationDialogInstance.optionChosen;
        }

        if(result != null) {
            result.finished = true;
        }

        CloseConversationDialog();
    }

    public Unit GetUnitAtLoc(Loc loc)
    {
        Tile t = map.GetTile(loc);
        if(t != null) {
            return t.unit;
        }

        return null;
    }

    [SerializeField]
    SpriteRenderer _deathBackground = null;

    [SerializeField]
    DefeatBanner _defeatUI = null;

    [SerializeField]
    Image _defeatBGUI = null;

    //the unit that inflicted the death blow on the player.
    public Unit unitKillingPlayer = null;

    public void TriggerGameOver(bool victory=false)
    {
        Unit ruler = playerTeamInfo.GetRuler();

        _defeatUI.ruler = ruler;
        _defeatUI.victory = victory;

        _deathBackground.color = new Color(0f, 0f, 0f, 0f);
        _deathBackground.gameObject.SetActive(true);

        _deathBackground.DOColor(new Color(0f, 0f, 0f, 1f), 1f);

        this.enabled = false;

        StartCoroutine(GameOverCo(ruler));
    }

    IEnumerator GameOverCo(Unit ruler)
    {
        yield return new WaitForSeconds(1f);

        _defeatBGUI.gameObject.SetActive(true);
        _defeatBGUI.color = new Color(0f, 0f, 0f, 0f);
        _defeatBGUI.DOColor(new Color(0f, 0f, 0f, 1f), 0.5f);

        _saveState = null;
        _defeatUI.gameObject.SetActive(true);
    }

    public bool CheckUnitDeath(Unit unit, bool forceDead=false, bool playerInvolved=false)
    {
        if(forceDead || unit.unitInfo.dead) {

            if(playerInvolved) {
                QueueStoryEventCommand(unit.unitInfo.killedByPlayerEvent);

                if(unit.team.player == false) {
                    teams[0].scoreInfo.RecordEnemyKilled(unit.unitInfo.level);

                    foreach(TeamInfo teamInfo in teams) {
                        foreach(QuestInProgress quest in teamInfo.currentQuests) {
                            quest.quest.OnEnemyUnitKilled(unit, quest);
                        }

                        if(unit.team.barbarian == false && teamInfo.enemyOfPlayer == false && teamInfo.allyOfPlayer == false) {
                            if(teamInfo.team.IsEnemy(unit.team)) {
                                teamInfo.AddRelationsWithPlayerChange(string.Format("Fighting our enemies, {0}", unit.team.teamNameAsProperNoun), unit.unitInfo.level+1);
                            } else if(teamInfo.team.IsAlly(unit.team)) {
                                teamInfo.AddRelationsWithPlayerChange(string.Format("Fighting our friends, {0}", unit.team.teamNameAsProperNoun), -(unit.unitInfo.level+1));
                            }
                        }
                    }
                }
            }

            List<DungeonInfo> newDungeons = new List<DungeonInfo>();

            foreach(DungeonInfo dungeon in gameState.dungeonInfo) {
                if(dungeon.rulerGuid != unit.unitInfo.guid) {
                    newDungeons.Add(dungeon);
                } else {
                    unit.teamInfo.scoreInfo.dungeonsLooted++;
                    foreach(QuestInProgress quest in playerTeamInfo.currentQuests) {
                        quest.quest.OnDungeonCleared(quest.dungeonGuid, quest);
                    }
                }
            }

            gameState.dungeonInfo = newDungeons;

            if(unit.unitInfo.equipment.Count > 0) {
                List<Equipment> eligibleEquipment = new List<Equipment>();
                foreach(var equip in unit.unitInfo.equipment) {
                    if(equip.cursed == false && equip.consumable == false) {
                        eligibleEquipment.Add(equip);
                    }
                }

                if(eligibleEquipment.Count > 0) {
                    unit.tile.AddLoot(GameConfig.instance.deadBodyLoot, new LootInfo() {
                        description = unit.unitInfo.characterName,
                        equipment = eligibleEquipment,
                    });
                }
            }

            RemoveUnit(unit);

            if(unit.unitInfo.ruler && unit.team.unitsDieOnRulerDeath) {
                var units = unit.teamInfo.GetUnits();
                foreach(var u in units) {
                    if(u != null && u != unit && u.unitInfo.ruler == false) {
                        CheckUnitDeath(u, true);
                    }
                }
            }

            RecalculateVision();
            return true;
        }

        return false;
    }

    public void RemoveUnit(Unit unit)
    {
        unitsByGuid.Remove(unit.unitInfo.guid);
        units.Remove(unit);
        unit.Die();
    }

    public bool IsLocOnScreen(Loc loc)
    {
        float aspect = Screen.width/Screen.height;
        Vector3 targetPos = Tile.LocToPos(loc);
        Vector3 delta = targetPos - _pixelPerfectCamera.transform.position;
        return Mathf.Abs(delta.x) < 6f*aspect && Mathf.Abs(delta.y) < 6f;
    }

    public bool IsLocNearCenterOfScreen(Loc loc)
    {
        Vector3 targetPos = Tile.LocToPos(loc);
        Vector3 delta = targetPos - _pixelPerfectCamera.transform.position;
        return Mathf.Abs(delta.x) < 4f && Mathf.Abs(delta.y) < 3f;
    }

    public bool IsLocGoodPositionForUnitSpeech(Loc loc)
    {
        Vector3 targetPos = Tile.LocToPos(loc);
        Vector3 delta = targetPos - _pixelPerfectCamera.transform.position;
        return delta.x > -7f && delta.x < 2.5f && delta.y < 3f && delta.y > -5f;
    }


    public void ScrollCameraTo(Loc loc, bool waitForCompletion = true)
    {
        ScrollCameraCommand cmd = Instantiate(_scrollCameraProto, transform);
        cmd.info = new ScrollCameraCommandInfo() {
            target = loc,
        };
        cmd.instant = !_started;
        cmd.waitForCompletion = true;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public void CreateVillageBuildingCommand(Unit unit, VillageBuilding building)
    {
        CreateVillageBuildingCommand cmd = Instantiate(_createVillageBuildingCommand, transform);
        cmd.info = new CreateVillageBuildingCommandInfo() {
            unitGuid = unit.unitInfo.guid,
            building = building,
        };

        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public void CreateBuildingCmd(CreateBuildingCommand cmd)
    {
        StartCoroutine(CreateBuildingCmdCo(cmd));
    }

    IEnumerator CreateBuildingCmdCo(CreateBuildingCommand cmd)
    {
        Debug.LogFormat("XXX: Create building co: {0}", cmd.runningFromNetwork);
        ShowDialogMessage("Your Renown Grows", "As your renown among your subjects grows they come together to erect a new building in your honor. Choose what you will have them build you...");
        //ShowDialogMessage("Building Complete", string.Format("You have completed your {0}. {1}. You may now choose your next project...", building.buildingName, building.achievementText));
        yield return new WaitUntil(() => GameConfig.modalDialog == 0);

        playerTeamInfo.buildingProject = null;

        if(cmd.info.building == null) {
            Assert.IsFalse(cmd.runningFromNetwork);

            //the player chooses the building and then uploads this command to the network.
            OnToggleEconomyDialog(true);
            yield return new WaitUntil(() => GameConfig.modalDialog == 0);

            cmd.info.building = playerTeamInfo.buildingProject;
            cmd.Upload();
        } else {
            Assert.IsTrue(cmd.runningFromNetwork);
            playerTeamInfo.buildingProject = cmd.info.building;
        }

        var building = playerTeamInfo.buildingProject;

        playerTeamInfo.buildingsCompleted.Add(building);
        playerTeamInfo.buildingProject = null;

        ShowDialogMessage("Your Renown Grows", string.Format("Your people come together to build your {0}. {1}. You have made another step toward the crown!", building.buildingName, building.achievementText));

        foreach(var info in building.additionalAchievementText) {
            var dialogInfo = new ConversationDialog.Info() {
                title = info.title,
                text = info.message,
            };

            if(building.grantsEquipmentOnBuild != null) {
                dialogInfo.AddLink("equip", building.grantsEquipmentOnBuild.CreateTooltip());
            }

            ShowDialogMessage(dialogInfo);

            yield return new WaitUntil(() => GameConfig.modalDialog == 0);
        }

        if(building.grantsEquipmentOnBuild != null) {
            Unit ruler = playerTeamInfo.GetRuler();
            if(ruler != null) {
                ExecuteGrantEquipment(ruler, new List<Equipment>() { building.grantsEquipmentOnBuild });
            }
        }

        cmd.finished = true;
    }

    public void StartNewTurn()
    {
        if(localPlayerTurn && _endPlayerTurnTime > 0f && _aiTimerStopwatch != null) {
            float aiTime = Time.time - _endPlayerTurnTime;
            float commandTime = GameCommandQueue.elapsedTime - _endPlayerTurnCommandTotals;

            Debug.LogFormat("PERF: NEW TIMESTAMP: {0}; REAL TIME: {1}ms TOTAL AI TIME: {2} AI THINK TIME: {3}; COMMAND TIME: {4}", Time.time, _aiTimerStopwatch.ElapsedMilliseconds, aiTime, AI.aiThinkTime, commandTime);
        }

        SetRoundNumberText();

        currentTeamInfo.BeginTurn();

        //Make a copy of the list before iterating to guard against modification.
        List<Unit> unitsCopy = new List<Unit>(units);

        Unit ruler = null;
        foreach(Unit unit in unitsCopy) {
            if(unit.unitInfo.ncontroller == gameState.nturn) {
                unit.BeginTurn();

                if(unit.unitInfo.ruler) {
                    ruler = unit;
                }
            }
        }

        if(ruler != null && localPlayerTurn) {
            ScrollCameraTo(ruler.loc);
            RecalculateVision();
        }


        _turnIcon.color = currentTeam.coloring.color;

        if(currentTeam.ai != null && spectating == false) {
            Debug.Log("AI STATE: " + gameState.nturn + " / " + teams.Count + " / " + aiStates.Count);
            currentTeam.ai.NewTurn(aiStates[gameState.nturn]);
        }

        if(localPlayerTurn && _autoSaveGame) {
            _saveQueued = true;
        }

        if(localPlayerTurn && playerTeamInfo.economicDevelopment >= playerTeamInfo.scoreNeededForNextLevel) {
            CreateBuildingCommand cmd = Instantiate(_createBuildingCommand, transform);
            //gets uploaded with information about what was built when executing.
            commandQueue.QueueCommand(cmd);
        }
    }

    float _endPlayerTurnTime = -1f;
    float _endPlayerTurnCommandTotals = 0f;
    System.Diagnostics.Stopwatch _aiTimerStopwatch = null;

    static ProfilerMarker s_profileEndTurn = new ProfilerMarker("GameController.EndTurn");

    public void EndTurn()
    {
        using(s_profileEndTurn.Auto()) {
            if(localPlayerTurn && _autoSaveGame) {
                SaveGame();
                AI.aiThinkTime = 0f;
                Debug.LogFormat("PERF: EndTime: {0}", Time.time);
                _aiTimerStopwatch = System.Diagnostics.Stopwatch.StartNew();
                _endPlayerTurnTime = Time.time;
                _endPlayerTurnCommandTotals = GameCommandQueue.elapsedTime;
            }

            var cmd = Instantiate(_endTurnCommand, transform);
            cmd.Upload();
            commandQueue.QueueCommand(cmd);
        }
    }

    public void DoEndTurn()
    {
        currentTeamInfo.mapUnfogged = new HashSet<Loc>(_lastVision);

        if(localPlayerTurn) {
            RecalculateVision();
        }

        foreach(Unit unit in units) {
            if(unit.unitInfo.ncontroller == gameState.nturn) {
                unit.EndTurn();
            }
        }


        gameState.nturn++;
        if(gameState.nturn >= gameState.teams.Count) {
            gameState.nturn = 0;
            gameState.nround++;

            StartNewRound();
        }

        StartNewTurn();
    }

    public void StartNewRound()
    {
        UpdateSeason();

        TurnBanner turnBanner = Instantiate(_turnBannerPrefab, _gameCanvas.transform);
        turnBanner.Display(currentMonth);

        foreach(TeamInfo t in gameState.teams) {
            t.availableRecruitsInit = false;
            t.availableRecruits.Clear();
        }

        List<BaseSpawnEvent> newSpawnEvents = new List<BaseSpawnEvent>();
        foreach(BaseSpawnEvent spawnEvent in gameState.spawnEvents) {

            if(spawnEvent.TrySpawn()) {
                continue;
            }

            newSpawnEvents.Add(spawnEvent);
        }

        gameState.spawnEvents = newSpawnEvents;

        Debug.Log("DONE SPAWNING BARBARIANS");
    }

    public void EndTurnPressed()
    {
        ClearHighlights();
        EndTurn();
    }

    //What the player could see unfogged the last time we calculated.
    HashSet<Loc> _lastUnfogged = new HashSet<Loc>();

    //The vision of units when we last calculated. The player can
    //generally seen the union of all visions of units experienced this turn.
    HashSet<Loc> _lastVision = new HashSet<Loc>();

    void FullVisionRecalculation()
    {
        gameState.underworldUnlocked = true;

        foreach(Tile tile in map.tiles) {
            tile.revealed = true;
            RecalculateVisionForTile(playerTeamInfo, tile);
        }

        foreach(Tile tile in underworldMap.tiles) {
            if(tile.isvoid) {
                continue;
            }
            if(tile.revealed == false) {
                tile.revealed = true;
            }
            RecalculateVisionForTile(playerTeamInfo, tile);

        }
    }

    public void RecalculateVision(List<Tile> forceRefreshLocs = null)
    {
        HashSet<Tile> vision = null;
        vision = CalculateVision(visionPerspectiveIndex);

        TeamInfo teamInfo = visionPerspective;

        //all tiles that might need some kind of change/recalculation.
        HashSet<Tile> delta = new HashSet<Tile>();

        List<Tile> revealedUnderworldGates = new List<Tile>();

        List<Unit> revealedUnits = new List<Unit>();

        if(forceRefreshLocs != null) {
            foreach(Tile t in forceRefreshLocs) {
                Assert.IsNotNull(t);
                delta.Add(t);
            }
        }

        foreach(Tile tile in vision) {
            if(_lastUnfogged.Contains(tile.loc) == false) {
                Assert.IsNotNull(tile);
                delta.Add(tile);
            }

            if(tile.underworldGate) {
                revealedUnderworldGates.Add(tile);
            }

            if(tile.unit != null) {
                revealedUnits.Add(tile.unit);
            }
        }

        foreach(Loc loc in _lastUnfogged) {
            Tile tile = map.GetTile(loc);
            if(vision.Contains(tile) == false) {
                Assert.IsNotNull(tile);
                delta.Add(tile);
            }
        }

        HashSet<Tile> border = new HashSet<Tile>();
        foreach(Tile tile in delta) {
            foreach(Tile.Edge edge in tile.edges) {
                border.Add(edge.dest);
            }
        }

        foreach(Tile tile in border) {
            delta.Add(tile);
        }

        if(forceRefreshLocs != null) {
            foreach(Tile tile in forceRefreshLocs) {
                if(teamInfo.mapRevealed.Contains(tile.loc)) {
                    RevealTileAndAdjacent(tile);
                }
            }
        }

        foreach(Tile tile in vision) {
            teamInfo.mapRevealed.Add(tile.loc);
            teamInfo.mapUnfogged.Add(tile.loc);

            RevealTileAndAdjacent(tile);
        }

        _lastVision = new HashSet<Loc>();
        foreach(Tile t in vision) {
            _lastVision.Add(t.loc);
        }
        _lastUnfogged = teamInfo.mapUnfogged;

        foreach(Tile tile in delta) {
            RecalculateVisionForTile(teamInfo, tile);
        }

        _minimap.SetVisibleMap(teamInfo.mapRevealed);

        foreach(Unit sightedUnit in revealedUnits) {
            foreach(Unit sighter in playerTeamInfo.GetUnits()) {
                if(sighter.lastCalculatedVision != null && sighter.lastCalculatedVision.ContainsKey(sightedUnit.loc)) {
                    SpeechPrompt.UnitSighted(sighter, sightedUnit);
                }
            }
        }

        foreach(Tile sightedGate in revealedUnderworldGates) {
            foreach(Unit sighter in playerTeamInfo.GetUnits()) {
                if(sighter.lastCalculatedVision != null && sighter.lastCalculatedVision.ContainsKey(sightedGate.loc)) {
                    SpeechPrompt.UnderworldGateSighted(sighter, sightedGate);
                }
            }
        }
    }

    void RevealTileAndAdjacent(Tile tile)
    {
        if(tile.revealed == false) {
            RevealTile(tile);
        }

        foreach(Tile adj in tile.adjacentTiles) {
            if(adj == null) {
                continue;
            }

            if(adj.revealed == false) {
                RevealTile(adj);
            }
        }
    }

    void RevealTile(Tile tile)
    {
        tile.revealed = true;
        if(_showUnderworld && tile.loc.underworld) {
            Loc overworldLoc = new Loc(tile.loc.x, tile.loc.y);
            Tile overworldTile = map.GetTile(overworldLoc);
            overworldTile.isOverUnderworld = true;
            if(overworldTile.unit != null) {
                overworldTile.unit.hiddenInUnderworld = true;
            }
            if(tile.isvoid == false) {
                tile.gameObject.SetActive(true);
            }
        }
    }

    void RecalculateVisionForTile(TeamInfo teamInfo, Tile tile)
    {
        bool visible = revealEntireMap || teamInfo.mapUnfogged.Contains(tile.loc);
        bool revealed = revealEntireMap || teamInfo.mapRevealed.Contains(tile.loc);
        if(!revealed) {
            tile.shroud.SetFog(true, null);
            if(tile.unit != null) {
                tile.unit.isOnVisibleLoc = false;
            }
        }

        Loc loc = tile.loc;

        bool[] shroudDir = null;
        if(revealed) {
            foreach(Tile.Edge edge in tile.edges) {
                if(tile.loc.depth != edge.dest.loc.depth) {
                    //edges between underworld and overworld don't spread shroud.
                    continue;
                }

                if(!revealEntireMap && edge.dest != null && !teamInfo.mapRevealed.Contains(edge.dest.loc)) {
                    if(shroudDir == null) {
                        shroudDir = new bool[6];
                    }

                    shroudDir[(int)edge.direction] = true;
                }
            }
        }

        bool[] dir = null;
        if(visible) {
            foreach(Tile.Edge edge in tile.edges) {
                if(tile.loc.depth != edge.dest.loc.depth) {
                    //edges between underworld and overworld don't spread shroud.
                    continue;
                }

                if(!revealEntireMap && edge.dest != null && !teamInfo.mapUnfogged.Contains(edge.dest.loc)) {
                    if(dir == null) {
                        dir = new bool[6];
                    }

                    dir[(int)edge.direction] = true;
                }
            }
        }

        tile.shroud.SetFog(!revealed, shroudDir);
        tile.fog.SetFog(!visible, dir);
        
        if(tile.unit != null) {
            tile.unit.isOnVisibleLoc = tile.fog.atLeastPartlyVisibleToPlayer;
        }
    }

    public Dictionary<Loc, Pathfind.Path> CalculateUnitVision(Unit u)
    {
        bool elevated = u.unitInfo.isFlying || u.tile.terrain.rules.elevatedVision;
        if(u.tile.underworldGate || u.tile.loc.underworld) {
            elevated = false;
        }

        int vision = u.unitInfo.vision;
        if(elevated && vision < 8) {
            vision = 8;
        }

        u.lastCalculatedVision = Pathfind.FindPaths(this, u.unitInfo, vision, new Pathfind.PathOptions() {
            excludeOccupied = false,
            ignoreZocs = true,
            moveThroughEnemies = true,
            vision = true,
            ignoreTerrainCosts = elevated,
        });

        return u.lastCalculatedVision;
    }

    public HashSet<Tile> CalculateVision(int nteam)
    {
        HashSet<Tile> result = new HashSet<Tile>();

        //calculate 3 vision radius around owned keeps.
        foreach(var p in gameState.owners) {
            if(p.Value.currentOwner == nteam && map.GetTile(p.Key).terrain.rules.keep) {
                foreach(var loc in Tile.GetTilesInRadius(p.Key, 3)) {
                    if(map.LocOnBoard(loc)) {
                        result.Add(map.GetTile(loc));
                    }
                }
            }
        }

        var units = GetUnitsAndAlliesOfTeam(nteam);
        foreach(Unit u in units) {
            if(u.unitInfo.movementExpended > 0 && gameState.nturn == u.unitInfo.nteam) {
                if(u.unitInfo.expendedVision) {
                    continue;
                }

                u.unitInfo.expendedVision = true;
            }

            var paths = CalculateUnitVision(u);

            foreach(var p in paths) {
                result.Add(p.Value.destTile);
            }
        }
        
        HashSet<Tile> borders = new HashSet<Tile>();
        foreach(Tile t in result) {
            foreach(Tile.Edge edge in t.edges) {
                if(result.Contains(edge.dest) == false) {
                    borders.Add(edge.dest);
                }
            }
        }

        foreach(Tile t in borders) {
            result.Add(t);
        }

        return result;
    }

    bool _started = false;

    // Start is called before the first frame update
    void Start()
    {
        Debug.LogFormat("GameController GUID: {0}", gameState.guid);
        CloudInterface.instance.BeginPlayingGame(gameState.guid);
        //Glowwave.Json.UnitTest();
        //Glowwave.Json.UnitTestScriptableObject(GameConfig.instance.unitAbilityAquatic);
        RecalculateVision();

        _started = true;

        //heightmap.Program.RunPeaMap();
    }

    private void OnDisable()
    {
        CloudInterface.instance.StopPlayingGame();
    }

    public void GameLoaded()
    {
        Debug.Log("ON LOAD TILES REVEALED: " + playerTeamInfo.mapRevealed.Count);

        List<Tile> revealed = new List<Tile>();
        foreach(var loc in playerTeamInfo.mapRevealed) {
            if(map.LocOnBoard(loc)) {
                revealed.Add(map.GetTile(loc));
            }
        }

        RecalculateVision(revealed);

        var ruler = playerTeamInfo.GetRuler();
        if(ruler != null) {
            ScrollCameraTo(ruler.loc);
        }

        TurnBanner turnBanner = Instantiate(_turnBannerPrefab, _gameCanvas.transform);
        turnBanner.Display(currentMonth);
        SetRoundNumberText();
    }

    void SetRoundNumberText()
    {
        _roundNumberText.text = (gameState.nround+1).ToString();

        int nmonth = gameState.nround%GameConfig.instance.months.Length;
        string seasonPart = (nmonth%3 == 0 ? "early" : (nmonth%3 == 1 ? "the middle of" : "late"));
        string season = currentMonth.season.description;
        string tip = string.Format("Round {0} -- year {1}, month {2} -- {3} {4}", gameState.nround+1, currentYear+1, nmonth+1, seasonPart, season);

        UnitStatusPanel.SetTooltip(_roundNumberText, tip);
        UnitStatusPanel.SetTooltip(_turnIcon, tip);
    }

    public void StartNewGame()
    {
        SpeechPrompt.InitNewGame(playerTeam);

        StartNewRound();
        StartNewTurn();

        foreach(TeamInfo team in gameState.teams) {
            team.scoreInfo = new ScoreInfo();
        }
    }

    public void ActivateUI()
    {
        _gameCanvas.GetComponent<Canvas>().sortingLayerName = "UI";
        _gameCanvas.gameObject.SetActive(true);
        _pixelPerfectCamera.gameObject.SetActive(true);
    }

    public bool readyToDisplay {
        get {
            return _nupdates >= 2;
        }
    }

    int _nupdates = 0;

    bool _middleClickDrag = false;
    Vector3 _middleClickDragAnchor;
    Vector3 _middleClickDragCameraPos;

    float _basezoom = 5f;

    public float zoom {
        get { return _zoom; }
        set {
            if(value != _zoom) {
                if(_pixelPerfectCamera.enabled) {
                    _basezoom = _pixelPerfectCamera.GetComponent<Camera>().orthographicSize;
                }
                _zoom = value;
                if(value <= 1f) {
                    _pixelPerfectCamera.enabled = true;
                    _zoom = 1f;
                } else {
                    _pixelPerfectCamera.enabled = false;
                    _pixelPerfectCamera.GetComponent<Camera>().orthographicSize = _basezoom*_zoom;
                }
            }
        }
    }

    float _zoom = 1f;

    [SerializeField]
    UnityEngine.U2D.PixelPerfectCamera _pixelPerfectCamera = null;

    public Camera camera {
        get {
            return _pixelPerfectCamera.GetComponent<Camera>();
        }
    }

    public bool underworldDisplayed {
        get { return _showUnderworld; }
    }
    bool _showUnderworld = false;
    public void ToggleUnderworld()
    {
        Debug.Log("ToggleUnderworld");
        _showUnderworld = !_showUnderworld;
        map.ShowUnderworld(underworldMap, _showUnderworld);

        foreach(Unit unit in units) {
            bool unitHidden = false;
            if(unit.loc.underworld && _showUnderworld == false) {
                unitHidden = true;
            } else if(_showUnderworld && unit.loc.underworld == false && unit.tile.isOverUnderworld) {
                unitHidden = true;
            }

            unit.hiddenInUnderworld = unitHidden;
        }
    }

    public void ForceUnderworldShown()
    {
        if(_showUnderworld == false) {
            ToggleUnderworld();
        }
    }

    List<AttackWarning> _attackWarnings = new List<AttackWarning>();
    public List<AttackWarning> attackWarnings {
        get {
            if(_attackWarnings == null) {
                return new List<AttackWarning>();
            }
            return _attackWarnings;
        }
    }

    int _attackWarningCalculation = 1;

    int ClearAttackWarnings()
    {
        ++_attackWarningCalculation;
        if(_attackWarnings == null) {
            return _attackWarningCalculation;
        }

        foreach(var warning in _attackWarnings) {
            if(warning != null) {
                GameObject.Destroy(warning.gameObject);
            }
        }

        _attackWarnings = null;
        return _attackWarningCalculation;
    }

    int _attackWarningsProcessing = 0;
    public bool attackWarningsComplete {
        get { return _attackWarningsProcessing == 0; }
    }

    void CalculateWhichEnemiesCanReach(Unit unitMoving, Tile targetTile)
    {
        int attackWarningId = ClearAttackWarnings();

        _attackWarnings = new List<AttackWarning>();

        ++_attackWarningsProcessing;
        StartCoroutine(CalculateWhichEnemiesCanReachCo(attackWarningId, unitMoving, targetTile));
    }

    IEnumerator CalculateWhichEnemiesCanReachCo(int attackWarningId, Unit unitMoving, Tile targetTile)
    {
        List<Unit> unitList = new List<Unit>(units);

        var adjLocs = targetTile.loc.adjacent;

        foreach(Unit enemyUnit in unitList) {
            if(enemyUnit.team.player == false && enemyUnit.isOnVisibleLoc && (enemyUnit.IsEnemy(unitMoving) || enemyUnit.ShouldEnterDiplomacy(unitMoving))) {
                if(enemyUnit.unitInfo.movement+1 < Tile.DistanceBetween(enemyUnit.loc, targetTile.loc)) {
                    //can't possibly reach the player's unit with available movement.
                    continue;
                }

                yield return null;

                if(attackWarningId != _attackWarningCalculation) {
                    break;
                }

                Loc backupLoc = unitMoving.loc;
                unitMoving.loc = targetTile.loc;

                var paths = Pathfind.FindPaths(this, enemyUnit.unitInfo, enemyUnit.unitInfo.movement, new Pathfind.PathOptions() { ignoreZocs = enemyUnit.unitInfo.isSkirmish });

                bool canReach = false;
                foreach(Loc adj in adjLocs) {
                    if(paths.ContainsKey(adj)) {
                        canReach = true;
                        break;
                    }
                }

                bool canReachNoZocs = false;
                if(canReach == false) {
                    paths = Pathfind.FindPaths(this, enemyUnit.unitInfo, enemyUnit.unitInfo.movement, new Pathfind.PathOptions() { ignoreZocs = true, moveThroughEnemies = true });
                    foreach(Loc adj in adjLocs) {
                        if(paths.ContainsKey(adj)) {
                            canReachNoZocs = true;
                            break;
                        }
                    }
                }

                if(canReach || canReachNoZocs) {
                    AttackWarning warning = Instantiate(_attackWarningPrefab, transform);
                    warning.loc = enemyUnit.loc;
                    warning.unit = enemyUnit;
                    warning.theoretical = canReachNoZocs;
                    if(enemyUnit.ShouldEnterDiplomacy(unitMoving)) {
                        warning.SetDiplomacy();
                        if(enemyUnit.WantsContact(unitMoving) == false) {
                            warning.theoretical = true;
                        }
                    }

                    _attackWarnings.Add(warning);
                }

                unitMoving.loc = backupLoc;
            }
        }

        --_attackWarningsProcessing;
    }


    public void MouseoverTileChanged(Tile targetTile)
    {
        ClearAttackWarnings();

        if(targetTile != null && _unitMoving != null && _unitPaths != null && _unitPaths.ContainsKey(targetTile.loc) && targetTile.unit == null) {
            Pathfind.Path path = _unitPaths[targetTile.loc];
            ShowMapArrow(path);

            CalculateWhichEnemiesCanReach(_unitMoving, targetTile);

        } else {
            _mapArrow.gameObject.SetActive(false);
        }
    }

    void ShowMapArrow(Pathfind.Path path)
    {
        if(path.steps.Count <= 1) {
            _mapArrow.gameObject.SetActive(false);
            return;
        }

        List<Vector2> points = new List<Vector2>();

        foreach(Loc loc in path.steps) {
            points.Add(Tile.LocToPos(loc));
        }

        Vector2 basePoint = points[0];

        _mapArrow.transform.localPosition = new Vector3(basePoint.x, basePoint.y, 0f);

        for(int i = 0; i != points.Count; ++i) {
            points[i] -= basePoint;
        }

        _mapArrow.points = points;
        _mapArrow.Recalculate();
        _mapArrow.gameObject.SetActive(true);

    }

    public Vector2 mousePointInWorld {
        get {
            Ray ray = _pixelPerfectCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            Plane mapPlane = new Plane(Vector3.forward, 1f);
            float enter = 0f;
            if(mapPlane.Raycast(ray, out enter)) {
                Vector3 hitpoint = ray.GetPoint(enter);
                return new Vector2(hitpoint.x, hitpoint.y);
            }

            return Vector2.zero;
        }
    }

    Vector3 _lastCameraPos;

    HashSet<Tile> _tilesOnScreen = new HashSet<Tile>();

    void UpdateLocalPlayerTurn()
    {
        if(_saveQueued) {
            string data = SaveGame(gameState.nround > 0);
            _saveQueued = false;

            string path = string.Format("/games/{0}/snapshots/round{1}", gameState.guid, gameState.nround);
            CloudInterface.instance.PutData(path, data);
        }

        if(currentTeamInfo.numTilesRevealed < currentTeamInfo.mapRevealed.Count) {
            //record economic development for exploring the map.
            currentTeamInfo.scoreInfo.tilesDiscovered += currentTeamInfo.mapRevealed.Count - currentTeamInfo.numTilesRevealed;
            currentTeamInfo.numTilesRevealed = currentTeamInfo.mapRevealed.Count;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(spectating) {
            UpdateSpectate();
        }

        _gameLogPanel.UpdateLog();

        if(localPlayerTurn && playerSkipTurn && animating == false) {
            EndTurnPressed();
        }

        speechQueue.Update();

        DG.Tweening.DOTween.timeScale = timeScale;

        UpdateSeason();
        ++_nupdates;

        if(localPlayerTurn && animating == false)
        {
            UpdateLocalPlayerTurn();
        }

        if(_nupdates > 20) {
            _hintsPanel.ShowBestHint(_unitStatusPanel == null ? null : _unitStatusPanel.displayedUnit);
        }

        //_scoreDialog.gameObject.SetActive(Input.GetKey(KeyCode.L));
        //if(Input.GetKey(KeyCode.L)) {
        //    _scoreDialog.UpdateScores(teams[0]);
        //}

        if(debugAllTilesActive) {
            foreach(Tile tile in map.allTilesInUnderworldAndOverworld) {
                tile.onScreen = true;
            }
        }
        else if(_lastCameraPos != _pixelPerfectCamera.transform.position) {
            float aspect = ((float)Screen.width)/(float)Screen.height;
            int xdiff = (int)(8*aspect);

            HashSet<Tile> newTilesOnScreen = new HashSet<Tile>();
            Loc centerLoc = new Loc((int)(_pixelPerfectCamera.transform.position.x/0.75f), (int)_pixelPerfectCamera.transform.position.y);
            for(int x = centerLoc.x - xdiff; x <= centerLoc.x+xdiff; ++x) {
                for(int y = centerLoc.y - 6; y <= centerLoc.y+6; ++y) {
                    Loc loc = new Loc(x, y);
                    if(map.LocOnBoard(loc) == false) {
                        continue;
                    }

                    Tile t = map.GetTile(loc);
                    t.onScreen = true;
                    newTilesOnScreen.Add(t);

                    t = underworldMap.GetTile(loc);
                    t.onScreen = true;
                    newTilesOnScreen.Add(t);
                }
            }

            foreach(Tile t in _tilesOnScreen) {
                if(newTilesOnScreen.Contains(t) == false) {
                    t.onScreen = false;
                }
            }

            _tilesOnScreen = newTilesOnScreen;

            _lastCameraPos = _pixelPerfectCamera.transform.position;
        }

        _endTurnButton.interactable = localPlayerTurn && GameConfig.modalDialog == 0;

        //If we're previewing unit movement and we mouse over an attack,
        //show the relevant movement we would make to execute this attack.
        if(Tile.mouseoverTile != null) {
            Pathfind.Path path = FindPathToAttack(Tile.mouseoverTile);
            if(path == null || path.destTile.unit != null) {
                Tile.previewMoveTile = null;
            } else {
                Tile.previewMoveTile = path.destTile;

                var attackPath = path.Clone();
                attackPath.steps.Add(Tile.mouseoverTile.loc);
                ShowMapArrow(attackPath);

                if(_attackWarnings == null) {
                    CalculateWhichEnemiesCanReach(_unitMoving, path.destTile);
                }
            }
        } else {
            Tile.previewMoveTile = null;
        }

        if(_prevRevealEntireMap != revealEntireMap) {
            _prevRevealEntireMap = revealEntireMap;
            FullVisionRecalculation();
        }


        Tile.UpdateMouseover();

        if(Input.GetKeyDown(KeyCode.Escape)) {
            Debug.Log("Escape pressed");
            if(GameConfig.modalDialog == 0 && (unitMoving != null || _unitPaths != null)) {
                ClearHighlights();
            }
            else if(GameConfig.modalDialog == 0 || _escapeMenu.gameObject.activeSelf) {
                _escapeMenu.gameObject.SetActive(!_escapeMenu.gameObject.activeSelf);
            }
        }

        if(Input.GetKeyDown(KeyCode.P)) { // && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {

            SaveGame();
        }

        if(GetButtonDown("Undo")) {
            RefreshFromSnapshot();
        }

        if(GetButtonDown("Toggle Underworld") && underworldUnlocked) {
            ToggleUnderworld();
        }

        //Handle middle-click drag.
        if(GetButtonDown("Map Drag") && Tile.mouseoverTile != null && GameConfig.modalDialog == 0) {
            //beginning a middle-click drag.
            _middleClickDrag = true;
            _middleClickDragAnchor = Input.mousePosition;
            _middleClickDragCameraPos = _pixelPerfectCamera.transform.position;
        } else if(_middleClickDrag) {
            if(GetButton("Map Drag") == false) {
                _middleClickDrag = false;
            } else {
                Vector3 delta = Input.mousePosition - _middleClickDragAnchor;
                delta.z = 0f;
                _pixelPerfectCamera.transform.position = _middleClickDragCameraPos - delta*(_zoom/72f);
            }
        }

        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(zoomDelta != 0f) {
            //zoom -= zoomDelta;
        }

        if(teams.Count == 0) {
            return;
        }

        if(GameConfig.modalDialog == 0) {
            if(Tile.mouseoverTile != null && Tile.mouseoverTile.unit != null && Tile.mouseoverTile.fogged == false) {
                unitDisplayed = Tile.mouseoverTile.unit;
            } else if(unitMoving != null) {
                unitDisplayed = unitMoving;
            } else if(lastUnitClicked != null) {
                unitDisplayed = lastUnitClicked;
            } else {
                unitDisplayed = null;
            }

            if(Tile.mouseoverTile == null || Tile.mouseoverTile.unit == null) {
                unitMouseover = null;
            } else {
                unitMouseover = Tile.mouseoverTile.unit;
            }
        }

        UpdateUnitDisplayedPaths();

        CalculateTeamUpkeeps();

        if((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) && GameConfig.modalDialog == 0) {
            ClearHighlights();
        }

        if(spectating == false) {
            AIThink();
        }
        
        if(!animating && GameConfig.modalDialog == 0 && localPlayerTurn && GetButtonDown("Recruit") && Tile.mouseoverTile != null) {
            StartRecruit(Tile.mouseoverTile.loc);
        }

        if(localPlayerTurn && GetButtonDown("Rest") && unitMoving != null) {
            unitMoving.SpacePressed();
            ClearHighlights();
        }

        if(localPlayerTurn && GetButtonDown("Next Unit")) {
            NextUnitFocus();
        } else if(GetButtonDown("Center Unit") && _lastUnitClicked != null) {
            ScrollCameraCommand.CameraLookAt(_lastUnitClicked.loc);
        }


        _goldText.text = string.Format("{0}<color=#AAAAFF>+{1}</color>", playerTeamInfo.gold, playerTeamInfo.income);
        UnitStatusPanel.SetTooltip(_goldText, string.Format("{0} gold pieces, {1} gold income", playerTeamInfo.gold, playerTeamInfo.income));
        _villageText.text = playerTeamInfo.numVillages.ToString();
        UnitStatusPanel.SetTooltip(_villageText, string.Format("You control {0} villages.\nVillages provided you income and you can support one unit for each village you control\nuntil you have 8 units. For more than 8 units\nyou need to control 2 villages for each unit.", playerTeamInfo.numVillages));
        _upkeepText.text = string.Format("{0}/{1}", playerTeamInfo.numUnits, playerTeamInfo.affordUpkeep);

        string bonusUpkeepIncome = "";
        if(playerTeamInfo.numUnits < playerTeamInfo.affordUpkeep) {
            bonusUpkeepIncome = string.Format("You are gaining {0} gold bonus income each turn for having that many less units than your upkeep maximum\n", playerTeamInfo.affordUpkeep - playerTeamInfo.numUnits);
        }

        UnitStatusPanel.SetTooltip(_upkeepText, string.Format("Upkeep: You have {0} units and can support up to {1}.\n{2}Capture more villages to support more units.", playerTeamInfo.numUnits, playerTeamInfo.affordUpkeep, bonusUpkeepIncome));
    }

    static ProfilerMarker s_profileAIThink = new ProfilerMarker("AI Think");
    static ProfilerMarker s_profileAIPlay = new ProfilerMarker("AI.Play");
    static ProfilerMarker s_profilePumpQueue = new ProfilerMarker("Pump Queue");

    public void AIThink()
    {
        using(s_profileAIThink.Auto()) {
            if(!animating && teams[gameState.nturn].team.ai != null) {
                Debug.Log("TEAM: " + gameState.nturn + " / " + teams[gameState.nturn].team.teamName);
                Assert.IsNotNull(aiStates[gameState.nturn]);
                Assert.IsNotNull(aiStates[gameState.nturn].teamInfo);

                System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
                int nplay = 0;

                int nturn = gameState.nturn;
                while(nturn == gameState.nturn && !animating) {
                    //keep running the AI then trying to pump the command queue and then run the AI
                    //again and again until the command queue is empty.
                    using(s_profileAIPlay.Auto()) {
                        teams[gameState.nturn].team.ai.Play(aiStates[gameState.nturn]);
                    }
                    ++nplay;
                    if(commandQueue.empty) {
                        break;
                    }

                    using(s_profilePumpQueue.Auto()) {
                        commandQueue.PumpQueue();
                    }
                    if(commandQueue.empty == false) {
                        break;
                    }
                }

                if(nplay > 0) {
                    long elapsed = timer.ElapsedMilliseconds;
                    _totalAITime += elapsed;
                    Debug.LogFormat("PERF: AI PLAY CALLED {0} times taking {1}ms TOTAL = {2}; FRAMES = {3}; ELAPSED = {4}; queue empty = {5}", nplay, elapsed, _totalAITime, Time.frameCount, _aiTimerStopwatch != null ? _aiTimerStopwatch.ElapsedMilliseconds : -1, commandQueue.empty);
                }
            }
        }
    }

    long _totalAITime = 0;

    static public bool fastforward {
        get {
            return GameController.instance.GetButton("Fast Forward");
        }
    }

    public bool gameOverTimescale = false;

    static public float timeScale {
        get {
            if(GameController.instance != null && GameController.instance.gameOverTimescale) {
                return 0.4f;
            }
            return fastforward ? 4f : 1f;
        }
    }

    void NextUnitFocus()
    {
        var units = unitsOnCurrentTeam;
        if(units.Count == 0) {
            return;
        }

        int index = 0;
        for(int i = 0; i != units.Count; ++i) {
            if(units[i] == _lastUnitMoving) {
                Debug.Log("MATCH UNIT: " + i);
                index = (i+1)%units.Count;
                break;
            }
        }

        Debug.Log("Starting index = " + index);

        for(int i = 0; i != units.Count; ++i) {

            Unit candidate = units[(index+i)%units.Count];
            if(candidate.movementStatus == Orb.Status.Unmoved || candidate.movementStatus == Orb.Status.PartMoved) {
                unitMoving = candidate;
                lastUnitClicked = candidate;

                bool unitSeesUnderworld = candidate.loc.underworld || candidate.tile.underworldGate;

                if(unitSeesUnderworld != _showUnderworld) {
                    ToggleUnderworld();
                }

                QueuePlayerLookAt(candidate.loc);
                break;
            }
        }


    }

    void CalculateTeamUpkeeps()
    {
        foreach(TeamInfo team in teams) {
            team.numUnits = 0;
        }

        foreach(Unit unit in units) {
            if(unit.unitInfo.isTemporal) {
                continue;
            }

            teams[unit.unitInfo.nteam].numUnits++;
        }
    }

    public void OfferQuest(QuestInProgress quest)
    {
        var cmd = Instantiate(_offerQuestCommand, transform);
        cmd.quest = quest;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    public IEnumerator ShowQuestOffer(OfferQuestCommand cmd)
    {
        string preludeText = cmd.quest.quest.GetDetailsPrelude(cmd.quest);
        if(preludeText != null) {
            ShowDialogMessage(cmd.quest.quest.questOfferTitle, preludeText, cmd.quest.clientTeam.rulerType.portrait);
            while(GameConfig.modalDialog > 0) {
                yield return null;
            }

            Loc target = cmd.quest.quest.GetQuestTarget(cmd.quest);
            if(target.valid) {
                var scrollCmd = Instantiate(_scrollCameraProto, transform);
                scrollCmd.info = new ScrollCameraCommandInfo() {
                    target = target,
                };
                scrollCmd.gameObject.SetActive(true);

                while(scrollCmd.finished == false) {
                    yield return null;
                }

                GameObject.Destroy(scrollCmd.gameObject);

                List<Tile> revealTiles = new List<Tile>();
                int revealRadius = cmd.quest.quest.GetQuestReveal(cmd.quest);
                if(revealRadius > 0) {
                    foreach(Loc loc in Tile.GetTilesInRadius(target, revealRadius)) {
                        if(map.LocOnBoard(loc)) {
                            playerTeamInfo.mapRevealed.Add(loc);
                            revealTiles.Add(map.GetTile(loc));
                        }
                    }
                }

                RecalculateVision(revealTiles);

                GameConfig.modalDialog++;
                yield return new WaitForSeconds(1f);
                GameConfig.modalDialog--;
            }
        }

        CreateConversationDialog();

        _conversationDialogInstance.info = new ConversationDialog.Info() {
            title = cmd.quest.quest.questOfferTitle,
            text = cmd.quest.quest.GetDetails(cmd.quest),
            options = new List<string>() { "Accept", "Reject" },
            primarySprite = cmd.quest.clientTeam.rulerType.portrait,
        };

        _conversationDialogInstance.gameObject.SetActive(true);

        yield return new WaitUntil(() => _conversationDialogInstance.gameObject.activeSelf == false);

        if(_conversationDialogInstance.optionChosen == 0) {
            GameController.instance.currentTeamInfo.currentQuests.Add(cmd.quest);
        } else {
            GameController.instance.currentTeamInfo.questRejected = true;
        }

        cmd.finished = true;
    }

    public void QueueBeginTurnEffect(BeginTurnEffectInfo info)
    {
        var cmd = Instantiate(_beginTurnEffectCommand, transform);
        cmd.info = info;
        cmd.Upload();
        commandQueue.QueueCommand(cmd);
    }

    Glowwave.Json.GameSerializer _serializer = new Glowwave.Json.GameSerializer();

    [System.Serializable]
    public class SerializedGame
    {
        public GameState gameState;
        public List<UnitInfo> units = new List<UnitInfo>();
        public SerializedMap mapInfo;
    }

    bool _saveQueued = false;

    public bool hasSaveState {
        get {
            return string.IsNullOrEmpty(_saveState) == false;
        }
    }

    string _saveState {
        get {
            return PlayerPrefs.GetString("save", "");
        }
        set {
            if(value == null) {
                PlayerPrefs.DeleteKey("save");
            } else {
                PlayerPrefs.SetString("save", value);
            }
        }
    }

    void RefreshFromSnapshot(SerializedGame snapshot=null)
    {
        if(snapshot == null) {
            snapshot = Glowwave.Json.FromJson<SerializedGame>(_saveState, _serializer);
            if(snapshot == null) {
                return;
            }
        }

        if(snapshot.gameState.guid != gameState.guid) {
            return;
        }

        map.RefreshFromSnapshot(snapshot.mapInfo);
        gameState = snapshot.gameState;
        
        HashSet<string> unitGuidsKnown = new HashSet<string>(); 
        foreach(UnitInfo unitInfo in snapshot.units) {
            Unit unit = GetUnitByGuid(unitInfo.guid);
            bool newUnit = (unit == null);

            if(newUnit) {
                unit = Instantiate(_unitPrefab, transform);
            }

            unit.RefreshUnitInfo(unitInfo);
            unit.RefreshStatusDisplay();

            unitGuidsKnown.Add(unitInfo.guid);

            if(newUnit) {
                AddUnit(unit);
            }
        }


        List<Unit> unitsRemove = new List<Unit>();
        foreach(Unit unit in units) {
            if(unitGuidsKnown.Contains(unit.unitInfo.guid) == false) {
                unitsRemove.Add(unit);
            }
        }

        foreach(Unit u in unitsRemove) {
            RemoveUnit(u);
        }

        RefreshUnitDisplayed();

        foreach(Tile tile in map.allTilesInUnderworldAndOverworld) {
            if(tile.terrain.valid && tile.terrain.rules.capturable) {
                int nteam = gameState.GetOwnerOfLoc(tile.loc);
                if(nteam >= 0) {
                    tile.flag.gameObject.SetActive(true);
                    tile.flag.team = nteam;
                } else {
                    tile.flag.gameObject.SetActive(false);
                }
            }

            if(tile.loc.underworld && tile.unit != null && underworldDisplayed == false) {
                tile.unit.hiddenInUnderworld = true;
            }
        }

        RecalculateVision();
    }

    public void SaveAndQuit()
    {
        if(localPlayerTurn) {
            SaveGame();
        }

        if(spectating) {
            _gameLogPanel.SendChatMessage(new GameLogEntry() {
                logType = GameLogEntry.LogType.Emote,
                nick = GameConfig.instance.username,
                message = "is no longer spectating",
            });
        }

        _gameHarness.ReturnFromGame();
    }

    static ProfilerMarker s_profileSaveGame = new ProfilerMarker("GameController.SaveGame");
    static ProfilerMarker s_profileToJson = new ProfilerMarker("GameController.SaveGame.ToJson");
    static ProfilerMarker s_profileSerialize = new ProfilerMarker("GameController.SaveGame.Serialize");


    string SaveGame(bool saveData=true)
    {
        if(spectating) {
            return null;
        }

        using(s_profileSaveGame.Auto()) {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            s_profileSerialize.Begin();
            SerializedGame state = new SerializedGame() {
                gameState = gameState,
                mapInfo = map.Serialize(),
            };

            foreach(Unit u in units) {
                state.units.Add(u.unitInfo);
            }
            s_profileSerialize.End();

            s_profileToJson.Begin();
            string result = Glowwave.Json.ToJson(state, _serializer);
            s_profileToJson.End();

            if(saveData) {
                _saveState = result;
            }
            sw.Stop();
            Debug.Log("SERIALIZE: time = " + sw.ElapsedMilliseconds + "ms; len = " + _saveState.Length + " :: " + _saveState);
           // Debug.Log("MAP LEN: " + Glowwave.Json.ToJson(map.Serialize(), _serializer).Length);
            //Debug.Log("GAME LEN: " + Glowwave.Json.ToJson(state.gameState, _serializer).Length);
            //Debug.Log("UNITS LEN: " + Glowwave.Json.ToJson(state.units, _serializer).Length);
            //Debug.Log("TILES REVEALED: " + playerTeamInfo.mapRevealed.Count);

            return result;
        }
    }

    void ClearGame()
    {
        _lastVision.Clear();
        _lastUnfogged.Clear();

        foreach(Unit unit in units) {
            GameObject.Destroy(unit.gameObject);
        }

        units.Clear();

        _villageLocs = null;

        map.Clear();
    }

    public string cloudError = "";
    Dictionary<string, object> _cloudSnapshots = null;
    Dictionary<string, object> _cloudMoves = null;
    string _cloudRound = null;
    object _latestCloudSnapshot = null;
    SerializedGame _cloudSnapshotState = null;
    bool _cloudSnapshotUpdated = false;
    bool _cloudUseSnapshots = true; //if true will use 'snapshot' updates at the start of every round to refresh state.
    int _cloudForceStartRound = -1;
    HashSet<string> _cloudKeysProcessed = new HashSet<string>();
    public bool spectating {
        get {
            return _cloudSnapshotState != null;
        }
    }

    void ReceiveGameSnapshots(object obj)
    {
        if(_cloudUseSnapshots == false && _cloudSnapshots != null) {
            //not using snapshots and we already have the first snapshot so don't bother here.
            return;
        }

        _cloudSnapshots = obj as Dictionary<string, object>;
        string latestRound = null;
        int nlatest = -1;
        foreach(string k in _cloudSnapshots.Keys) {
            if(k.StartsWith("round")) {
                string s = k.Replace("round", "");
                int nround = int.Parse(s);
                if(nround > nlatest && (_cloudForceStartRound < 0 || nround == _cloudForceStartRound)) {
                    nlatest = nround;
                    latestRound = k;
                }
            }
        }

        Assert.IsNotNull(latestRound);
        if(latestRound == null) {
            cloudError = "Could not find any rounds in cloud message";
        }

        _cloudRound = latestRound;

        _cloudSnapshotState = Glowwave.Json.FromObject<SerializedGame>(_cloudSnapshots[latestRound], _serializer);
        _cloudSnapshotUpdated = true;

        Debug.LogFormat("Loaded round: {0}", _cloudRound);
    }

    void ReceiveGameMoves(object obj)
    {
        _cloudMoves = obj as Dictionary<string, object>;

    }

    int _spectateSeqExpected = -1;
    List<NetworkedCommand> _pendingSpectateCommands = new List<NetworkedCommand>();
    float _lastProcessedSpectateCommands = -1f;
    bool _waitingForSpectateCommandsTimedOut {
        get {
            return _pendingSpectateCommands.Count > 0 && (Time.realtimeSinceStartup - _lastProcessedSpectateCommands) > 20f;
        }
    }


    void UpdateSpectate()
    {
        if(_cloudUseSnapshots && animating == false && (_pendingSpectateCommands.Count == 0 || _waitingForSpectateCommandsTimedOut) && _cloudSnapshotUpdated) {
            RefreshFromSnapshot(_cloudSnapshotState);
            _cloudSnapshotUpdated = false;
            _pendingSpectateCommands.Clear();
        }

        if(_cloudUseSnapshots == false) {
            _cloudRound = string.Format("round{0}", gameState.nround);
        }

        if(_cloudMoves != null && _cloudRound != null && _cloudMoves.ContainsKey(_cloudRound) && _cloudSnapshotUpdated == false) {
            Dictionary<int, string> sequences = new Dictionary<int, string>();

            Dictionary<string, object> cloudMovesThisRound = _cloudMoves[_cloudRound] as Dictionary<string, object>;
            for(int i = 0; i != 50; ++i) {
                string turnKey = string.Format("turn{0}", i);
                if(cloudMovesThisRound.ContainsKey(turnKey)) {
                    Dictionary<string, object> turnMoves = cloudMovesThisRound[turnKey] as Dictionary<string, object>;
                    foreach(var p in turnMoves) {
                        string key = string.Format("{0}/{1}/{2}", _cloudRound, turnKey, p.Key);
                        if(_cloudKeysProcessed.Contains(key)) {
                            continue;
                        }

                        _cloudKeysProcessed.Add(key);

                        NetworkedCommand cmd = Glowwave.Json.FromObject<NetworkedCommand>(p.Value, null);
                        _pendingSpectateCommands.Add(cmd);

                        if(sequences.ContainsKey(cmd.seq)) {
                            Debug.LogErrorFormat("Sequence duplicated: {0} in {1} and {2}", cmd.seq, key, sequences[cmd.seq]);
                        } else {
                            sequences.Add(cmd.seq, key);
                        }
                    }                    
                }
            }

            _pendingSpectateCommands.Sort((a, b) => a.seq.CompareTo(b.seq));

            if(_pendingSpectateCommands.Count > 0) {
                Debug.LogFormat("Found {0} new moves for round {1}", _pendingSpectateCommands.Count, _cloudRound);
            }

            int ncommandsExecuted = 0;

            //deserialize and queue the commands for execution.
            foreach(var cmd in _pendingSpectateCommands) {
                if(_spectateSeqExpected != -1 && cmd.seq > _spectateSeqExpected) {
                    Debug.LogWarningFormat("Missed state id! Expected {0} got {1}. Waiting for state id {0}...", _spectateSeqExpected, cmd.seq);
                    break;
                } else if(cmd.seq < _spectateSeqExpected) {
                    Debug.LogWarningFormat("Repeated state id received: {0} received when we were expecting {1}", cmd.seq, _spectateSeqExpected);
                }

                _spectateSeqExpected = cmd.seq+1;

                Debug.LogFormat("Processing seq {0}, expecting {1} next", cmd.seq, _spectateSeqExpected);

                var c = GameCommand.FromNetworkedCommand(cmd);
                commandQueue.QueueCommand(c);

                _lastProcessedSpectateCommands = Time.realtimeSinceStartup;

                ++ncommandsExecuted;
            }

            _pendingSpectateCommands.RemoveRange(0, ncommandsExecuted);
        }
    }

    public IEnumerator ReplayGame(LoadingScreen loadingScreen, int nRoundStart)
    {
        if(_saveState == null) {
            yield break;
        }

        loadingScreen.UpdateProgress("Loading...", 0f);
        yield return null;

        ClearGame();

        SerializedGame state = Glowwave.Json.FromJson<SerializedGame>(_saveState, _serializer);

        loadingScreen.UpdateProgress("Syncing from Cloud...", 0.01f);
        yield return null;

        string gameId = state.gameState.guid;

        _cloudUseSnapshots = false;
        _cloudForceStartRound = nRoundStart;


        DataStore.instance.MonitorData(string.Format("/games/{0}/snapshots", gameId), ReceiveGameSnapshots);
        DataStore.instance.MonitorData(string.Format("/games/{0}/moves", gameId), ReceiveGameMoves);

        yield return new WaitUntil(() => _cloudSnapshotState != null);

        _cloudSnapshotUpdated = false;

        yield return _gameHarness.StartCoroutine(LoadGameFromState(loadingScreen, _cloudSnapshotState));

    }

    public IEnumerator SpectateGame(LoadingScreen loadingScreen, string gameId)
    {
        loadingScreen.UpdateProgress("Syncing from Cloud...", 0.01f);

        DataStore.instance.MonitorData(string.Format("/games/{0}/snapshots", gameId), ReceiveGameSnapshots);
        DataStore.instance.MonitorData(string.Format("/games/{0}/moves", gameId), ReceiveGameMoves);

        yield return new WaitUntil(() => _cloudSnapshotState != null);

        _cloudSnapshotUpdated = false;

        yield return _gameHarness.StartCoroutine(LoadGameFromState(loadingScreen, _cloudSnapshotState));

        _gameLogPanel.SendChatMessage(new GameLogEntry() {
            logType = GameLogEntry.LogType.Emote,
            nick = GameConfig.instance.username,
            message = "is now spectating",
        });
    }

    public IEnumerator LoadGameFromState(LoadingScreen loadingScreen, SerializedGame state)
    {
        loadingScreen.UpdateProgress("Reading Map...", 0.1f);
        yield return null;

        map.Deserialize(state.mapInfo);

        gameState = state.gameState;

        loadingScreen.UpdateProgress("Reading Units...", 0.6f);
        yield return null;

        foreach(UnitInfo unitInfo in state.units) {
            Unit unit = Instantiate(_unitPrefab, transform);
            unit.unitInfo = unitInfo;
            unit.loc = unitInfo.loc;

            AddUnit(unit);

            unit.RefreshStatusDisplay();
        }

        foreach(Tile tile in map.allTilesInUnderworldAndOverworld) {
            if(tile.terrain.valid && tile.terrain.rules.capturable) {
                int nteam = gameState.GetOwnerOfLoc(tile.loc);
                if(nteam >= 0) {
                    tile.flag.gameObject.SetActive(true);
                    tile.flag.team = nteam;
                }
            }

            if(tile.loc.underworld && tile.unit != null) {
                tile.unit.hiddenInUnderworld = true;
            }
        }

        _underworldButton.gameObject.SetActive(underworldUnlocked);

        RecalculateVision();

        loadingScreen.UpdateProgress("Finalizing...", 1.0f);
        yield return null;

        for(int i = 0; i != aiStates.Count; ++i) {
            if(aiStates[i] != null) {
                aiStates[i].teamNumber = i;
            }
        }

        loadingScreen.complete = true;
    }

    public IEnumerator LoadGame(LoadingScreen loadingScreen)
    {
        if(_saveState == null) {
            yield break;
        }

        loadingScreen.UpdateProgress("Loading...", 0f);
        yield return null;

        ClearGame();

        SerializedGame state = Glowwave.Json.FromJson<SerializedGame>(_saveState, _serializer);

        yield return _gameHarness.StartCoroutine(LoadGameFromState(loadingScreen, state));
    }

    [SerializeField]
    public Terrain _peaWater = null, _peaIce, _peaVolcano, _peaMountain, _peaForest, _peaDesert, _peaDry, _peaGrass, _peaSand;
}

namespace heightmap
{
    class Program
    {
        class heightmapgen
        {
            public heightmapgen(int w, int h)
            {
                _map = new int[w, h];
                _landmap = new string[w, h];
                _tempmap = new int[w, h];

                heightavg = 0;
                tempavg = 0;

                percent = (w * h) / 10;

                hills((w * h), "height", true);
                hills((w * h), "temp", true);
                hills((w * h) / 2, "height", false);
                average(h, w);

                HeighttoLand();

            }


            public void hills(int amount, string type, bool direction)
            {
                System.Random rnd = new System.Random();
                for(int i = 0; i < amount; i++) {

                    hill_x = rnd.Next(0, (_map.GetLength(0) - 1));
                    hill_y = rnd.Next(0, (_map.GetLength(1) - 1));
                    radius = rnd.Next((_map.GetLength(1)/8), (_map.GetLength(1) / 2));
                    maxheight = radius;

                    setHeight(_map.GetLength(0), _map.GetLength(1), type, direction);

                }
            }



            public int checkdist(int x, int y)
            {
                double dist = System.Math.Sqrt(System.Math.Pow((x - hill_x), 2) + System.Math.Pow((y - hill_y), 2));
                int a = System.Convert.ToInt32(dist);
                return a;
            }

            public string printmap(string type)
            {
                string msg = "";
                for(int y = 0; y < _landmap.GetLength(1); y++) {
                    for(int x = 0; x < _landmap.GetLength(0); x++) {
                        if(type == "land")
                            msg = msg + _landmap[x, y];
                        if(type == "temp")
                            msg = msg + _tempmap[x, y];
                        if(type == "height")
                            msg = msg + _map[x, y];

                        Terrain terrain = null;
                        switch(_landmap[x, y]) {
                            case "w": terrain = GameController.instance._peaWater; break;
                            case "i":
                                terrain = GameController.instance._peaVolcano; break;
                            case "v":
                                terrain = GameController.instance._peaVolcano; break;
                            case "m":
                                terrain = GameController.instance._peaMountain; break;
                            case "f":
                                terrain = GameController.instance._peaForest; break;
                            case "d":
                                terrain = GameController.instance._peaDesert; break;
                            case "p":
                                terrain = GameController.instance._peaDry; break;
                            case "g":
                                terrain = GameController.instance._peaGrass; break;
                            case "s":
                                terrain = GameController.instance._peaSand; break;                             
                        }

                        if(terrain != null) {
                            Tile tile = GameController.instance.map.GetTile(new Loc(x, y));
                            tile.terrain = new TerrainInfo(terrain);
                            tile.SetDirty();

                        }
                    }

                    msg = msg + "\n";
                }
                return msg;


            }
            public void HeighttoLand()
            {
                for(int i = 0; i < _landmap.GetLength(0); i++)
                    for(int j = 0; j < _landmap.GetLength(1); j++) {



                        if(_map[i, j] <= heightavg * 4)
                            _landmap[i, j] = "w"; //water
                        if(_map[i, j] >= heightavg * 9)
                            _landmap[i, j] = "i"; //ice peak
                        if(_map[i, j] >= heightavg * 9 && _tempmap[i, j] > tempavg * 5)
                            _landmap[i, j] = "v"; //volcano-magma
                        if(_map[i, j] > heightavg  * 7 && _map[i, j] <= heightavg * 9)
                            _landmap[i, j] = "m"; //mountain
                        if(_map[i, j] > heightavg * 5 && _map[i, j] <= heightavg * 7)
                            _landmap[i, j] = "f"; //forest
                        if(_map[i, j] > heightavg * 5 && _map[i, j] <= heightavg * 7 && _tempmap[i, j] >= tempavg * 6)
                            _landmap[i, j] = "d"; //desert
                        if(_map[i, j] > heightavg * 5 && _map[i, j] <= heightavg * 7 && _tempmap[i, j] <= tempavg * 4)
                            _landmap[i, j] = "p"; //dry plains
                        if(_map[i, j] <= heightavg * 5 && _map[i, j] > heightavg * 4 && _tempmap[i, j] > tempavg * 4)
                            _landmap[i, j] = "g"; //grassland
                        if(_map[i, j] > heightavg * 4 && _map[i, j] <= heightavg * 5 && _tempmap[i, j] <= tempavg * 4)
                            _landmap[i, j] = "s"; //sand


                    }
            }

            public void average(int w, int h)
            {
                for(int i = 0; i < _map.GetLength(0); i++) {
                    for(int j = 0; j < _map.GetLength(1); j++) {
                        heightavg = heightavg + _map[i, j];
                        tempavg = tempavg + _tempmap[i, j];
                    }
                }
                heightavg = (heightavg / (w * h))/ 5;
                tempavg = (tempavg / (w * h))/ 5;
            }


            public void setHeight(int w, int h, string type, bool direction)
            {
                for(int i = 0; i < w; i++)
                    for(int j = 0; j < h; j++) {
                        int distance = checkdist(i, j);

                        if(distance <= radius) {
                            if(direction == true) {
                                if(type == "height")
                                    _map[i, j] = _map[i, j] + (maxheight - distance);
                                if(type == "temp")
                                    _tempmap[i, j] = _tempmap[i, j] + (maxheight - distance);
                            }
                            if(direction == false) {
                                if(type == "height")
                                    _map[i, j] = _map[i, j] - (maxheight - distance);
                                if(type == "temp")
                                    _tempmap[i, j] = _tempmap[i, j] - (maxheight - distance);
                            }
                        }

                    }

            }

            private int percent;
            private int maxheight;
            private int radius;
            private int hill_x;
            private int hill_y;
            private string[,] _landmap;
            private int[,] _tempmap;
            private int[,] _map;
            private int heightavg;
            private int tempavg;
        }
        public static void RunPeaMap()
        {
            heightmapgen cool = new heightmapgen(80, 60);
            Debug.LogFormat("LAND:: {0}", cool.printmap("land"));


        }
    }
}
