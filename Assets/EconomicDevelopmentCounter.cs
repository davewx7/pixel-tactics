using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EconomicDevelopmentCounter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    int _value = -1;

    int _display = -1;
    int _delta = -1;
    float _pauseUntil = 0f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(GameConfig.modalDialog == 0) {
            GameController.instance.ShowScoreDialog(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GameController.instance.ShowScoreDialog(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {}

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int development = GameController.instance.playerTeamInfo.economicDevelopment;

        if(_value == -1) {
            _display = development;
            _delta = 0;
        } else if(development != _value) {
            _pauseUntil = Time.time + 1f;
            _delta = development - _display;
        }

        _value = development;

        if(_delta != 0) {
            if(_pauseUntil <= Time.time) {
                if(_delta > 0) {
                    --_delta;
                    ++_display;
                } else {
                    ++_delta;
                    --_display;
                }
            }
        }

        string buildCost = GameController.instance.playerTeamInfo.scoreNeededForNextLevel.ToString();
        if(_delta == 0) {
            _text.text = string.Format("<color=#aaaaaa>{0}/{1}</color>", _display, buildCost);
        } else {
            _text.text = string.Format("<color=#aaaaaa>{0}/{1}</color><color=#ffffff>{2}{3}</color>", _display, buildCost, _delta > 0 ? "+" : "", _delta);
        }
    }
}
