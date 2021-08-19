using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class LevelUpCommandInfo
{
    public Loc loc;
    public UnitInfo unitInfo;
}


public class LevelUpCommand : GameCommand
{
    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<LevelUpCommandInfo>(data); }


    public LevelUpCommandInfo info;
     
    IEnumerator Exec()
    {
        Unit targetUnit = GameController.instance.GetUnitAtLoc(info.loc);

        if(targetUnit.tile.fogged == false) {
            targetUnit.PlayLevelUpEffect();

            targetUnit.colorFlash = new Color(2f, 2f, 2f, 0f);
            var tween = DOTween.To(() => targetUnit.colorFlash, x => targetUnit.colorFlash = x, new Color(2f, 2f, 2f, 1f), 1f);

            yield return tween.WaitForCompletion();
        }

        info.unitInfo.guid = targetUnit.unitInfo.guid;
        info.unitInfo.loc = targetUnit.unitInfo.loc;
        info.unitInfo.experience = targetUnit.unitInfo.experience - targetUnit.unitInfo.experienceMax;
        targetUnit.unitInfo = info.unitInfo;
        targetUnit.unitInfo.Exhaust();
        targetUnit.PlayAnimation(AnimType.Stand);
        targetUnit.RefreshLocation();
        targetUnit.unitInfo.resting = true;

        if(targetUnit.tile.fogged == false) {
            DOTween.To(() => targetUnit.colorFlash, x => targetUnit.colorFlash = x, new Color(2f, 2f, 2f, 0f), 1f);
        }

        targetUnit.teamInfo.scoreInfo.RecordLevelUp(info.unitInfo.level);

        GameController.instance.OnLevelUp(targetUnit);

        finished = true;

    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Exec());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
