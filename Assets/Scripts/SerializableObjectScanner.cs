using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

public class SerializableObjectScanner : MonoBehaviour
{
    [MenuItem("Assets/Object Scan")]
    static void ObjectScan()
    {
        int nobjects = 0, nchanged = 0;
        string[] assets = AssetDatabase.FindAssets(null, new string[] { "Assets/GameScriptableObjects", "Assets/UnitTypes", "Assets/Terrain" });
        foreach(string asset in assets) {
            string path = AssetDatabase.GUIDToAssetPath(asset);
            GWScriptableObject obj = AssetDatabase.LoadAssetAtPath<GWScriptableObject>(path);
            if(obj != null) {
                bool result = obj.CheckGenerateGuid();
                if(result) {
                    ++nchanged;
                }

                Debug.Log("SCAN: " + obj.name + " UPDATED: " + result);
                ++nobjects;
            }
        }

        AssetInfo.instance.allSerializableObjects.RemoveAll((a) => a == null);
        EditorUtility.SetDirty(AssetInfo.instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SCANNED AND SAVED " + nobjects + " objects, " + nchanged + " changes");
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