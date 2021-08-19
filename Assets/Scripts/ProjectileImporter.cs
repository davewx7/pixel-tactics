using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

#if UNITY_EDITOR
public class ProjectileImporter : MonoBehaviour
{
    [MenuItem("Assets/Projectile Import")]
    static void ProjectileImport()
    {

        var assetsDict = new Dictionary<string, ProjectileType>();
        var newAssets = new List<string>();

        Regex reFilenamePattern = new Regex(@"Assets/Textures/Projectiles/(.+?).png");
        Regex reNumberPattern = new Regex(@"^\d+$");

        string[] assets = AssetDatabase.FindAssets(null, new string[] { "Assets/Textures/Projectiles" });
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

            Tile.Direction dir = Tile.Direction.North;

            int frameNumber = 0;

            List<string> tokens = new List<string>(filename.Split('-'));
            string last = tokens[tokens.Count-1];
            if(reNumberPattern.Match(last).Success) {
                frameNumber = int.Parse(last)-1;
                tokens.RemoveAt(tokens.Count-1);
            }

            last = tokens[tokens.Count-1];

            for(int i = 0; i != Tile.DirectionStr.Length; ++i) {
                if(last == Tile.DirectionStr[i]) {
                    dir = (Tile.Direction)i;
                    tokens.RemoveAt(tokens.Count-1);
                    break;
                }
            }

            last = tokens[tokens.Count-1];

            bool isFail = false, isImpact = false;

            if(last == "fail") {
                isFail = true;
                tokens.RemoveAt(tokens.Count-1);
            } else if(last == "impact") {
                isImpact = true;
                tokens.RemoveAt(tokens.Count-1);
            }

            filename = string.Join("-", tokens);


            string assetPath = "Assets/Projectiles/" + filename + ".asset";

            ProjectileType projectileType = null;
            if(assetsDict.TryGetValue(assetPath, out projectileType) == false) {
                projectileType = AssetDatabase.LoadAssetAtPath<ProjectileType>(assetPath);

                if(projectileType == null) {
                    projectileType = ProjectileType.CreateInstance<ProjectileType>();
                    newAssets.Add(assetPath);
                }

                assetsDict[assetPath] = projectileType;
            }

            AnimInfo anim = null;

            if(isFail) {
                if(projectileType.animFail == null) {
                    projectileType.animFail = new AnimInfo() {
                        duration = 1f,
                        cycle = false,
                    };
                }

                anim = projectileType.animFail;
            } else if(isImpact) {
                if(projectileType.animImpact == null) {
                    projectileType.animImpact = new AnimInfo() {
                        duration = 1f,
                        cycle = false,
                    };
                }

                anim = projectileType.animImpact;
            } else {

                if(projectileType.animations == null || projectileType.animations.Length <= (int)dir) {
                    AnimInfo[] animations = new AnimInfo[(int)dir+1];
                    if(projectileType.animations != null) {
                        for(int i = 0; i != projectileType.animations.Length; ++i) {
                            animations[i] = projectileType.animations[i];
                        }
                    }

                    projectileType.animations = animations;
                }

                if(projectileType.animations[(int)dir] == null) {
                    projectileType.animations[(int)dir] = new AnimInfo() {
                        duration = 0.5f,
                        cycle = true,
                    };
                }

                anim = projectileType.animations[(int)dir];
            }


            anim.EnsureSpriteIndexValid(frameNumber);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            anim.sprites[frameNumber] = sprite;
        }

        foreach(string asset in newAssets) {
            ProjectileType projectileType = assetsDict[asset];
            AssetDatabase.CreateAsset(projectileType, asset);
        }

        AssetDatabase.SaveAssets();
    }
}

#endif
