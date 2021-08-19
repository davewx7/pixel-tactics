using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HintsPanel : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    public List<GameHint> hints;

    GameHint _hintShown = null;

    [SerializeField]
    Image _highlightProto = null;

    Image _highlight = null;
    float _highlightSpawn = 0f;

    [SerializeField]
    SpriteRenderer _tileHighlightProto = null;

    List<SpriteRenderer> _tileHighlights = new List<SpriteRenderer>();

    public void OnCommandExecution(GameCommand cmd)
    {
        var t = cmd.GetType();
        foreach(GameHint hint in hints) {
            if(hint.commandExpiresThisHint != null && hint.commandExpiresThisHint.GetType() == cmd.GetType()) {
                GameController.instance.gameState.AddHintExpired(hint.name);
            }
        }
    }

    public void ShowBestHint(Unit unit)
    {
        GameHint best = null;
        if(GameConfig.modalDialog == 0 && TurnBanner.numActive == 0) {
            foreach(GameHint hint in hints) {
                if(GameController.instance.gameState.hintsExpired.Contains(hint.name)) {
                    continue;
                }

                if(hint.CanShow(unit)) {
                    best = hint;
                    break;
                }
            }
        }

        if(best == _hintShown) {
            return;
        }

        if(_highlight != null) {
            GameObject.Destroy(_highlight.gameObject);
            _highlight = null;
        }

        foreach(var h in _tileHighlights) {
            GameObject.Destroy(h.gameObject);
        }

        _tileHighlights.Clear();

        if(best != null) {
            _text.text = string.Format("Hint: <color=#aaaaaa>{0}</color>", best.text);

            if(best.targetUIElement != null) {
                _highlight = Instantiate(_highlightProto, best.targetUIElement);
                _highlight.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, best.targetUIElement.sizeDelta.x + 4f);
                _highlight.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, best.targetUIElement.sizeDelta.y + 4f);
                _highlightSpawn = Time.time;
            }

            if(best.recruitmentHint) {
                Unit ruler = GameController.instance.playerTeamInfo.GetRuler();
                Dictionary<Loc, Pathfind.Path> paths = Pathfind.FindPaths(GameController.instance, ruler.unitInfo, 1, new Pathfind.PathOptions() {
                    recruit = true,
                    excludeOccupied = true,
                    excludeSelf = true,
                });

                foreach(var p in paths) {
                    Tile t = GameController.instance.map.GetTile(p.Key);
                    var highlight = Instantiate(_tileHighlightProto, t.transform);
                    _tileHighlights.Add(highlight);
                }
            }
        }

        gameObject.SetActive(best != null);
        _hintShown = best;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_highlight != null) {
            float t = Time.time - _highlightSpawn;
            _highlight.gameObject.SetActive(t - Mathf.Floor(t) < 0.5f);
        }

        foreach(var tileHighlight in _tileHighlights) {
            float t = Time.time - _highlightSpawn;
            tileHighlight.gameObject.SetActive(t - Mathf.Floor(t) < 0.5f);
        }
    }
}
