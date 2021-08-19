using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;


public class BuildingIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    Image _image = null;

    public Loc loc;

    VillageBuilding _building = null;

    float _mouseoverTime = -1f;

    bool _buildingComplete = false;

    public void SetBuilding(VillageBuilding building, bool buildingCompleted=true)
    {
        _image.sprite = buildingCompleted ? building.icon : GameConfig.instance.underConstructionIcon;
        _building = building;
        _buildingComplete = buildingCompleted;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_building != null) {
            _mouseoverTime = Time.time;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _mouseoverTime = -1f;
        GameCanvas.instance.ClearTooltip(transform);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
    }

    public void OnDisable()
    {
        _mouseoverTime = -1f;
        GameCanvas.instance.ClearTooltip(transform);
    }

    public void FadeIn()
    {
        Vector3 scale = transform.localScale;
        transform.localScale = scale*4f;
        transform.DOScale(scale, 1f);

        Color color = _image.color;
        _image.color = new Color(1f, 1f, 1f, 0f);
        _image.DOColor(color, 1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_building != null && _mouseoverTime > 0f && Time.time > _mouseoverTime+1f) {
            int nroundsToComplete = GameController.instance.gameState.GetLocOwnerInfo(loc).roundsUntilBuildingComplete;

            string underConstructionText = "";
            if(nroundsToComplete > 0) {
                underConstructionText = string.Format("\n<b><color=#aaaaaa>Under construction: this building will be complete in {0} {1}</color></b>", nroundsToComplete, nroundsToComplete == 1 ? "moon" : "moons");
            }

            _mouseoverTime = -1f;
            GameCanvas.instance.ShowTooltip(string.Format("<color=#ffffff>{0}</color>: <color=#bbbbbb>{1}</color>{2}", _building.description, _building.rulesText, underConstructionText), new TooltipText.Options(), transform);
        }
    }
}
