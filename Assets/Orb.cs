using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Orb : MonoBehaviour
{
    public enum Status { Uninit, Unmoved, PartMoved, Exhausted, Ally, Enemy, Indifferent };
    Status _status = Status.Uninit;
    public Status status {
        get { return _status; }
        set {
            if(value != _status) {

                if(value == Status.Enemy) {
                    gameObject.SetActive(false);
                    _status = value;
                    return;
                } else {
                    gameObject.SetActive(true);
                }

                float newValue = 0f;
                switch(value) {
                    case Status.Unmoved: newValue = 0.3f; break;
                    case Status.PartMoved: newValue = 0.15f; break;
                    case Status.Exhausted: newValue = 0f; break;
                    case Status.Ally: newValue = 0.7f; break;
                    case Status.Indifferent: newValue = 0.5f; break;
                }

                DOTween.To(() => hue, v => hue = v, newValue, _status == Status.Uninit ? 0f : 0.5f);

                _status = value;
            }
        }
    }

    [SerializeField]
    SpriteRenderer _renderer;

    float _hue = 0.0f;

    public float hue {
        get { return _hue; }
        set {
            _hue = value;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            block.SetFloat("_Hue", value);
            _renderer.SetPropertyBlock(block);
        }
    }

    float _alpha = 1.0f;
    public float alpha {
        get { return _alpha; }
        set {
            if(_alpha != value) {
                _alpha = value;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetFloat("_Alpha", value);
                _renderer.SetPropertyBlock(block);
            }
        }
    }

    public float targetHue = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
