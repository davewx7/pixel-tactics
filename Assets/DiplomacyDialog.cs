using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiplomacyDialog : MonoBehaviour
{
    [SerializeField]
    ScrollRect _scrollRect = null;

    [SerializeField]
    RectTransform _contentTransform = null;

    [SerializeField]
    DiplomacyKingdomPanel _kingdomPanelPrefab = null;

    List<DiplomacyKingdomPanel> _kingdomPanels = new List<DiplomacyKingdomPanel>();

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
        float ypos = 4f;
        foreach(TeamInfo teamInfo in GameController.instance.gameState.teams) {

            if(teamInfo.hasPlayerContact == false || teamInfo.team.barbarian) {
                continue;
            }

            var panel = Instantiate(_kingdomPanelPrefab, _contentTransform);
            panel.GetComponent<RectTransform>().anchoredPosition += new Vector2(4f, -ypos);
            panel.teamInfo = teamInfo;
            panel.gameObject.SetActive(true);

            ypos += 64f;
        }

        _contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ypos + 4f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
