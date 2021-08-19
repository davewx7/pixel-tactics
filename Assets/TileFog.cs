using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileFog : MonoBehaviour
{
    [SerializeField]
    Tile _tile;

    [SerializeField]
    TileFogData _fogInfo = null;

    public List<SpriteRenderer> renderers {
        get {
            return _renderers;
        }
    }

    [SerializeField]
    List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

    SpriteRenderer GetRenderer(int index)
    {
        while(_renderers.Count <= index) {
            _renderers.Add(Instantiate(_renderers[0], transform));
        }

        _renderers[index].gameObject.SetActive(true);
        return _renderers[index];
    }

    void SetNumRenderers(int numRenderers)
    {
        for(int i = numRenderers; i < _renderers.Count; ++i) {
            _renderers[i].gameObject.SetActive(false);
        }
    }

    public bool isFogged { get { return _fogged; } }

    bool _partlyVisible = false;
    public bool atLeastPartlyVisibleToPlayer {
        get {
            return _hidden == false || _partlyVisible;
        }
    }

    bool _hidden = true;
    public bool hiddenFromPlayer {
        get {
            return _hidden;
        }
    }

    bool _fogged = false;
    bool[] _adj = null;

    public void SetFog(bool fogged, bool[] adj)
    {
        _hidden = fogged || adj != null;
        _partlyVisible = adj != null;

        if(fogged && _fogged) {
            return;
        }

        if(!fogged && !_fogged) {
            if(adj == null && _adj == null) {
                return;
            }

            if(adj != null && _adj != null) {
                bool equal = true;
                for(int i = 0; i != adj.Length; ++i) {
                    if(adj[i] != _adj[i]) {
                        equal = false;
                        break;
                    }
                }

                if(equal) {
                    return;
                }
            }
        }

        _fogged = fogged;
        _adj = adj;

        if(_fogInfo == null) {
            return;
        }
        if(fogged) {
            GetRenderer(0).sprite = _fogInfo.fog[(_tile.loc.x*3 + _tile.loc.y)%_fogInfo.fog.Length];
            SetNumRenderers(1);
        } else {
            if(_adj == null) {
                SetNumRenderers(0);
            } else if(_adj[0] && _adj[1] && _adj[2] && _adj[3] && _adj[4] && _adj[5]) {
                GetRenderer(0).sprite = _fogInfo.adj[2];
                GetRenderer(1).sprite = _fogInfo.adj[9];
                SetNumRenderers(2);
            } else {

                int beginIndex = 0;
                while(adj[beginIndex]) {
                    ++beginIndex;
                }

                int rendererIndex = 0;

                for(int i = 0; i != 6; ++i) {
                    int index = (beginIndex+i)%6;
                    if(adj[index%6] == false) {
                        continue;
                    }

                    int ndir = 1;
                    while(adj[(index+ndir)%6] && ndir < 3) {
                        ++ndir;
                    }

                    GetRenderer(rendererIndex).sprite = _fogInfo.adj[(index*3 + ndir-1)];
                    rendererIndex++;
                }

                SetNumRenderers(rendererIndex);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
