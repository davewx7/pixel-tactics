using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/UnitTag")]
public class UnitTag : GWScriptableObject
{
    public bool isRace = false;

    public string description;
    public string descriptionPlural;

    public List<UnitTrait> traits = new List<UnitTrait>();
    public int numberOfTraits = 0;

    public UnitTrait zombieTrait = null;

    public List<TerrainRules> cannotEnterTerrain = new List<TerrainRules>();
    public List<TerrainRules> advantagedTerrain = new List<TerrainRules>();
    public List<TerrainRules> disadvantagedTerrain = new List<TerrainRules>();

    public string maleNames;
    public string femaleNames;

    List<string> _maleNamesCache = null;
    List<string> _femaleNamesCache = null;

    public bool hasNames {
        get {
            return string.IsNullOrEmpty(maleNames) == false || string.IsNullOrEmpty(femaleNames) == false;
        }
    }

    void GenerateNamesCache()
    {
        if(_maleNamesCache == null || _femaleNamesCache == null || _maleNamesCache.Count == 0 || _femaleNamesCache.Count == 0) {
            _maleNamesCache = new List<string>(maleNames.Split(new char[] { ',' }));
            _femaleNamesCache = new List<string>(femaleNames.Split(new char[] { ',' }));

            if(_maleNamesCache.Count == 0) {
                _maleNamesCache = _femaleNamesCache;
            }

            if(_femaleNamesCache.Count == 0) {
                _femaleNamesCache = _maleNamesCache;
            }

            Debug.Log("MALE NAMES: " + _maleNamesCache.Count);
        }
    }

    public string GenerateName(ConsistentRandom rng, UnitGender gender)
    {
        GenerateNamesCache();
        List<string> cache = gender == UnitGender.Female ? _femaleNamesCache : _maleNamesCache;
        if(cache.Count == 0) {
            return "";
        }

        return cache[rng.Range(0, cache.Count)];
    }
}
