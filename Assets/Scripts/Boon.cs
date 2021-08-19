using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A boon granted by a village.
[CreateAssetMenu(menuName = "Wesnoth/Boon/Generic")]
public class Boon : GWScriptableObject
{

    [System.Serializable]
    public class AlternateTeamEntry
    {
        public Team team;
        public Boon boon;
    }

    public List<AlternateTeamEntry> alternatives;

    public Boon GetBoon(Team team)
    {
        foreach(var entry in alternatives) {
            if(entry.team == team) {
                return entry.boon;
            }
        }

        return this;
    }

    public int priority = 1;

    public int embargoAfterOffering = -1;

    public int minimumRound = 0;

    public int minimumEnemiesLevel = 0;

    public bool unitMustBeInjured = false;
    public bool unitMustBeRuler = false;

    public bool allowOnlyInNeutralVillages = false;

    public Boon forcedSecondaryBoon = null;
    public bool noSecondaryBoon = false;

    public VillageBuilding createBuilding = null;

    public virtual bool AllowOptions(Unit unit)
    {
        return true;
    }

    [TextArea(3,3)]
    public string dialogStorylineText;

    public virtual string GetDialogStoryline(Unit unit, int nseed)
    {
        return ModifyStoryText(unit, dialogStorylineText);
    }

    public string declineStoryText;

    public Sprite avatarSprite;

    public string effectText;
    public string storyText;

    public virtual Sprite GetAvatarSprite(Unit unit, int nseed)
    {
        return avatarSprite;
    }

    public virtual string GetEffectText(Unit unit, int nseed)
    {
        return effectText;
    }

    public virtual string GetStoryText(Unit unit, int nseed)
    {
        return storyText;
    }

    public virtual string GetDeclineStoryText(Unit unit)
    {
        return declineStoryText;
    }


    public bool isPrimary {
        get {
            return dialogStorylineText.Length > 0;
        }
    }

    public virtual string GetTooltipText(Unit unit)
    {
        return null;
    }

    public virtual string DeclineSummaryText(Unit unit, Boon secondaryBoon)
    {
        return string.Format("<color=#ffffff>[{0}]</color> <color=#aaaaaa>{1}</color>", secondaryBoon.effectText, GetDeclineStoryText(unit));
    }

    public virtual string GetSummaryText(Unit unit, int nseed)
    {
        return string.Format("<color=#ffffff>[{0}]</color> <color=#aaaaaa>{1}</color>", GetEffectText(unit, nseed), GetStoryText(unit, nseed));
    }

    public bool CanOfferBoon(Unit unit)
    {
        if(GameController.instance.gameState.nround < minimumRound) {
            return false;
        }

        return IsEligible(unit);
    }

    public virtual bool IsEligible(Unit unit)
    {
        if(createBuilding != null && createBuilding.LocationEligible(unit.loc) == false) {
            return false;
        }

        if(unitMustBeInjured && unit.unitInfo.hitpointsRemaining > unit.unitInfo.hitpointsMax*0.6f) {
            return false;
        }

        if(unitMustBeRuler && unit.unitInfo.ruler == false) {
            return false;
        }

        if(minimumEnemiesLevel > 0 && CalculateEnemies(unit) < minimumEnemiesLevel) {
            return false;
        }

        return true;
    }

    public int CalculateEnemies(Unit unit)
    {
        //have to be an enemy within two spaces or at least two enemies within four spaces.
        int points = 0;
        List<Loc> locs = Tile.GetTilesInRadius(unit.loc, 4);
        foreach(Loc loc in locs) {
            Unit enemyUnit = GameController.instance.GetUnitAtLoc(loc);
            if(enemyUnit != null && enemyUnit.unitInfo.level > 0 && enemyUnit.IsEnemy(unit) && enemyUnit.unitInfo.unitType.tags.Contains(GameConfig.instance.unitTagBeast) == false && enemyUnit.WantsContact(unit) == false) {
                if(Tile.DistanceBetween(loc, unit.loc) <= 2) {
                    points += 2;
                } else {
                    ++points;
                }

                if(points >= 2) {
                    break;
                }
            }
        }

        return points;
    }

    // handles the boon being offered.
    public virtual void RecordOffer(int nseed, Unit unit, bool accepted)
    {

    }

    public virtual void Award(AwardBoonInfo info, Unit unit)
    {
        if(createBuilding != null) {
            GameController.instance.gameState.SetVillageBuilding(unit.loc, createBuilding);

            Tile tile = GameController.instance.map.GetTile(unit.loc);
            if(tile != null) {
                tile.FadeInBuildingIcon();
            }
        }
    }

    public string ModifyStoryText(Unit unit, string text)
    {
        Tile t = GameController.instance.map.GetTile(unit.loc);
        string village_name = "this village";
        if(t != null) {
            if(string.IsNullOrEmpty(t.GetLabelText()) == false) {
                village_name = t.GetLabelText();
            }
        }

        Unit playerRuler = unit.teamInfo.GetRuler();
        bool rulerFemale = playerRuler.unitInfo.gender == UnitGender.Female;

        text = text.Replace("village_name", village_name);
        text = text.Replace("ruler_title", rulerFemale ? "Queen" : "King");
        text = text.Replace("ruler_her", rulerFemale ? "her" : "him");


        return text;
    }
}



