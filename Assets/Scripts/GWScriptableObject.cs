using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GWScriptableObject : ScriptableObject
{
    public static Dictionary<string, GWScriptableObject> allObjects {
        get {
            if(_allObjects.Count != AssetInfo.instance.allSerializableObjects.Count) {
                _allObjects.Clear();
                foreach(var item in AssetInfo.instance.allSerializableObjects) {
                    if(item == null) {
                        continue;
                    }
                    _allObjects[item.guidNameSet + ":" + item.guid] = item;
                }
            }

            return _allObjects;
        }
    }

    static Dictionary<string, GWScriptableObject> _allObjects = new Dictionary<string, GWScriptableObject>();

    //A guid we record for this object when it's enabled.
    [SerializeField]
    public string guid;

    //If an object changes name, generate a new guid for it. this is primarily
    //so if you duplicate a ScriptableObject it gets a new guid.
    [SerializeField]
    [HideInInspector]
    public string guidNameSet;

    public virtual void Init()
    {

    }

    public void OnEnable()
    {
#if UNITY_EDITOR
        if(CheckGenerateGuid()) {
            AssetDatabase.SaveAssets();
        }
#endif

        Init();
    }

#if UNITY_EDITOR
    public bool CheckGenerateGuid()
    {
        bool result = false;
        if(string.IsNullOrEmpty(guid) || name != guidNameSet) {
            Debug.Log("GENERATE NEW GUID FOR " + name + " PREVIOUSLY guid = '" + guid + "' guidNameSet = '" + guidNameSet + "' name = '" + name + "'");
            guid = System.Guid.NewGuid().ToString();
            guidNameSet = name;

            EditorUtility.SetDirty(this);
            result = true;
        }

        if(AssetInfo.instance.RecordScriptableObject(this)) {
            result = true;
        }

        return result;
    }
#endif
}
