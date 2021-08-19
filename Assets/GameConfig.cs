using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public static GameConfig instance {
        get {
            if(_instance == null) {
                GameObject gameObj = GameObject.FindGameObjectWithTag("GameConfig");
                if(gameObj != null) {
                    _instance = gameObj.GetComponent<GameConfig>();
                }
            }

            return _instance;
        }
    }

    static GameConfig _instance = null;

    public string gameVersion = "0.1";

    public string PlayerGuid {
        get {
            string result = PlayerPrefs.GetString("PlayerGuid", "");
            if(result == "") {
                result = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("PlayerGuid", result);
            }

            return result;
        }
    }

    public List<SpeechPrompt> globalSpeechPrompts = new List<SpeechPrompt>();

    public int[] renownLevelThresholds = { 100, 300 };

    public UnitMod amlaMod;

    public Loot deadBodyLoot;

    public DiplomacyRegistry defaultDiplomacyRegistry;

    public SpecialEffectsConfig specialEffects;

    public AssetInfo assetInfo;

    public Material iconHighlightMaterial;

    public Material inventorySlotMaterialCursed;
    public Material inventorySlotMaterialCursedHighlight;

    public Material[] inventorySlotMaterialByTier;
    public Material[] inventorySlotMaterialByTierHighlight;

    public Material GetMaterialForInventorySlot(int itemTier, bool highlight=false)
    {
        if(itemTier == -1) {
            return highlight ? inventorySlotMaterialCursedHighlight : inventorySlotMaterialCursed;
        }

        Material[] array = highlight ? inventorySlotMaterialByTierHighlight : inventorySlotMaterialByTier;
        itemTier--;
        if(itemTier < 0 || itemTier >= array.Length) {
            return array[0];
        }

        return array[itemTier];
    }

    [System.Serializable]
    public class PlayerLevelUnlock
    {
        public int xpcost = 2000;
        public bool unlockTeam = false;
    }

    public List<PlayerLevelUnlock> playerLevelUnlocks = new List<PlayerLevelUnlock>();
    public PlayerLevelUnlock GetPlayerLevel(int nlevel)
    {
        if(nlevel < playerLevelUnlocks.Count) {
            return playerLevelUnlocks[nlevel];
        }

        return playerLevelUnlocks[playerLevelUnlocks.Count-1];
    }

    public List<Team> playerTeams = new List<Team>();

    public int playerLevel {
        get {
            return PlayerPrefs.GetInt("PlayerLevel", 0);
        }
        set {
            PlayerPrefs.SetInt("PlayerLevel", value);
        }
    }

    public int playerExperience {
        get {
            return PlayerPrefs.GetInt("PlayerXP", 0);
        }
        set {
            PlayerPrefs.SetInt("PlayerXP", value);
        }
    }

    public int teamsUnlocked {
        get {
            if(debugUnlockAllTeams) {
                return playerTeams.Count;
            }

            int nresult = 1;
            int nlevel = playerLevel;
            for(int i = 0; i != playerLevel; ++i) {
                if(i < playerLevelUnlocks.Count && playerLevelUnlocks[i].unlockTeam && nresult < playerTeams.Count) {
                    nresult++;
                }
            }

            return nresult;
        }
        set {
            PlayerPrefs.SetInt("TeamsUnlocked", value);
        }
    }

    public string username {
        get {
            string result = PlayerPrefs.GetString("username");
            if(string.IsNullOrEmpty(result)) {
                result = System.Environment.UserName;
                if(string.IsNullOrEmpty(result)) {
                    result = "Guest";
                }
            }

            return result;
        }
        set {
            PlayerPrefs.SetString("username", value);
        }
    }

    public bool allowObservers {
        get {
            return PlayerPrefs.GetInt("allowObservers", 0) != 0;
        }
        set {
            PlayerPrefs.SetInt("allowObservers", value ? 1 : 0);
        }
    }

    public bool debugUnlockAllTeams = false;

    public UnitTag undeadTag;
    public UnitTrait mountedZombieUnitTrait;

    public UnitType zombieUnitType;

    public Terrain caveWallTerrain = null;

    public CalendarMonth[] months;

    public UnitTag unitTagBeast;

    public EconomyBuilding[] baseEconomyBuildings;

    public Sprite magicIcon;
    public Sprite underConstructionIcon;

    public VillageBuilding villageBuildingHerbalist;

    public InventorySlot inventorySlotConsumables;

    public UnitAbility unitAbilityAlchemist;
    public UnitAbility unitAbilityAquatic;
    public UnitAbility unitAbilityCavalry;
    public UnitAbility unitAbilityEthereal;
    public UnitAbility unitAbilityFlank;
    public UnitAbility unitAbilityFlying;
    public UnitAbility unitAbilityInvisible;
    public UnitAbility unitAbilityRegeneration;
    public UnitAbility unitAbilityShielded;
    public UnitAbility unitAbilityShieldWall;
    public UnitAbility unitAbilitySkirmish;
    public UnitAbility unitAbilityUnhealable;

    public AttackAbility attackAbilityAssassinate;
    public AttackAbility attackAbilityBackstab;
    public AttackAbility attackAbilityBerserk;
    public AttackAbility attackAbilityBludgeon;
    public AttackAbility attackAbilityCharge;
    public AttackAbility attackAbilityFirstStrike;
    public AttackAbility attackAbilityLifesteal;
    public AttackAbility attackAbilityPoison;
    public AttackAbility attackAbilityPolearm;
    public AttackAbility attackAbilityZombify;

    public ProjectileType defaultProjectile;

    public UnitStatus statusTired;
    public UnitStatus statusCharmed;
    public UnitStatus statusEntangled;
    public UnitStatus statusSleep;
    public UnitStatus statusFallingAsleep;

    public UnitStatus statusBlessingOfStrength;
    public UnitStatus statusBlessingOfProtection;

    public UnitStatus statusCurse;


    public UnitStatus statusPoisoned;
    public UnitStatus statusLevitating;

    public UnitStatus statusTemporal;

    public AttackAbility magicAttackDummyAbility = null;

    public Unit unitPrefab;
    public Tile tilePrefab;

    public static int modalDialog = 0;

    private void Awake()
    {
        _instance = this;
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
