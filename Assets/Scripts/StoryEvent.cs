using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StoryEventInfo
{
    public bool valid = false;

    public int goldReward = 0;
    public int renownReward = 0;
    public string renownDescription = "";

    [System.Serializable]
    public struct TerrainChange
    {
        public Loc loc;
        public TerrainInfo terrain;
    }

    public List<TerrainChange> terrainChanges = null;


    [System.Serializable]
    public struct DialogItem
    {
        public string dialogTitle;
        public string dialogText;
        //public Sprite dialogSprite; //TODO: make this serializable.
    }

    public List<DialogItem> dialog = null;
}