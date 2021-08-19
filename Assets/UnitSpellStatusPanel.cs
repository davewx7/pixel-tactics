using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSpellStatusPanel : MonoBehaviour
{
    public UnitSpell spell;
    public bool expended = false;

    [SerializeField]
    UnitStatusPanel _unitStatusPanel = null;

    [SerializeField]
    Button _castButton = null;

    [SerializeField]
    Image _iconImage = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _nameText = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _rulesText = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _expendedText = null;

    public void OnCast()
    {
        spell.StartCasting(_unitStatusPanel.displayedUnit);
    }



    // Start is called before the first frame update
    void Start()
    {
        _iconImage.sprite = spell.icon;
        _nameText.text = spell.description;
        _rulesText.text = spell.summaryRules;

        UnitStatusPanel.SetTooltip(_iconImage, spell.GetTooltip());
        UnitStatusPanel.SetTooltip(_nameText, spell.GetTooltip());
        UnitStatusPanel.SetTooltip(_rulesText, spell.GetTooltip());

        _expendedText.gameObject.SetActive(expended);
        UnitStatusPanel.SetTooltip(_expendedText, "This unit must rest in a village or castle to cast this spell again.");

        CheckCanCast();
    }

    // Update is called once per frame
    void Update()
    {
        CheckCanCast();
    }

    void CheckCanCast()
    {
        if(_castButton != null) {
            bool cannotCast = expended || _unitStatusPanel.displayedUnit.unitInfo.hasAttacked || _unitStatusPanel.displayedUnit.unitInfo.ncontroller != GameController.instance.currentTeamNumber || _unitStatusPanel.displayedUnit.unitInfo.ncontroller != GameController.instance.numPlayerTeam;

            _castButton.gameObject.SetActive(cannotCast == false);
        }

    }
}
