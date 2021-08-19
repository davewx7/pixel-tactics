using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DevelopmentButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public EconomyBuilding building = null;

    [SerializeField]
    EconomicDevelopmentDialog _dialog = null;

    [SerializeField]
    Button _button = null;

    [SerializeField]
    Image _image = null;

    [SerializeField]
    GameObject _selected = null;

    [SerializeField]
    GameObject _highlight = null;

    Color _startingColor;

    public bool highlight {
        get { return _highlight.gameObject.activeSelf; }
        set {
            _highlight.gameObject.SetActive(value);
        }
    }

    public bool selected {
        get { return _selected.gameObject.activeSelf; }
        set {
            _selected.gameObject.SetActive(value);
        }
    }


    public bool built {
        get {
            return GameController.instance.currentTeamInfo.buildingsCompleted.Contains(building);
        }
    }

    public EconomyBuilding GetBuildingAtLoc(Vector2Int loc)
    {
        foreach(EconomyBuilding owned in GameController.instance.currentTeamInfo.team.economyBuildings) {
            if(owned.loc == loc) {
                return owned;
            }
        }

        return null;
    }

    public bool canBeBuilt {
        get {
            if(building.loc == Vector2Int.zero) {
                return true;
            }

            Loc[] adj = Tile.AdjacentLocs(new Loc(building.loc));
            foreach(Loc a in adj) {
                var building = GetBuildingAtLoc(a.vecloc);
                if(GameController.instance.currentTeamInfo.buildingsCompleted.Contains(building)) {
                    return true;
                }
            }

            return false;
        }
    }

    private void OnEnable()
    {
        GameConfig.modalDialog++;
    }

    private void OnDisable()
    {
        GameConfig.modalDialog--;
    }

    public void Init()
    {
        _image.sprite = building.icon;
        transform.localPosition = new Vector3(64f, 0f, 0f) + new Vector3(building.loc.x*68f, building.loc.y*68f + (Mathf.Abs(building.loc.x)%2 == 1 ? 68f*0.5f : 0f), 0f);

        if(built) {
            _image.color = new Color(0.8f, 0.8f, 1f);
        } else if(canBeBuilt) {

        } else {
            //_button.enabled = false;
            _image.color = new Color(0.4f, 0.4f, 0.4f);

        }
    }

    float _highlightColor = 0f;
    public float highlightColor {
        get { return _highlightColor; }
        set {
            _highlightColor = value;
            _image.color = Color.Lerp(_startingColor, Color.white, value);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _startingColor = _image.color;
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        Init();
        highlight = (building == GameController.instance.currentTeamInfo.buildingProject);

        if(highlight) {
            _dialog.canBuildSelected = canBeBuilt;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _dialog.canBuildMouseover = canBeBuilt;
        _dialog.mouseoverBuilding = building;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(_dialog.mouseoverBuilding == building) {
            _dialog.mouseoverBuilding = null;
        }
    }
}
