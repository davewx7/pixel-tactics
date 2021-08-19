using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EconomicDevelopmentDialog : MonoBehaviour
{

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    [HideInInspector]
    public bool levelingUp = false;

    [SerializeField]
    Transform _panel = null;

    [SerializeField]
    Transform _buildingGrid = null;

    [SerializeField]
    DevelopmentButton _developmentButtonPrefab = null;

    [SerializeField]
    Transform _buildingPreviewPane = null;

    [SerializeField]
    Transform _buildingPreviewObjects = null;

    [SerializeField]
    Image _buildingPreviewImage = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _buildingPreviewNameText = null, _buildingPreviewCompletionText = null, _buildingPreviewDescription = null;

    [HideInInspector]
    public bool canBuildMouseover = false, canBuildSelected = false;

    [SerializeField]
    Button _closeButton = null;

    [SerializeField]
    Button _confirmButton = null;


    [SerializeField]
    GameObject _promptText = null;

    [SerializeField]
    Image _itemPreviewPrefab = null;

    [SerializeField]
    Image _buildingPreviewPrefab = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _itemHeadingPrefab = null;

    List<Image> _itemPreviews = new List<Image>();
    List<TMPro.TextMeshProUGUI> _itemHeadings = new List<TMPro.TextMeshProUGUI>();

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    TeamInfo _teamInfo {
        get { return GameController.instance.currentTeamInfo; }
    }

    EconomyBuilding _currentBuilding {
        get {
            if(_mouseoverBuilding != null) {
                return _mouseoverBuilding;
            }

            if(_clickedBuilding != null) {
                return _clickedBuilding;
            }

            if(_teamInfo.buildingProject != null) {
                return _teamInfo.buildingProject;
            }

            if(_teamInfo.buildingsCompleted.Count > 0) {
                return _teamInfo.buildingsCompleted[_teamInfo.buildingsCompleted.Count-1];
            }

            return null;
        }
    }

    void ShowBuilding(EconomyBuilding building)
    {
        foreach(Image preview in _itemPreviews) {
            GameObject.Destroy(preview.gameObject);
        }

        foreach(TMPro.TextMeshProUGUI text in _itemHeadings) {
            GameObject.Destroy(text.gameObject);
        }

        _itemPreviews.Clear();
        _itemHeadings.Clear();

        _unitStatusPanel.gameObject.SetActive(false);

        if(building == null) {
            _buildingPreviewPane.gameObject.SetActive(false);
            return;
        }

        _buildingPreviewPane.gameObject.SetActive(true);

        _buildingPreviewImage.sprite = building.icon;
        _buildingPreviewNameText.text = building.buildingName;
        _buildingPreviewDescription.text = building.tooltip;

        if(_teamInfo.buildingsCompleted.Contains(building)) {
            _buildingPreviewCompletionText.text = "Completed";
        } else if(_teamInfo.buildingProject == building) {
            _buildingPreviewCompletionText.text = "Selected";
        } else {
            _buildingPreviewCompletionText.text = canBuildMouseover ? "Available" : "Pre-requisites not met";
        }

        float equip_xpos = 0f, equip_ypos = 60f;

        if(building.unitRecruits.Count > 0) {
            _unitStatusPanel.Init(building.unitRecruits[0]);
            _unitStatusPanel.gameObject.SetActive(true);

            equip_ypos -= 150f;
        }

        if(building.villageBuildingsAvailable.Count > 0) {
            TMPro.TextMeshProUGUI heading = Instantiate(_itemHeadingPrefab, _buildingPreviewObjects);
            heading.text = "Village Buildings";
            heading.rectTransform.anchoredPosition += new Vector2(equip_xpos, equip_ypos);
            heading.gameObject.SetActive(true);
            _itemHeadings.Add(heading);
            equip_ypos -= 20f;
            foreach(VillageBuilding villageBuilding in building.villageBuildingsAvailable) {
                Image preview = Instantiate(_buildingPreviewPrefab, _buildingPreviewObjects);
                preview.sprite = villageBuilding.icon;

                preview.rectTransform.anchoredPosition += new Vector2(equip_xpos, equip_ypos);
                preview.gameObject.SetActive(true);

                string tooltip = string.Format("<color=#ffffff>{0}</color>\n<color=#ffffaa>{1} gold, {2} {3}\n<color=#cccccc>{4}</color>", villageBuilding.description, villageBuilding.goldCost, villageBuilding.timeCost, villageBuilding.timeCost == 1 ? "Moon" : "Moons", villageBuilding.rulesText);
                UnitStatusPanel.SetTooltip(preview, tooltip);

                equip_xpos += 64;

                _itemPreviews.Add(preview);
            }
        }

        if(equip_xpos > 0f) {
            equip_xpos = 0f;
            equip_ypos -= 64f;
        }

        if(building.equipmentInMarket.Count > 0) {
            TMPro.TextMeshProUGUI heading = Instantiate(_itemHeadingPrefab, _buildingPreviewObjects);
            heading.text = "Equipment Available";
            heading.rectTransform.anchoredPosition += new Vector2(equip_xpos, equip_ypos);
            heading.gameObject.SetActive(true);
            _itemHeadings.Add(heading);

            equip_ypos -= 20f;

            foreach(Equipment equip in building.equipmentInMarket) {
                Image preview = Instantiate(_itemPreviewPrefab, _buildingPreviewObjects);
                preview.sprite = equip.icon;
                preview.material = new Material(GameConfig.instance.GetMaterialForInventorySlot(equip.tier, false));
                if(preview.material != null) {
                    preview.material.SetFloat("_hueshift", equip.hueShift);
                }

                preview.rectTransform.anchoredPosition += new Vector2(equip_xpos, equip_ypos);
                preview.gameObject.SetActive(true);

                string tooltip = string.Format("<color=#ffffff>{0}</color>\n<color=#ffffaa>{1} gold\n<color=#cccccc>{2}</color>", equip.description, equip.price, equip.GetToolTip());
                UnitStatusPanel.SetTooltip(preview, tooltip);

                equip_xpos += 62f;
                if(equip_xpos > 62f*3) {
                    equip_xpos = 0f;
                    equip_ypos -= 62f;
                }

                _itemPreviews.Add(preview);
            }
        }

        if(equip_xpos > 0f) {
            equip_xpos = 0f;
            equip_ypos -= 62f;
        }
    }

    EconomyBuilding _mouseoverBuilding = null;
    EconomyBuilding _clickedBuilding = null;

    public EconomyBuilding mouseoverBuilding {
        get {
            return _mouseoverBuilding;
        }
        set {
            _mouseoverBuilding = value;
            ShowBuilding(_currentBuilding);
        }
    }

    public void BuildingClicked()
    {
        _clickedBuilding = _mouseoverBuilding;
        if(_mouseoverBuilding != null) { // && canBuild && _teamInfo.buildingsCompleted.Contains(_mouseoverBuilding) == false) {
            _teamInfo.buildingProject = _mouseoverBuilding;

            foreach(DevelopmentButton button in _buttons) {
                button.Init();
            }
        }

        foreach(DevelopmentButton button in _buttons) {
            button.selected = (_clickedBuilding == button.building);
        }

        ShowBuilding(_currentBuilding);
    }

    List<DevelopmentButton> _buttons = new List<DevelopmentButton>();

    // Start is called before the first frame update
    void Start()
    {
        foreach(EconomyBuilding building in _teamInfo.team.economyBuildings) {
            DevelopmentButton devButton = Instantiate(_developmentButtonPrefab, _buildingGrid);
            devButton.building = building;
            devButton.gameObject.SetActive(true);

            _buttons.Add(devButton);
        }

        ShowBuilding(_currentBuilding);
    }

    // Update is called once per frame
    void Update()
    {
        if(levelingUp == false) {
            _closeButton.gameObject.SetActive(true);
            _promptText.gameObject.SetActive(false);
            _confirmButton.gameObject.SetActive(false);
        } else {
            _closeButton.gameObject.SetActive(false);

            bool eligible = _teamInfo.buildingProject != null && canBuildSelected && _teamInfo.buildingsCompleted.Contains(_mouseoverBuilding) == false;
            _confirmButton.gameObject.SetActive(eligible);
            _promptText.gameObject.SetActive(!eligible);

            float v = 0.5f + 0.5f*Mathf.Sin(Time.time*5f);
            int ncount = 0;
            foreach(DevelopmentButton button in _buttons) {
                if(button.canBeBuilt && _teamInfo.buildingsCompleted.Contains(button.building) == false) {
                    button.highlightColor = v;
                    ++ncount;
                }
            }
        }
    }
}
