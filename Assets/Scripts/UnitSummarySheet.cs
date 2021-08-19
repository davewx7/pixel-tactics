using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

public class UnitSummarySheet : OdinEditorWindow
{
    [MenuItem("Wesnoth/Unit Summary Sheet")]
    private static void OpenWindow()
    {
        var window = GetWindow<UnitSummarySheet>();

        window.Read();
        window.Show();
    }

    [Button]
    void Read()
    {
        List<UnitSummary> summaries = new List<UnitSummary>();
        foreach(UnitType u in UnitType.GetAll()) {
            UnitSummary summary = new UnitSummary() {
                unit = u,
            };

            summary.Read();
            summaries.Add(summary);
        }

        units = new List<UnitSummary>();
        int nitemsLastTime = summaries.Count+1;
        while(summaries.Count < nitemsLastTime && summaries.Count > 0) {
            nitemsLastTime = summaries.Count;

            var s = summaries[0];

            string race = s.unit.raceId;

            UnitSummary header = new UnitSummary() {
                unitName = race.ToUpper()
            };

            units.Add(header);

            for(int level = 0; level <= 10; ++level) {
                bool changes = true;
                while(changes) {
                    changes = false;
                    UnitSummary chosenSummary = null;
                    foreach(UnitSummary summary in summaries) {
                        if(summary.unit.unitInfo.level != level || summary.unit.raceId != race) {
                            continue;
                        }

                        if(chosenSummary == null || summary.unitName.CompareTo(chosenSummary.unitName) < 0) {
                            chosenSummary = summary;
                        }
                    }

                    if(chosenSummary != null) {
                        changes = true;
                        List<UnitType> levelsInto = new List<UnitType>();
                        foreach(var u in chosenSummary.unit.levelsInto) {
                            levelsInto.Add(u);
                        }

                        int numLevelsInto = 0;
                        while(levelsInto.Count > numLevelsInto) {
                            numLevelsInto = levelsInto.Count;

                            foreach(UnitType u in new List<UnitType>(levelsInto)) {
                                foreach(UnitType nextLevel in u.levelsInto) {
                                    if(levelsInto.Contains(nextLevel) == false) {
                                        levelsInto.Add(nextLevel);
                                    }
                                }
                            }
                        }

                        List<UnitSummary> unitTree = new List<UnitSummary>();
                        unitTree.Add(chosenSummary);
                        foreach(UnitSummary summary in summaries) {
                            if(levelsInto.Contains(summary.unit)) {
                                unitTree.Add(summary);
                            }
                        }

                        List<UnitSummary> newSummaries = new List<UnitSummary>();
                        foreach(UnitSummary u in summaries) {
                            if(unitTree.Contains(u) == false) {
                                newSummaries.Add(u);
                            }
                        }

                        summaries = newSummaries;

                        unitTree.Sort((UnitSummary a, UnitSummary b) => a.unit.unitInfo.level.CompareTo(b.unit.unitInfo.level));

                        foreach(UnitSummary u in unitTree) {
                            units.Add(u);
                        }
                    }
                }
            }
        }

    }

    [TableList]
    public List<UnitSummary> units;

    public UnitSummary modify = new UnitSummary();

    [Button]
    public void WriteChanges()
    {
        foreach(UnitSummary u in units) {
            u.Write();
        }
    }

    [Button]
    public void RecalculateSurvivability()
    {
        foreach(UnitSummary u in units) {
            u.Recalculate();
        }
    }

    [Button]
    public void ApplyModification()
    {
        foreach(UnitSummary u in units) {
            u.Add(modify);
        }
        modify = new UnitSummary();
    }
}

public class UnitSummary
{
    [PreviewField(70, ObjectFieldAlignment.Center)]
    public Sprite sprite;
    public string unitName;
    public int level;
    public int experience;

    public int hitpoints;
    public int armor;
    public int resistance;
    public int evasion;

    public int survivability;

    public UnitType unit;

    public void Read()
    {
        if(unit == null) {
            return;
        }

        sprite = unit.avatarImage;
        unitName = unit.description;
        level = unit.unitInfo.level;
        experience = unit.experienceMax;
        hitpoints = unit.hitpointsMax;
        armor = unit.armor;
        resistance = unit.resistance;
        evasion = unit.evasion;

        Recalculate();
    }

    public void Write()
    {
        if(unit == null) {
            return;
        }

        unit.description = unitName;
        unit.unitInfo.level = level;
        unit.experienceMax = experience;
        unit.hitpointsMax = hitpoints;
        unit.armor = armor;
        unit.resistance = resistance;
        unit.evasion = evasion;

        EditorUtility.SetDirty(unit);
    }

    public void Add(UnitSummary other)
    {
        if(unit == null) {
            return;
        }

        level += other.level;
        experience += other.experience;
        hitpoints += other.hitpoints;
        armor += other.armor;
        resistance += other.resistance;
        evasion += other.evasion;
    }

    class SimAttack
    {
        public int damage;
        public int accuracy;
        public int crit;
        public bool magical;

        public float weight;
    }

    static SimAttack[] _simAttacks = new SimAttack[] {
        new SimAttack() { damage = 5, weight = 4f, accuracy = 20, crit = 5 },
        new SimAttack() { damage = 6, weight = 3f, accuracy = 10, crit = 5 },
        new SimAttack() { damage = 9, weight = 2f, accuracy = 0, crit = 5 },
        new SimAttack() { damage = 15, weight = 1f * 0.3f, accuracy = 0, crit = 5 },

        new SimAttack() { damage = 4, weight = 3f * 0.3f, accuracy = 40, magical = true },
    };

    public void Recalculate()
    {
        if(unit == null)
            return;
        survivability = (int)(CalculateSurvivability()*10f);
    }

    float CalculateSurvivability()
    {
        float weights = 0f;
        float sum_effectiveness = 0f;
        for(int i = 0; i != _simAttacks.Length; ++i) {
            var a = _simAttacks[i];
            float effectiveness = AttackEffectiveness(a);
            sum_effectiveness += effectiveness*a.weight;
            weights += a.weight;
        }

        float average_effectiveness = sum_effectiveness/weights;
        if(average_effectiveness <= 0)
            return 999f;
        return 1f/average_effectiveness;
    }

    //what percent of the defender's life this attack will do on average.
    float AttackEffectiveness(SimAttack attack)
    {
        int hit = 100 + attack.accuracy - evasion;
        if(hit > 100)
            hit = 100;
        if(hit < 0)
            hit = 0;

        int damage = attack.damage;
        if(attack.magical)
            damage -= resistance;
        else
            damage -= armor;

        if(damage < 0) {
            damage = 0;
        }

        if(damage > hitpoints) {
            damage = hitpoints;
        }

        int crit_damage = attack.damage*2;
        if(crit_damage > hitpoints) {
            crit_damage = hitpoints;
        }
        float weighted_damage = (damage*(100-attack.crit) + crit_damage*attack.crit)/100f;

        float damage_ev = (weighted_damage*hit)/100f;

        float effectiveness = damage_ev/hitpoints;
        return effectiveness;
    }
}

#endif