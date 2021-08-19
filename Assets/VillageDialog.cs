using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class VillageDialog : MonoBehaviour
{
    [SerializeField]
    Button _confirmButton = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _titleText = null, _buildingText = null, _confirmOrCancelText = null, _createBuildingConfirmationText = null;

    [SerializeField]
    Image _buildingImage = null;

    [SerializeField]
    VillageDialogBuildingPanel _buildingPanelPrefab = null;

    List<VillageDialogBuildingPanel> _buildingPanels = new List<VillageDialogBuildingPanel>();

    [SerializeField]
    RectTransform _buildingPanelsContent = null;

    Unit _unit = null;
    VillageBuilding _startingBuilding = null;
    VillageBuilding _pendingBuilding = null;

    public void ConfirmDialog()
    {
        if(_pendingBuilding != null) {
            GameController.instance.CreateVillageBuildingCommand(_unit, _pendingBuilding);
        }

        CloseDialog();
    }

    public void CloseDialog()
    {
        gameObject.SetActive(false);
    }

    public void Init(Unit unit)
    {
        _unit = unit;
        Loc loc = unit.loc;

        VillageBuilding building = GameController.instance.gameState.GetVillageBuildingBase(loc);
        _startingBuilding = building;
        Tile tile = GameController.instance.map.GetTile(loc);

        string villageName = tile.GetLabelText();

        if(string.IsNullOrEmpty(villageName) == false) {
            _titleText.text = string.Format("Village of {0}", villageName);
        }

        if(building != null) {

            _buildingImage.sprite = building.icon;
            _buildingText.text = string.Format("<b>{0}</b>: {1}", building.description, building.rulesText);
            int nroundsToComplete = GameController.instance.gameState.GetLocOwnerInfo(loc).roundsUntilBuildingComplete;
            if(nroundsToComplete > 0) {
                _buildingText.text += string.Format("\n<b><color=#aaaaaa>Under Construction: {0} {1} to complete", nroundsToComplete, nroundsToComplete == 1 ? "moon" : "moons");
            }
        } else {

        }

        float ypos = 4f;
        foreach(VillageBuilding availableBuilding in unit.teamInfo.villageBuildingsAvailableToBuild) {
            if(availableBuilding == building) {
                continue;
            }

            VillageDialogBuildingPanel panel = Instantiate(_buildingPanelPrefab, _buildingPanelsContent);
            panel.gameObject.SetActive(true);
            panel.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -ypos);
            panel.villageDialog = this;
            panel.Init(availableBuilding, unit);
            ypos += 66f;

            _buildingPanels.Add(panel);
        }

        _buildingPanelsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ypos);
    }

    public void BuildBuilding(VillageBuilding building)
    {
        _confirmOrCancelText.text = "Cancel";
        _confirmButton.gameObject.SetActive(true);
        _createBuildingConfirmationText.text = string.Format("Order the construction of a {0}, which will take {1} {2} to complete and cost {3} gold.", building.description, building.timeCost, building.timeCost == 1 ? "moon" : "moons", building.goldCost);
        if(_startingBuilding != null) {
            _createBuildingConfirmationText.text += string.Format(" The {0} in this village will be demolished and replaced.", _startingBuilding.description);
        }

        _pendingBuilding = building;

        foreach(var panel in _buildingPanels) {
            panel.selected = (_pendingBuilding == panel.villageBuilding);
        }
    }

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }
}
