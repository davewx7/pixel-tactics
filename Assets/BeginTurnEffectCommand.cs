using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeginTurnEffectInfo
{
    public string unitGuid;
    public int healing;
    public List<UnitStatus> removeStatus;
}


public class BeginTurnEffectCommand : GameCommand
{
    public BeginTurnEffectInfo info;

    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<BeginTurnEffectInfo>(data); }


    float _ttl = 0f;

    // Start is called before the first frame update
    void Start()
    {
        Unit unit = GameController.instance.GetUnitByGuid(info.unitGuid);
        if(unit == null) {
            finished = true;
            return;
        }
        bool visible = unit.tile.fogged == false;
        unit.unitInfo.damageTaken -= info.healing;

        if(info.removeStatus != null) {
            foreach(UnitStatus status in info.removeStatus) {
                unit.unitInfo.status.Remove(status);
            }

            unit.RefreshStatusDisplay();
        }

        if(visible) {
            if(info.healing < 0) {
                unit.FlashHit(Color.red);
                unit.FloatLabel(string.Format("{0}", -info.healing), Color.red);
            } else {
                unit.FlashHit(Color.green);
                unit.FloatLabel(string.Format("{0}", info.healing), Color.green);
            }

            _ttl = 1f;
        } else {
            finished = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        _ttl -= Time.deltaTime;
        if(_ttl <= 0f) {
            finished = true;
        }
    }
}
