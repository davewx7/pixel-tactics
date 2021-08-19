using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TileLabel : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    [SerializeField]
    BuildingIcon _buildingIcon = null;

    public Tile tile;

    public void SetBuildingIcon(VillageBuilding building, bool fadeIn=false, bool buildingCompleted=true)
    {
        if(building == null) {
            _buildingIcon.gameObject.SetActive(false);
        } else {
            _buildingIcon.loc = tile.loc;
            _buildingIcon.SetBuilding(building, buildingCompleted);
            _buildingIcon.gameObject.SetActive(true);

            if(fadeIn) {
                _buildingIcon.FadeIn();
            }
        }
    }

    public void SetText(string str)
    {
        _text.text = str;
        _buildingIcon.GetComponent<RectTransform>().anchoredPosition = new Vector3(_text.preferredWidth*0.5f + 20f, 0f, 0f);
    }

    public void FadeIn()
    {
        _text.color = new Color(1f, 1f, 1f, 0f);
        _text.DOColor(Color.white, 1f);
        gameObject.SetActive(true);
    }
}
