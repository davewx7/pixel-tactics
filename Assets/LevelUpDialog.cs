using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpDialog : MonoBehaviour
{
    public Unit targetUnit;

    public UnitInfo chosenUnitInfo = null;

    public List<UnitInfo> units = new List<UnitInfo>();

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    [SerializeField]
    Button _okayButton = null;

    [SerializeField]
    LevelUpUnitEntry _unitEntry = null;

    List<LevelUpUnitEntry> _unitEntries;

    public void SelectUnitEntry(LevelUpUnitEntry entry)
    {
        foreach(var e in _unitEntries) {
            e.highlight = (e == entry);
        }

        chosenUnitInfo = entry.unitInfo;
        _unitStatusPanel.Init(entry.unitInfo);
    }

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    public static List<UnitInfo> GetLevelUpOptions(UnitInfo unitInfo)
    {
        List<UnitInfo> result = new List<UnitInfo>();

        foreach(var option in unitInfo.unitType.levelsInto) {
            result.Add(unitInfo.RefreshFromUnitType(option, resetSpells: true));
        }

        if(result.Count == 0) {
            var amlaUnit = unitInfo.RefreshFromUnitType(unitInfo.unitType, resetSpells: true);
            amlaUnit.amla++;
            result.Add(amlaUnit);
        }

        return result;
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach(var option in GetLevelUpOptions(targetUnit.unitInfo)) {
            units.Add(option);
        }

        if(_unitEntries != null) {
            foreach(var item in _unitEntries) {
                GameObject.Destroy(item.gameObject);
            }
        }

        _unitEntries = new List<LevelUpUnitEntry>();

        int nindex = 0;
        foreach(UnitInfo unitInfo in units) {
            LevelUpUnitEntry entry = Instantiate(_unitEntry, _unitEntry.transform.parent);
            entry.unitInfo = unitInfo;
            entry.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -70f*nindex);
            entry.gameObject.SetActive(true);

            _unitEntries.Add(entry);

            ++nindex;
        }

        SelectUnitEntry(_unitEntries[0]);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
