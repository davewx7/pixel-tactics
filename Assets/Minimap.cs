using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;

public class Minimap : MonoBehaviour
{

    [SerializeField]
    MinimapDisplay _display = null;

    [SerializeField]
    Camera _camera = null, _mainCamera = null;

    [SerializeField]
    Transform _cameraRectTransform = null;

    [SerializeField]
    SpriteRenderer _cameraRectLeft = null, _cameraRectRight = null, _cameraRectTop = null, _cameraRectBot = null;

    [SerializeField]
    float _cameraHShift = 0f;

    [SerializeField]
    SpriteRenderer _tilePrefab = null;
    struct Entry
    {
        public SpriteRenderer renderer;
        public Tile tile;
    }

    List<Entry> _entries = new List<Entry>();

    public void SetVisibleMap(HashSet<Loc> tiles)
    {
        if(tiles.Count == 0) {
            return;
        }

        Vector3 pos = Tile.LocToPos(tiles.First());
        float left = pos.x, right = pos.x, top = pos.y, bot = pos.y;


        foreach(Loc loc in tiles) {
            Tile t = GameController.instance.map.GetTile(loc);
            t.pathInfo.searchID = 0;

            pos = Tile.LocToPos(t.loc);
            if(pos.x < left) {
                left = pos.x;
            }

            if(pos.x > right) {
                right = pos.x;
            }

            if(pos.y > top) {
                top = pos.y;
            }

            if(pos.y < bot) {
                bot = pos.y;
            }
        }

        bool removeTiles = false;
        foreach(Entry entry in _entries) {
            if(tiles.Contains(entry.tile.loc) == false) {
                removeTiles = true;
            }

            entry.tile.pathInfo.searchID = 1;
        }

        if(removeTiles) {
            List<Entry> entries = new List<Entry>();
            foreach(Entry entry in _entries) {
                if(tiles.Contains(entry.tile.loc)) {
                    entries.Add(entry);
                }
            }

            _entries = entries;
        }

        foreach(Loc loc in tiles) {
            Tile t = GameController.instance.map.GetTile(loc);
            if(t.pathInfo.searchID == 0) {
                SpriteRenderer renderer = Instantiate(_tilePrefab, transform);
                renderer.transform.localPosition = Tile.LocToPos(t.loc);
                renderer.gameObject.SetActive(true);

                Entry entry = new Entry {
                    renderer = renderer,
                    tile = t,
                };

                _entries.Add(entry);
            }
        }

        foreach(Entry entry in _entries) {

            if(entry.tile.unit != null && entry.tile.fogged == false) {
                if(entry.tile.unit.team.player) {
                    entry.renderer.color = Color.white;
                } else if(entry.tile.unit.team.IsEnemy(GameController.instance.playerTeam)) {
                    entry.renderer.color = Color.red;
                } else {
                    entry.renderer.color = Color.magenta;

                }
            } else {
                entry.renderer.color = entry.tile.terrain.rules.minimapColor;
            }
        }
        
        left -= 0.75f;
        right += 0.75f;
        top += 1f;
        bot -= 1f;

        _display.topEdge = top;
        _display.botEdge = bot;
        _display.leftEdge = left;
        _display.rightEdge = right;
        _display.hadj = -_cameraHShift;

        _camera.transform.DOLocalMove(new Vector3((left+right)*0.5f, (top+bot)*0.5f, -10f), 0.2f);

        float w = right - left;
        float h = top - bot;
        float dim = Mathf.Max(w, h);

        _camera.DOOrthoSize(dim*0.5f, 0.2f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //_camera.transform.localPosition = _mainCamera.transform.localPosition;

        _cameraRectLeft.transform.localScale = _cameraRectRight.transform.localScale = new Vector3(_camera.orthographicSize*0.1f, (72f/32f)*_mainCamera.orthographicSize*2f, 1f);
        _cameraRectTop.transform.localScale = _cameraRectBot.transform.localScale = new Vector3((72f/32f)*_mainCamera.orthographicSize*2f, _camera.orthographicSize*0.1f, 1f);


        _cameraRectTransform.localPosition = _mainCamera.transform.localPosition + new Vector3(_cameraHShift, 0f, 10f);
    }
}
