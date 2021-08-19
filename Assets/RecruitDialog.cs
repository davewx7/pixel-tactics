using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecruitDialog : MonoBehaviour
{
    public Loc targetLoc;

    public UnitInfo chosenUnitInfo = null;
    public Team recruitingTeam = null;

    public List<UnitInfo> units;

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    [SerializeField]
    Button _okayButton = null;

    [SerializeField]
    RecruitDialogUnitEntry _unitEntry = null;

    List<RecruitDialogUnitEntry> _unitEntries;

    [SerializeField]
    RectTransform _unitScrollArea = null;

    [SerializeField]
    ScrollRect _unitScrollRect = null;

    [SerializeField]
    Transform _warningPanel = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _warningText = null;

    void DisplayWarning(string msg)
    {
        _warningPanel.gameObject.SetActive(true);
        _warningText.text = msg;
    }

    public void SelectUnitEntry(RecruitDialogUnitEntry entry)
    {
        foreach(var e in _unitEntries) {
            e.highlight = (e == entry);
        }

        chosenUnitInfo = entry.unitInfo;
        _unitStatusPanel.Init(entry.unitInfo);

        _okayButton.interactable = entry.optionEnabled;
    }

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(_unitEntries != null) {
            foreach(var item in _unitEntries) {
                GameObject.Destroy(item.gameObject);
            }
        }

        _unitEntries = new List<RecruitDialogUnitEntry>();

        int nindex = 0;
        foreach(UnitInfo unitInfo in units) {
            RecruitDialogUnitEntry entry = Instantiate(_unitEntry, _unitScrollArea.transform);
            entry.unitInfo = unitInfo;
            entry.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -70f*nindex - 2f);
            entry.gameObject.SetActive(true);

            entry.optionEnabled = GameController.instance.currentTeamInfo.CanRecruitUnit(unitInfo.unitType);
            entry.tooExpensive = GameController.instance.currentTeamInfo.CanAffordUnit(unitInfo.unitType) == false;

            _unitEntries.Add(entry);

            ++nindex;
        }

        _unitScrollArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 70f*units.Count);

        SelectUnitEntry(_unitEntries[0]);

        int numUnits = GameController.instance.currentTeamInfo.numUnits;
        int affordUpkeep = GameController.instance.currentTeamInfo.affordUpkeep;
        if(numUnits == affordUpkeep-1) {
            DisplayWarning(string.Format("Warning: You control {0} villages and are supporting {1} units. You can only recruit one more unit. Capture more villages to increase the number of units you can field.", affordUpkeep, numUnits));
        } else if(numUnits >= affordUpkeep) {
            DisplayWarning(string.Format("You control {0} villages and are supporting {1} units. You need to control one village for each unit you field. Capture more villages to deploy more units.", affordUpkeep, numUnits));
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
