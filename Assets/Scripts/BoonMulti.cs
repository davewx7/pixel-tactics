using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/Boon/Multi")]
public class BoonMulti : Boon
{
    [SerializeField]
    List<Boon> _boons = null;


    List<Boon> GetEligible(Unit unit)
    {
        List<Boon> result = new List<Boon>();

        foreach(Boon b in _boons) {
            if(b.IsEligible(unit)) {
                result.Add(b);
            }
        }

        return result;
    }

    Boon GetBoon(Unit unit)
    {
        var candidates = GetEligible(unit);
        return candidates[(unit.loc.y*7+unit.loc.x)%candidates.Count];
    }

    public override bool AllowOptions(Unit unit)
    {
        return GetBoon(unit).AllowOptions(unit);
    }

    public override bool IsEligible(Unit unit)
    {
        return GetEligible(unit).Count > 0;
    }

    public override void RecordOffer(int nseed, Unit unit, bool accepted)
    {
        if(_lastChoice != null) {
            GameController.instance.gameState.RecordBoonOffer(_lastChoice);
        }
    }

    Boon _lastChoice = null;

    public override void Award(AwardBoonInfo info, Unit unit)
    {
        var boon = GetBoon(unit);

        _lastChoice = boon;
        boon.Award(info, unit);
    }

    public override string GetDialogStoryline(Unit unit, int nseed)
    {
        return GetBoon(unit).GetDialogStoryline(unit, nseed);
    }

    public override Sprite GetAvatarSprite(Unit unit, int nseed)
    {
        return GetBoon(unit).GetAvatarSprite(unit, nseed);
    }

    public override string GetEffectText(Unit unit, int nseed)
    {
        return GetBoon(unit).GetEffectText(unit, nseed);
    }

    public override string GetStoryText(Unit unit, int nseed)
    {
        return GetBoon(unit).GetStoryText(unit, nseed);
    }

    public override string GetDeclineStoryText(Unit unit)
    {
        return GetBoon(unit).GetDeclineStoryText(unit);
    }
}
