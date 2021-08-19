using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/VillageInfo")]
public class VillageInfo : GWScriptableObject
{
    public string villageDescription;

    public Sprite villagerPortrait;
    public List<Boon> boons;

    [System.Serializable]
    public struct NameRule
    {
        public List<TerrainRules> rules;
        public List<string> names;
    }

    public List<NameRule> namePrefixes, namePostfixes;
    public List<string> names;

    List<NameRule> GetMatchingRules(Tile tile, List<NameRule> rules)
    {
        List<NameRule> result = new List<NameRule>();

        foreach(NameRule rule in rules) {
            if(rule.rules.Count == 0 || rule.rules.Contains(tile.terrain.rules)) {
                result.Add(rule);
            }
        }

        return result;
    }

    string GetNameFromRules(ConsistentRandom rng, Tile tile, List<NameRule> rules)
    {
        rules = GetMatchingRules(tile, rules);
        if(rules.Count == 0) {
            return "";
        }

        NameRule rule = rules[rng.Range(0, rules.Count)];
        return rule.names[rng.Range(0, rule.names.Count)];
    }

    public string GenerateName(Tile tile, ConsistentRandom rng)
    {
        if(names.Count == 0) {
            return null;
        }

        string result = "";

        result += GetNameFromRules(rng, tile, namePrefixes);
        result += names[rng.Range(0, names.Count)];
        result += GetNameFromRules(rng, tile, namePostfixes);

        return result;
    }
}
