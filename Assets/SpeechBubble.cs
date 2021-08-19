using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField]
    RectTransform _rectTransform = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    [SerializeField]
    SpriteRenderer _renderer = null;

    public string text = "TEST TEST";

    public float alpha {
        get {
            return _renderer.color.a;
        }
        set {
            _renderer.color = new Color(1f, 1f, 1f, value);
            _text.color = new Color(0f, 0f, 0f, value);
        }
    }

    float _createTime = -1f;

    // Start is called before the first frame update
    void Start()
    {
        _createTime = Time.time;

        Vector2 sz = _text.GetPreferredValues(text);

        _rectTransform.sizeDelta = sz;
        _renderer.size = new Vector2(sz.x/26f, _renderer.size.y);

        transform.localPosition = new Vector3(sz.x/500f, 0.8f, 0f);


        _text.text = text;

        Vector3 targetPos = transform.localPosition;
        transform.position += new Vector3(0f, -0.3f, 0f);

        alpha = 0f;

        transform.DOLocalMove(targetPos, 0.4f);
        DOTween.To(() => alpha, x => alpha = x, 1f, 0.4f);
    }

    // Update is called once per frame
    void Update()
    {
        if(_createTime > 0f && Time.time - _createTime > 2.2f) {
            _createTime = -1f;
            DOTween.To(() => alpha, x => alpha = x, 0f, 0.4f).OnComplete(() => GameObject.Destroy(gameObject));
        }
    }
}
