using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UnitEllipse : MonoBehaviour
{
    public SpriteRenderer[] renderers;

    public Unit unit;

    float _highlightCountdown = 0f;

    bool _highlight = false;
    public bool highlight {
        get { return _highlight; }
        set {
            if(value != _highlight) {
                _highlight = value;
                _highlightCountdown = 0f;
            }
        }
    }

    void HighlightPulse()
    {
        foreach(var r in renderers) {
            SpriteRenderer clone = Instantiate(r, transform);
            SetColorHue(new SpriteRenderer[] { clone });

            float startTime = Time.time;

            float duration = 1f;

            clone.transform.DOLocalMoveY(clone.transform.localPosition.y+0.1f, duration);
            clone.transform.DOScale(1.5f, duration).OnComplete(() => GameObject.Destroy(clone.gameObject))
                 .OnUpdate(() => {
                     float t = (Time.time - startTime)/duration;
                     MaterialPropertyBlock block = new MaterialPropertyBlock();
                     clone.GetPropertyBlock(block);
                     block.SetColor("_ColorMult", new Color(1f,1f,1f,1f-t));
                     clone.SetPropertyBlock(block);
                 });
        }
    }

    void SetScaling(float r)
    {
        transform.localScale = Vector3.one*r;
    }

    public void SetColorHue()
    {
        SetColorHue(renderers);
    }

    public void SetColorHue(SpriteRenderer[] renderersList)
    {
        float h, s, v;
        Color.RGBToHSV(unit.team.coloring.color, out h, out s, out v);
        foreach(SpriteRenderer renderer in renderersList) {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetFloat("_Hue", h);
            block.SetFloat("_Saturation", s);
            block.SetFloat("_Value", v);
            renderer.SetPropertyBlock(block);
        }
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.localPosition;
        pos.y = unit.waterline*0.5f;
        transform.localPosition = pos;

        if(_highlight) {
            _highlightCountdown -= Time.deltaTime;

            if(_highlightCountdown <= 0f) {
                _highlightCountdown += 0.5f;

                HighlightPulse();
            }
        }
    }
}
