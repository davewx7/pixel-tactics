using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileCursor : MonoBehaviour
{
    [SerializeField]
    Tile _tile = null;

    [SerializeField]
    SpriteRenderer _renderer = null;

    public enum Highlight
    {
        None,
        Defocus,
        Move,
        Attack,
        Spell,
        Diplomacy,
        DebugMark,
    }

    bool _previewMove = false;
    public bool previewMove {
        get { return _previewMove; }
        set {
            if(value != _previewMove) {
                _previewMove = value;
                Recalculate();
            }
        }
    }

    Highlight _highlight = Highlight.None;
    public Highlight highlight {
        get { return _highlight; }
        set {
            if(_highlight != value) {
                _highlight = value;
                Recalculate();
            }
        }
    }

    bool _mouseover = false;
    public bool mouseover {
        get { return _mouseover; }
        set {
            if(value != _mouseover) {
                _mouseover = value;
                Recalculate();
            }
        }
    }

    void Recalculate()
    {
        Color col = new Color(1f, 1f, 1f, 0f);
        switch(_highlight) {
            case Highlight.Move:
                col = new Color(0.5f, 0.5f, 0.9f, 0.1f + Mathf.Sin(Time.time*6f)*0.05f);
                break;
            case Highlight.Attack:
                col = new Color(0.9f, 0.3f, 0.3f, 0.5f + Mathf.Sin(Time.time*6f)*0.1f);
                break;
            case Highlight.Spell:
                col = new Color(0.9f, 0.3f, 0.9f, 0.5f + Mathf.Sin(Time.time*6f)*0.1f);
                break;
            case Highlight.Diplomacy:
                col = new Color(0.3f, 0.9f, 0.9f, 0.5f + Mathf.Sin(Time.time*6f)*0.1f);
                break;
            case Highlight.DebugMark:
                col = new Color(1.0f, 0.0f, 0.0f, 0.5f + Mathf.Sin(Time.time*6f)*0.1f);
                break;
        }

        bool mouseoverHighlight = _mouseover || previewMove;
        if(mouseoverHighlight) {
            col.a += 0.2f;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(block);
        block.SetFloat(mouseoverPropertyId, mouseoverHighlight ? 0.4f : 0.0f);
        block.SetColor(colorPropertyId, col);
        _renderer.SetPropertyBlock(block);

    }

    static int _mouseoverPropertyId = -1;
    static int _colorPropertyId = -1;

    static int mouseoverPropertyId {
        get {
            if(_mouseoverPropertyId == -1) {
                _mouseoverPropertyId = Shader.PropertyToID("_Mouseover");
            }

            return _mouseoverPropertyId;
        }
    }

    static int colorPropertyId {
        get {
            if(_colorPropertyId == -1) {
                _colorPropertyId = Shader.PropertyToID("_Color");
            }

            return _colorPropertyId;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(_highlight != Highlight.None) {
            Recalculate();
        }
    }

    private void OnMouseOver()
    {
        _tile.OnMouseOver();
    }

    private void OnMouseEnter()
    {
        _tile.OnMouseEnter();
    }

    private void OnMouseExit()
    {
        _tile.OnMouseExit();
    }

    private void OnMouseUpAsButton()
    {
        _tile.OnMouseUpAsButton();
    }
}
