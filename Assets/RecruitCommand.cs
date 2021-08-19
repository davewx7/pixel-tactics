using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

[System.Serializable]
public class RecruitCommandInfo
{
    public Loc loc;
    public UnitType unitType;
    public int seed;
    public bool payCost = false;
    public string unitGuid = System.Guid.NewGuid().ToString();
    public List<UnitStatus> unitStatus = new List<UnitStatus>();
    public List<UnitTrait> unitTraits = new List<UnitTrait>();

    public string summonerGuid = null;
    public bool isFamiliar = false;
    public Team team = null;

    public bool haveHaste = false;

    public AIUnitAssignment unitAssignment = AIUnitAssignment.None;

    public BoonRecruit.Entry recruitEntry = null;

    public UnitInfo unitOverride = null;
}

public class RecruitCommand : GameCommand
{
    public RecruitCommandInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<RecruitCommandInfo>(data); }

    static ProfilerMarker s_profileSummonUnit = new ProfilerMarker("Recruitcommand.SummonUnit");

    static public Unit SummonUnit(RecruitCommandInfo info, bool dryRun=false)
    {
        using(s_profileSummonUnit.Auto()) {
            TeamInfo teamInfo = GameController.instance.currentTeamInfo;
            if(info.team != null) {
                teamInfo = info.team.teamInfo;
            }

            Unit ruler = teamInfo.GetRuler();

            UnitInfo unitInfo = null;

            if(info.unitOverride != null) {
                unitInfo = info.unitOverride;
            } else {
                unitInfo = info.unitType.createUnit(info.seed, teamInfo);
            }

            unitInfo.nteam = teamInfo.nteam;
            if(info.unitGuid != null) {
                unitInfo.guid = info.unitGuid;
            }

            Unit unit = Instantiate(GameConfig.instance.unitPrefab, GameController.instance.transform);
            unit.unitInfo = unitInfo;
            unit.loc = info.loc;

            if(info.recruitEntry != null) {
                info.recruitEntry.ApplyOverrides(unit.unitInfo);
            }

            if(dryRun) {
                unit.gameObject.SetActive(false);
                return unit;
            }

            if(ruler != null) {
                ruler.PlayCastAnim();
            }


            if(string.IsNullOrEmpty(info.summonerGuid) == false) {
                unit.summoner = GameController.instance.GetUnitByGuid(info.summonerGuid);
                if(info.isFamiliar) {
                    unit.summoner.unitInfo.familiarGuids.Add(unit.unitInfo.guid);
                }
            }

            GameController.instance.AddUnit(unit);
            GameController.instance.OnRecruit(unit);
            unit.SummonAnim();
            //GameController.instance.RecalculateVision();

            if(ruler != null) {
                SpeechPrompt.UnitRecruited(ruler, unit);
            }

            foreach(var s in info.unitStatus) {
                unit.unitInfo.status.Add(s);
            }

            foreach(var s in info.unitTraits) {
                unit.unitInfo.traits.Add(s);
                if(info.unitType == GameConfig.instance.zombieUnitType) {
                    unit.unitInfo.zombietype = s.traitName.ToLower();
                }
            }

            if(info.haveHaste) {
                unit.BeginTurn();
            } else {
                unit.SetSummoningSickness();
            }

            if(info.unitAssignment != AIUnitAssignment.None) {
                GameController.instance.aiStates[teamInfo.nteam].SetUnitOrders(new AIUnitOrders() {
                    unitGuid = unit.unitInfo.guid,
                    assignment = info.unitAssignment,
                });
            }

            return unit;
        }
    }

    public override bool RunImmediately()
    {
        if(info.payCost) {
            GameController.instance.currentTeamInfo.gold -= info.unitType.cost;
        }

        SummonUnit(info);

        return true;
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
