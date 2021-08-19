using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

#if UNITY_EDITOR
public class TerrainImporter : MonoBehaviour
{
    class ImportInfo
    {
        public string rules = "Flat";

        public string folder;
        public string id;
        public string overlayid;
        public string baseid;
        public string castleFolder = "castle";
        public string castleWalls;

        public string assetname;
        public string name;
        public int zorder;

        public float waterline = 0f;

        bool newAsset = false;
        bool foundBase = false;

        public float yadj = 0f;


        Regex _reMatchBase = null;
        Regex _reMatchAdj = null;
        Regex _reMatchOverlay = null;

        Regex _reMatchCastle = null;


        public Terrain terrain;


        public string assetPath {
            get { return "Assets/Terrain/Imported/" + assetname + ".asset"; }
        }

        public void Init()
        {
            terrain = AssetDatabase.LoadAssetAtPath<Terrain>(assetPath);
            if(terrain != null) {
                EditorUtility.SetDirty(terrain);
            }

            if(terrain == null) {
                terrain = Terrain.CreateInstance<Terrain>();
                terrain.englishName = name;
                newAsset = true;
            }

            terrain.zorder = zorder;
            terrain.yadj = yadj;
            terrain.waterline = waterline;

            terrain.ClearSprites();
            terrain.ClearOverlaySprites();

            if(string.IsNullOrEmpty(id) == false) {
                _reMatchBase = new Regex(string.Format("Assets/Textures/terrain/{0}/{1}[0-9]*.png", folder, id));
                _reMatchAdj =  new Regex(string.Format("Assets/Textures/terrain/{0}/{1}(-n|-ne|-se|-s|-sw|-nw)+.png", folder, id));
            }

            if(string.IsNullOrEmpty(overlayid) == false) {
                _reMatchOverlay = new Regex(string.Format("Assets/Textures/terrain/{0}/{1}[0-9]*.png", folder, overlayid));
            }

            if(string.IsNullOrEmpty(castleWalls) == false) {
                _reMatchCastle = new Regex(string.Format("Assets/Textures/terrain/{0}/{1}-(concave|convex)-(bl|br|l|r|tl|tr).png", castleFolder, castleWalls));
            }

            terrain.rules = AssetDatabase.LoadAssetAtPath<TerrainRules>("Assets/GameScriptableObjects/TerrainRules/" + rules + ".asset");
            if(terrain.rules == null) {
                Debug.LogError("Unknown terrain rules: " + rules);
            }
        }

        public bool MatchBase(string path)
        {
            return _reMatchBase != null && _reMatchBase.IsMatch(path);
        }

        public bool MatchAdj(string path)
        {
            return _reMatchAdj != null && _reMatchAdj.IsMatch(path);
        }

        public bool MatchOverlay(string path)
        {
            return _reMatchOverlay != null && _reMatchOverlay.IsMatch(path);
        }

        public bool ConsumeAsset(string path)
        {
            if(_reMatchCastle != null && _reMatchCastle.IsMatch(path)) {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                terrain.EnsureCastleWallsInit();

                int index = 0;

                var match = _reMatchCastle.Match(path);
                string convexConcave = match.Groups[1].Value;
                string dir = match.Groups[2].Value;

                if(convexConcave == "convex") {
                    index += 6;
                }

                if(dir == "bl") {
                    index += 0;
                } else if(dir == "br") {
                    index += 1;
                } else if(dir == "l") {
                    index += 2;
                } else if(dir == "r") {
                    index += 3;
                } else if(dir == "tl") {
                    index += 4;
                } else if(dir == "tr") {
                    index += 5;
                }

                Debug.Log("PATH: " + path + " convex = " + convexConcave + " dir = " + dir + " index = " + index);

                terrain.castleWalls[index] = sprite;

                return true;
            } else if(MatchOverlay(path)) {
                foundBase = true;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                terrain.AddOverlaySprite(sprite);
                Debug.Log("MATCH OVERLAY: " + path);

                return true;
            } else if(MatchBase(path)) {
                foundBase = true;

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                terrain.AddSprite(sprite);

                Debug.Log("MATCH BASE: " + path);
                return true;
            } else if(MatchAdj(path)) {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                var match = _reMatchAdj.Match(path);

                int count = match.Groups[1].Captures.Count;
                string dir = match.Groups[1].Captures[0].Value;

                int ndir = -1;
                if(dir == "-n") {
                    ndir = 0;
                } else if(dir == "-ne") {
                    ndir = 1;
                } else if(dir == "-se") {
                    ndir = 2;
                } else if(dir == "-s") {
                    ndir = 3;
                } else if(dir == "-sw") {
                    ndir = 4;
                } else if(dir == "-nw") {
                    ndir = 5;
                }

                if(ndir != -1) {
                    Debug.Log("MATCH ADJ: " + path + " -> dir = " + ndir + " count = " + count);
                    terrain.SetAdjacent(ndir, count, sprite);
                }
                
                return true;
            }

            return false;
        }

        public void CreateAsset()
        {
            if(foundBase == false) {
                Debug.Log("Didn't find sprites for " + id);
            }

            if(newAsset) {
                AssetDatabase.CreateAsset(terrain, assetPath);
            }
        }
    }

    [MenuItem("Assets/Terrain Import")]
    static void TerrainImport()
    {
        ImportInfo[] imports = new ImportInfo[] {

            new ImportInfo() {
                folder = "flat",
                id = "dirt",
                assetname = "dirt",
                name = "Dirt",
                zorder = 10,
            },

            new ImportInfo() {
                folder = "flat",
                id = "dirt-dark",
                assetname = "dirt-dark",
                name = "Dirt",
                zorder = 10,
            },

            new ImportInfo() {
                folder = "flat",
                id = "desert-road",
                assetname = "desert-road",
                name = "Road",
                zorder = 30,
            },

            new ImportInfo() {
                folder = "flat",
                id = "road",
                assetname = "road",
                name = "Road",
                zorder = 5,
            },

            new ImportInfo() {
                folder = "flat",
                id = "road-clean",
                assetname = "road-clean",
                name = "Road",
                zorder = 4,
            },

            new ImportInfo() {
                folder = "flat",
                id = "stone-path",
                assetname = "road-stone",
                name = "Road",
                zorder = 3,
            },

            new ImportInfo() {
                folder = "grass",
                id = "green",
                assetname = "grassland",
                name = "Grassland",
                zorder = 102,
            },
            new ImportInfo() {
                folder = "grass",
                id = "dry",
                assetname = "grassland-dry",
                name = "Grassland",
                zorder = 101,
            },
            new ImportInfo() {
                folder = "grass",
                id = "semi-dry",
                assetname = "grassland-semi-dry",
                name = "Grassland",
                zorder = 100,
            },
            new ImportInfo() {
                folder = "grass",
                id = "leaf-litter",
                assetname = "grassland-leaves",
                name = "Grassland",
                zorder = 103,
            },
            new ImportInfo() {
                folder = "frozen",
                id = "ice",
                assetname = "ice",
                name = "Ice",
                zorder = 160,
            },
            new ImportInfo() {
                folder = "frozen",
                id = "snow",
                assetname = "snow",
                name = "Snow",
                zorder = 200,
            },


            new ImportInfo() {
                folder = "hills",
                id = "desert",
                assetname = "hills-desert",
                name = "Dunes",
                zorder = 20,
                rules = "Hills",
            },
            new ImportInfo() {
                folder = "hills",
                id = "dry",
                assetname = "hills-dry",
                name = "Hills",
                zorder = 55,
                rules = "Hills",
            },
            new ImportInfo() {
                folder = "hills",
                id = "regular",
                assetname = "hills-regular",
                name = "Hills",
                zorder = 110,
                rules = "Hills",
            },
            new ImportInfo() {
                folder = "hills",
                id = "snow",
                assetname = "hills-snow",
                name = "Hills",
                zorder = 250,
                rules = "Hills",
            },

            new ImportInfo() {
                folder = "sand",
                id = "beach",
                assetname = "beach",
                name = "Beach",
                zorder = 20,
                rules = "Dune",
            },
            new ImportInfo() {
                folder = "sand",
                id = "desert",
                assetname = "desert",
                name = "Desert",
                zorder = 20,
                rules = "Dune",
            },

            new ImportInfo() {
                folder = "water",
                id = "coast-tropical-A",
                assetname = "coastal-water",
                name = "Shallow Water",
                zorder = 0,
                rules = "Shallow",
                waterline = 0.3f,
            },

            new ImportInfo() {
                folder = "water",
                id = "ocean-A",
                assetname = "ocean",
                name = "Ocean",
                zorder = -10,
                rules = "Deep",
                waterline = 0.5f,
            },

            //Swamp
            new ImportInfo() {
                folder = "swamp",
                id = "water",
                overlayid = "reed",
                assetname = "swamp",
                name = "Swamp",
                zorder = 80,
                rules = "Swamp",
            },

            //Forest
            new ImportInfo() {
                folder = "forest",
                overlayid = "deciduous-fall",
                assetname = "forest-deciduous-fall",
                name = "Forest",
                zorder = 100,
                baseid = "grassland",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "deciduous-summer",
                assetname = "forest-deciduous-summer",
                name = "Forest",
                zorder = 100,
                baseid = "grassland",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "deciduous-winter-snow",
                assetname = "forest-deciduous-winter",
                name = "Forest",
                zorder = 100,
                baseid = "grassland",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "deciduous-winter-snow",
                assetname = "forest-deciduous-winter-snow",
                name = "Forest",
                zorder = 100,
                baseid = "snow",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "mixed-summer",
                assetname = "forest-mixed-summer",
                name = "Forest",
                zorder = 100,
                baseid = "grassland",
                rules = "Forest",
            },


            new ImportInfo() {
                folder = "forest",
                overlayid = "mixed-winter",
                assetname = "forest-mixed-winter",
                name = "Forest",
                zorder = 100,
                baseid = "grassland",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "mixed-winter-snow",
                assetname = "forest-mixed-winter-snow",
                name = "Forest",
                zorder = 100,
                baseid = "snow",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "pine",
                assetname = "forest-pine",
                name = "Forest",
                zorder = 100,
                baseid = "grassland",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "snow-forest",
                assetname = "forest-pine-snow",
                name = "Forest",
                zorder = 100,
                baseid = "snow",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "human-city-snow",
                assetname = "village-human-city-snow",
                name = "Village",
                zorder = 100,
                baseid = "snow",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "human-city",
                assetname = "village-human-city",
                name = "Village",
                zorder = 100,
                baseid = "grassland",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "human-snow",
                assetname = "village-human-snow",
                name = "Village",
                zorder = 100,
                baseid = "snow",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "human",
                assetname = "village-human",
                name = "Village",
                zorder = 100,
                baseid = "grassland",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "human-snow-hills",
                assetname = "village-human-snow-hills",
                name = "Village",
                zorder = 110,
                baseid = "hills-snow",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "human-hills",
                assetname = "village-human-hills",
                name = "Village",
                zorder = 110,
                baseid = "hills-regular",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "hut-snow",
                assetname = "village-hut-snow",
                name = "Village",
                zorder = 100,
                baseid = "snow",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "hut",
                assetname = "village-hut",
                name = "Village",
                zorder = 100,
                baseid = "grassland",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "elven-snow",
                assetname = "village-elven-snow",
                name = "Village",
                zorder = 100,
                baseid = "snow",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "elven",
                assetname = "village-elven",
                name = "Village",
                zorder = 100,
                baseid = "grassland",
                rules = "Village",
            },


            new ImportInfo() {
                folder = "village",
                overlayid = "dwarven",
                assetname = "village-dwarven",
                name = "Village",
                zorder = 110,
                baseid = "hills-regular",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "orc-snow",
                assetname = "village-orc-snow",
                name = "Village",
                zorder = 110,
                baseid = "hills-snow",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "orc",
                assetname = "village-orc",
                name = "Village",
                zorder = 110,
                baseid = "hills-regular",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "village",
                overlayid = "coast",
                assetname = "village-merfolk",
                name = "Village",
                zorder = 0,
                baseid = "coastal-water",
                rules = "Village",
            },

            new ImportInfo() {
                folder = "flat",
                id = "road-clean",
                assetname = "castle-human",
                castleWalls = "castle",
                name = "Castle",
                zorder = 2000,
                rules = "Castle",
            },

            new ImportInfo() {
                folder = "flat",
                id = "road-clean",
                assetname = "keep-human",
                castleWalls = "keep",
                name = "Keep",
                zorder = 2100,
                yadj = 0.4f,
                rules = "Keep",
            },

            new ImportInfo() {
                folder = "flat",
                id = "dirt",
                assetname = "castle-encampment",
                castleWalls = "encampment/regular",
                name = "Castle",
                zorder = 2000,
                rules = "Castle",
            },

            new ImportInfo() {
                folder = "flat",
                id = "dirt",
                assetname = "keep-encampment",
                castleWalls = "encampment/tall-keep",
                name = "Keep",
                zorder = 2100,
                rules = "Keep",
                yadj = 0.4f,
            },

            new ImportInfo() {
                folder = "castle",
                id = "dwarven-castle-floor",
                assetname = "castle-dwarven",
                castleWalls = "dwarven-castle",
                name = "Castle",
                zorder = 2000,
                rules = "Castle",
            },

            new ImportInfo() {
                folder = "castle",
                id = "dwarven-keep-floor",
                assetname = "keep-dwarven",
                castleWalls = "dwarven-castle",
                overlayid = "dwarven-keep",
                name = "Keep",
                zorder = 2100,
                yadj = 0.4f,
                rules = "Keep",
            },


            new ImportInfo() {
                folder = "castle/elven",
                id = "grounds",
                assetname = "castle-elvish",
                castleWalls = "elven/castle",
                name = "Castle",
                zorder = 2000,
                rules = "Castle",
            },

            new ImportInfo() {
                folder = "castle/elven",
                id = "keep",
                assetname = "keep-elvish",
                castleWalls = "elven/keep",
                name = "Keep",
                zorder = 2100,
                yadj = 0.5f,
                rules = "Keep",
            },


            new ImportInfo() {
                folder = "castle/orcish",
                id = "keep",
                assetname = "castle-orcish",
                castleWalls = "orcish/fort",
                name = "Castle",
                zorder = 2000,
                rules = "Castle",
            },

            new ImportInfo() {
                folder = "castle/orcish",
                id = "keep",
                assetname = "keep-orcish",
                castleWalls = "orcish/keep",
                name = "Keep",
                zorder = 2100,
                yadj = 0.5f,
                rules = "Keep",
            },


            new ImportInfo() {
                folder = "castle/aquatic-castle",
                id = "cobbles",
                assetname = "castle-aquatic",
                castleWalls = "aquatic-castle/castle",
                name = "Castle",
                zorder = 2000,
                rules = "Castle",
            },

            new ImportInfo() {
                folder = "castle/aquatic-castle",
                id = "cobbles",
                assetname = "keep-aquatic",
                castleWalls = "aquatic-castle/keep",
                name = "Keep",
                zorder = 2100,
                yadj = 0.4f,
                rules = "Keep",
            },

            //Cave
            new ImportInfo() {
                folder = "cave",
                id = "earthy-floor",
                assetname = "cave-earthy-floor",
                name = "Flat",
                zorder = -1000,
            },

            new ImportInfo() {
                folder = "cave",
                id = "floor",
                assetname = "cave-floor",
                name = "Flat",
                zorder = -1001,
            },

            new ImportInfo() {
                folder = "cave",
                id = "hills",
                assetname = "cave-hills",
                name = "Hills",
                zorder = -999,
                rules = "Hills",
            },

            new ImportInfo() {
                folder = "forest",
                overlayid = "mushrooms",
                assetname = "cave-mushrooms",
                name = "Mushrooms",
                zorder = -500,
                baseid = "cave-floor",
                rules = "Forest",
            },

            new ImportInfo() {
                folder = "void",
                id = "void",
                assetname = "cave-wall",
                //castleFolder = "walls",
                //castleWalls = "wall-mine",
                name = "Wall",
                zorder = -2000,
                rules = "Impassable",
            },



        };

        foreach(var import in imports) {
            import.Init();

        }

        foreach(var import in imports) {
            if(string.IsNullOrEmpty(import.baseid) == false) {
                bool found = false;
                foreach(var baseImport in imports) {
                    if(baseImport.assetname == import.baseid) {
                        found = true;
                        import.terrain.SetBase(baseImport.terrain);
                        break;
                    }
                }

                if(!found) {
                    Debug.LogError("Could not find base terrain: " + import.baseid);
                }
            }
        }

        int nmatch = 0;

        string[] assets = AssetDatabase.FindAssets(null, new string[] { "Assets/Textures/terrain" });
        List<string> processed = new List<string>();
        foreach(string asset in assets) {
            if(processed.Contains(asset)) {
                continue;
            }

            processed.Add(asset);

            string path = AssetDatabase.GUIDToAssetPath(asset);
            if(path.EndsWith(".png") == false) {
                continue;
            }

            foreach(var import in imports) {
                if(import.ConsumeAsset(path)) {
                    ++nmatch;
                }
            }
        }

        Debug.Log("Processed " + assets.Length + " assets with " + nmatch + " matches");

        foreach(var import in imports) {
            import.CreateAsset();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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

#endif