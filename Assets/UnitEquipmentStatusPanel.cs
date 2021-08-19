using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitEquipmentStatusPanel : MonoBehaviour
{
    public Equipment equipment;

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    [SerializeField]
    Image _iconImage = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _nameText = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _rulesText = null;



    // Start is called before the first frame update
    void Start()
    {
        _iconImage.sprite = equipment.icon;
        _nameText.text = equipment.description;
        _rulesText.text = equipment.summaryRules;

        string tooltip = equipment.GetToolTip();
        UnitStatusPanel.SetTooltip(_iconImage, tooltip);
        UnitStatusPanel.SetTooltip(_nameText, tooltip);
        UnitStatusPanel.SetTooltip(_rulesText, tooltip);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
