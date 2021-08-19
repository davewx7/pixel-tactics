using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VillageDialogBuildingPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public VillageDialog villageDialog = null;

    [SerializeField]
    Image _buildingIcon = null, _background = null;

    [SerializeField]
    Sprite _normalBackground = null, _highlightBackground = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _buildingDescription = null, _buildingRules = null, _goldCost = null, _timeCost = null;

    VillageBuilding _building = null;

    public VillageBuilding villageBuilding {
        get { return _building; }
    }

    bool _mouseover = false;
    public bool selected = false;
    bool _canClick = true;

    public void Init(VillageBuilding building, Unit unit)
    {
        string errorReason = "";
        bool eligible = unit == null ? true : building.LocationEligible(unit.loc, out errorReason);

        _building = building;

        _buildingIcon.sprite = building.icon;
        _buildingDescription.text = building.description;
        _buildingRules.text = building.rulesText;
        if(eligible == false) {
            _buildingRules.text += string.Format("\n<color=#ff8888>{0}</color>", errorReason);
        }

        _goldCost.text = building.goldCost.ToString();
        _timeCost.text = building.timeCost.ToString();

        if(unit != null && building.goldCost > unit.teamInfo.gold) {
            _goldCost.color = Color.red;
            _background.color = Color.gray;
            _canClick = false;
        } else if(eligible == false) {
            _background.color = Color.gray;
            _canClick = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _mouseover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _mouseover = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(_canClick && villageDialog != null) {
            villageDialog.BuildBuilding(_building);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _background.sprite = ((_mouseover && _canClick) || selected) ? _highlightBackground : _normalBackground;
    }
}
