using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

#if UNITY_EDITOR

public class UnitImporter : MonoBehaviour
{
    class FolderInfo
    {
        public string prefix;
    }

    [MenuItem("Assets/Wesnoth Import")]
    static void WesnothImport()
    {
        var availableIcons = GetAttackIcons();

        Dictionary<string, FolderInfo> folderInfoDict = new Dictionary<string, FolderInfo>() {
            { "drakes", new FolderInfo() { prefix = "drake" } },
            { "dunefolk", new FolderInfo() { prefix = "dunefolk" } },
            { "dwarves", new FolderInfo() { prefix = "dwarf" } },
            { "elves-wood", new FolderInfo() { prefix = "elf" } },
            { "goblins", new FolderInfo() { prefix = "goblin" } },
            { "human-loyalists", new FolderInfo() { prefix = "loyalist" } },
            { "human-magi", new FolderInfo() { prefix = "human" } },
            { "human-outlaws", new FolderInfo() { prefix = "outlaw" } },
            { "human-peasants", new FolderInfo() { prefix = "human" } },
            { "leaders", new FolderInfo() { prefix = "leader" } },
            { "merfolk", new FolderInfo() { prefix = "merfolk" } },
            { "monsters", new FolderInfo() { prefix = "monster" } },
            { "nagas", new FolderInfo() { prefix = "naga" } },
            { "ogres", new FolderInfo() { prefix = "monster" } },
            { "orcs", new FolderInfo() { prefix = "orc" } },
            { "saurians", new FolderInfo() { prefix = "saurian" } },
            { "transport", new FolderInfo() { prefix = "ship" } },
            { "trolls", new FolderInfo() { prefix = "troll" } },
            { "undead", new FolderInfo() { prefix = "undead" } },
            { "undead-necromancers", new FolderInfo() { prefix = "necromancer" } },
            { "undead-skeletal", new FolderInfo() { prefix = "undead" } },
            { "woses", new FolderInfo() { prefix = "wose" } },
        };

        Regex reFolderNamePattern = new Regex(@"Assets/UnitTextures/(.+?)/");
        Regex reFilenamePattern = new Regex(@"Assets/UnitTextures/.+/(.+?).png");

        Regex reFilenameFacingPattern = new Regex(@"(.*)-(n|ne|se|s|sw|nw)(-\d)$");

        string[] assets = AssetDatabase.FindAssets(null, new string[] { "Assets/UnitTextures" });

        var unitNamesByDir = new Dictionary<string, List<string>>();
        foreach(string a in assets) {
            string path = AssetDatabase.GUIDToAssetPath(a);
            if(path.EndsWith(".png") == false) {
                continue;
            }

            Match folderMatch = reFolderNamePattern.Match(path);
            Match filenameMatch = reFilenamePattern.Match(path);
            if(folderMatch.Success == false || filenameMatch.Success == false) {
                Debug.LogError("Failed to match for path: " + path);
                continue;
            }

            if(unitNamesByDir.ContainsKey(folderMatch.Groups[1].Value) == false) {
                unitNamesByDir[folderMatch.Groups[1].Value] = new List<string>();
            }

            string filename = filenameMatch.Groups[1].Value;

            Match facingMatch = reFilenameFacingPattern.Match(filename);
            if(facingMatch.Success) {
                filename = facingMatch.Groups[1].Value;
            }

            List<string> unitNames = unitNamesByDir[folderMatch.Groups[1].Value];
            bool needsDeletes = false;
            bool hasUnitAlready = false;
            foreach(string unitName in unitNames) {
                if(filename.StartsWith(unitName)) {
                    hasUnitAlready = true;
                    break;
                }

                if(unitName.StartsWith(filename)) {
                    needsDeletes = true;
                }
            }

            if(hasUnitAlready) {
                continue;
            }

            if(needsDeletes) {

                List<string> newUnitNames = new List<string>();
                foreach(string unitName in unitNames) {
                    if(unitName.StartsWith(filename)) {
                        continue;
                    }

                    newUnitNames.Add(unitName);
                }

                unitNamesByDir[folderMatch.Groups[1].Value] = newUnitNames;
            }

            unitNamesByDir[folderMatch.Groups[1].Value].Add(filename);
        }

        foreach(var p in unitNamesByDir) {
            foreach(var str in p.Value) {
                Debug.Log("Unit: " + p.Key + " / " + str);
            }
        }

        Regex reGenderPattern = new Regex(@"^\+([a-z]+)(.*)$");
        Regex reFacingPattern = new Regex(@"-(n|ne|se|s|sw|nw)(-.+$|$)");
        Regex reNumPattern = new Regex(@"(\d+)$");

        var unitTypes = new Dictionary<string, UnitType>();
        var unitsCreated = new List<string>();

        foreach(string a in assets) {
            string path = AssetDatabase.GUIDToAssetPath(a);
            if(path.EndsWith(".png") == false) {
                continue;
            }

            Match folderNameMatch = reFolderNamePattern.Match(path);
            string folderName = folderNameMatch.Groups[1].Value;

            if(folderInfoDict.ContainsKey(folderName) == false) {
                Debug.Log("Skipping unknown folder: " + folderName);
                continue;
            }

            FolderInfo folderInfo = folderInfoDict[folderName];

            Match filenameMatch = reFilenamePattern.Match(path);

            if(filenameMatch.Success == false) {
                Debug.LogError("Could not match file path: " + path);
                continue;
            }

            string filename = filenameMatch.Groups[1].Value;

            List<string> unitNames = unitNamesByDir[folderName];
            string matchedUnitName = null;
            foreach(string unitName in unitNames) {
                if(filename.StartsWith(unitName)) {
                    matchedUnitName = unitName;
                    break;
                }
            }

            Assert.IsNotNull(matchedUnitName);

            string strInfo = filename;
            strInfo = strInfo.Remove(0, matchedUnitName.Length);

            string genderInfo = null;
            UnitGender gender = UnitGender.None;

            Match genderMatch = reGenderPattern.Match(strInfo);
            if(genderMatch.Success) {
                genderInfo = genderMatch.Groups[1].Value;
                strInfo = genderMatch.Groups[2].Value;

                if(genderInfo == "female") {
                    gender = UnitGender.Female;
                } else {
                    gender = UnitGender.Male;
                }
            }

            Tile.Direction facing = Tile.Direction.None;
            string facingInfo = null;

            Match facingMatch = reFacingPattern.Match(strInfo);
            if(facingMatch.Success) {
                facingInfo = facingMatch.Groups[1].Value;
                strInfo = Regex.Replace(strInfo, "-(n|ne|se|s|sw|nw)(-|$)", "");

                if(facingInfo == "n") {
                    facing = Tile.Direction.North;
                } else if(facingInfo == "ne") {
                    facing = Tile.Direction.NorthEast;
                } else if(facingInfo == "se") {
                    facing = Tile.Direction.SouthEast;
                } else if(facingInfo == "s") {
                    facing = Tile.Direction.South;
                } else if(facingInfo == "sw") {
                    facing = Tile.Direction.SouthWest;
                } else if(facingInfo == "nw") {
                    facing = Tile.Direction.NorthWest;
                }
            }

            string numInfo = null;

            int frameNum = 0;

            Match numMatch = reNumPattern.Match(strInfo);
            if(numMatch.Success) {
                numInfo = numMatch.Groups[1].Value;
                strInfo = Regex.Replace(strInfo, @"\d+$", "");
                strInfo = Regex.Replace(strInfo, @"-$", "");

                frameNum = int.Parse(numInfo)-1;
            }

            string zombietype = "";
            AnimType animType = AnimType.Stand;

            strInfo = Regex.Replace(strInfo, @"^-", "");
            strInfo = Regex.Replace(strInfo, @"-$", "");

            List<string> tags = new List<string>(strInfo.Split('-'));
            if(tags.Contains("defend")) {
                tags.Remove("defend");
                animType = AnimType.Defend;
            } else if(tags.Contains("die")) {
                tags.Remove("die");
                animType = AnimType.Die;
            } else if(tags.Contains("idle")) {
                tags.Remove("idle");
                animType = AnimType.Idle;
            } else if(tags.Contains("leading") || tags.Contains("magic")) {
                tags.Remove("leading");
                animType = AnimType.Cast;
            }

            if(matchedUnitName == "zombie" && tags.Count > 0 && string.IsNullOrEmpty(tags[0]) == false && tags[0] != "attack") {
                zombietype = tags[0];
                tags.RemoveAt(0);
            }

            strInfo = string.Join("-", tags);

            if(animType == AnimType.Stand && strInfo != "") {
                animType = AnimType.Other;
            }

            string fullUnitName = folderInfo.prefix + "-" + matchedUnitName;

            if(unitTypes.ContainsKey(fullUnitName) == false) {
                string assetPath = "Assets/UnitTypes/" + fullUnitName + ".asset";
                
                bool brandNew = false;
                UnitType newUnitType = AssetDatabase.LoadAssetAtPath<UnitType>(assetPath);
                if(newUnitType != null) {
                    EditorUtility.SetDirty(newUnitType);
                    DoLevelUps(newUnitType);
                }

                if(newUnitType == null) {
                    newUnitType = UnitType.CreateInstance<UnitType>();
                    unitsCreated.Add(fullUnitName);
                    brandNew = true;
                }


                newUnitType.raceId = folderInfo.prefix;
                newUnitType.classId = matchedUnitName;

                string strCat = newUnitType.raceId + "-" + newUnitType.classId;
                string[] strTokens = strCat.Split('-');

                if(brandNew) {
                    //Set the unit's description only if brand new.
                    newUnitType.description = null;
                    foreach(string strToken in strTokens) {
                        if(strToken == "human") {
                            continue;
                        }
                        string strTokenUpper = string.Format("{0}{1}", strToken.Substring(0, 1).ToUpper(), strToken.Substring(1));
                        if(newUnitType.description == null) {
                            newUnitType.description = strTokenUpper;
                        } else {
                            newUnitType.description += " " + strTokenUpper;
                        }
                    }
                }

                newUnitType.animInfo.Clear(); //TODO: REMOVE WHEN WE WANT TO MODIFY ANIMS
                //newUnitType.attacks.Clear(); //SAME FOR THIS

                Debug.Log("Set unit type: " + folderInfo.prefix + " " + matchedUnitName + ": " + newUnitType.description);

                unitTypes[fullUnitName] = newUnitType;
            }

            UnitType unitType = unitTypes[fullUnitName];

            AnimMatch matchInfo = new AnimMatch() { gender = gender, direction = facing, animType = animType, tag = strInfo, zombietype = zombietype };

            AnimInfo anim = null;
            foreach(AnimInfo candidate in unitType.animInfo) {
                if(candidate.matchInfo.IsEqual(matchInfo)) {
                    anim = candidate;
                    break;
                }
            }

            if(anim == null) {
                anim = new AnimInfo() {
                    matchInfo = matchInfo,
                    cycle = (animType == AnimType.Stand),
                    reverse = (animType == AnimType.Other || animType == AnimType.Cast),
                };
                if(animType == AnimType.Idle) {
                    anim.duration = 2f;     
                }
                unitType.animInfo.Add(anim);
            }

            anim.EnsureSpriteIndexValid(frameNum);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            anim.sprites[frameNum] = sprite;

            #region ADD_ATTACKS
            /*

            if(animType == AnimType.Other && strInfo != "") {

                bool found = false;
                foreach(AttackInfo attack in unitType.attacks) {
                    if(attack.id == strInfo) {
                        found = true;
                        break;
                    }
                }

                if(found == false) {
                    Debug.Log("Adding attack for " + fullUnitName + " / " + strInfo);

                    Sprite icon = null;
                    int bestScore = 0;
                    string[] attackTags = strInfo.Split('-');

                    //See if we can find an icon for this attack.
                    foreach(AttackIconInfo info in availableIcons) {
                        int score = 0;
                        foreach(string t in attackTags) {
                            if(t == "attack") {
                                continue;
                            }

                            foreach(string t2 in info.tags) {
                                if(t == t2) {
                                    ++score;
                                }
                            }
                        }

                        if(score > bestScore) {
                            bestScore = score;
                            icon = info.icon;
                        }
                    }

                    AttackInfo newAttack = new AttackInfo() {
                        description = strInfo.Replace('-', ' '),
                        id = strInfo,
                        damage = 5,
                        nstrikes = 4,
                        range = AttackInfo.Range.Melee,
                        icon = icon,
                    };

                    unitType.attacks.Add(newAttack);
                }
            }
            */
            #endregion

            //Debug.Log("INFO: " + path + " -> " + string.Format("unit: {0}; gender: {1}; facing: {2}; num: {3}; anim: {4}", matchedUnitName, genderInfo, facingInfo, numInfo, strInfo));

            //s = sprite;
            //Debug.Log("ASSET: " + a + " " + path + " -> " + (sprite != null) + " FOLDER: " + folderName);
            //if(sprite != null) {
            //break;
            //}
        }

        foreach(string newUnitName in unitsCreated) {
            string assetPath = "Assets/UnitTypes/" + newUnitName + ".asset";
            UnitType unitType = unitTypes[newUnitName];
            AssetDatabase.CreateAsset(unitType, assetPath);
        }

        Debug.Log("NUM ASSETS: " + assets.Length);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void DoLevelUps(UnitType unitType)
    {
        foreach(UnitType levelsInto in unitType.levelsInto) {
            if(unitType.evasion > 0 && levelsInto.evasion == 0) {
                levelsInto.evasion = unitType.evasion;
            }

            if(unitType.armor > 0 && levelsInto.armor == 0) {
                levelsInto.armor = unitType.armor;
            }

            if(levelsInto.unitInfo.level == 1) {
                levelsInto.unitInfo.level = unitType.unitInfo.level+1;
            }

            if(levelsInto.portrait == null) {
                levelsInto.portrait = unitType.portrait;
            }

            if(levelsInto.portraitFemale == null) {
                levelsInto.portraitFemale = unitType.portraitFemale;
            }

            if(levelsInto.tags.Count == 0) {
                foreach(var tag in unitType.tags) {
                    levelsInto.tags.Add(tag);
                }
            }

            if(levelsInto.abilities.Count == 0) {
                foreach(var tag in unitType.abilities) {
                    levelsInto.abilities.Add(tag);
                }
            }

            if(levelsInto.attacks.Count == 0) {
                Debug.Log("duplicating attacks from " + unitType.classDescription + " to " + levelsInto.classDescription);
                foreach(AttackInfo attack in unitType.attacks) {
                    levelsInto.attacks.Add(JsonUtility.FromJson<AttackInfo>(JsonUtility.ToJson(attack)));
                }

            }

            EditorUtility.SetDirty(levelsInto);
            DoLevelUps(levelsInto);
        }

    }

    //functionality to get a list of available attack icons so we can assign them to attacks.
    struct AttackIconInfo
    {
        public string[] tags;
        public Sprite icon;
    }

    static List<AttackIconInfo> GetAttackIcons()
    {
        var results = new List<AttackIconInfo>();

        Regex reFilenamePattern = new Regex(@"Assets/UITextures/Icons/attacks/(.+?).png");

        string[] assets = AssetDatabase.FindAssets(null, new string[] { "Assets/UITextures/Icons/attacks" });
        foreach(string a in assets) {
            string path = AssetDatabase.GUIDToAssetPath(a);
            if(path.EndsWith(".png") == false) {
                continue;
            }

            Match filenameMatch = reFilenamePattern.Match(path);
            if(filenameMatch.Success == false) {
                Debug.LogError("Failed to match for path: " + path);
                continue;
            }


            string filename = filenameMatch.Groups[1].Value;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if(sprite != null) {
                string[] tags = filename.Split('-');
                results.Add(new AttackIconInfo() { tags = tags, icon = sprite });
            }
        }

        Debug.Log("Found " + results.Count + " attacks");

        return results;
    }
}

#endif