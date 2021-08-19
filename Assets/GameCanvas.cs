using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCanvas : MonoBehaviour
{
    static public GameCanvas instance = null;

    [SerializeField]
    Tooltip _tooltip = null;

    Transform _currentTip = null;

    private void Awake()
    {
        instance = this;
    }

    public void ShowTooltip(string text, TooltipText.Options options, Transform t)
    {
        _currentTip = t;

        _tooltip.options = options;
        _tooltip.gameObject.SetActive(true);
        _tooltip.text = text;
        _tooltip.clientTransform = _currentTip;

        float scaleFactor = 768f/Screen.height;

        Vector3 pos = Input.mousePosition*scaleFactor;
        pos.y = 768f - pos.y;

        float roomOnLeft = pos.x;
        float roomOnRight = Screen.width*scaleFactor - pos.x;
        bool leftAlign = roomOnRight > roomOnLeft;
        _tooltip.AlignWith(new Vector2(pos.x + (leftAlign ? 16f : -16f), Mathf.Max(80f, pos.y)), leftAlign);
    }

    public void ShowTooltip(string text, TooltipText.Options options, RectTransform rectTransform)
    {
        _tooltip.options = options;
        _tooltip.gameObject.SetActive(true);
        _tooltip.text = text;
        _currentTip = rectTransform;
        _tooltip.clientTransform = _currentTip;

        Rect pos = RectTransformToScreenSpace(rectTransform);

        float scaleFactor = 768f/Screen.height;

        float roomOnLeft = pos.x;
        float roomOnRight = Screen.width*scaleFactor - (pos.x + pos.width);

        bool leftAlign = roomOnRight > roomOnLeft;
        _tooltip.AlignWith(new Vector2(leftAlign ? (pos.x + pos.width) : pos.x, Mathf.Max(80f, pos.center.y)), leftAlign);
    }

    public void ClearTooltip(Transform trans)
    {
        if(_currentTip == trans) {
            _tooltip.gameObject.SetActive(false);
            _currentTip = null;
        }
    }

    public static Rect RectTransformToScreenSpace(RectTransform transform)
    {
        float referenceScreenHeight = Screen.height;

        float scaleFactor = 768f/Screen.height;

        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        Rect rect = new Rect(transform.position.x, referenceScreenHeight - transform.position.y, size.x, size.y);
        rect.x -= (transform.pivot.x * size.x);
        rect.y -= ((1.0f - transform.pivot.y) * size.y);

        rect.x *= scaleFactor;
        rect.y *= scaleFactor;
        rect.width *= scaleFactor;
        rect.height *= scaleFactor;

        return rect;
    }

}
