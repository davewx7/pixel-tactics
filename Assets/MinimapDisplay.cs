using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapDisplay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    RectTransform _rt = null;

    [HideInInspector]
    public float leftEdge = -1f, rightEdge = -1f, topEdge = -1f, botEdge = -1f, hadj = 0f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(leftEdge == rightEdge) {
            return;
        }

        float left = _rt.transform.position.x + _rt.rect.xMin;
        float right = _rt.transform.position.x + _rt.rect.xMax;
        float top = _rt.transform.position.y + _rt.rect.yMax;
        float bot = _rt.transform.position.y + _rt.rect.yMin;

        float xpos = (eventData.position.x - left)/(right - left);
        float ypos = (eventData.position.y - bot)/(top - bot);

        Camera.main.transform.localPosition = new Vector3(hadj + leftEdge + (rightEdge-leftEdge)*xpos, botEdge + (topEdge-botEdge)*ypos, Camera.main.transform.localPosition.z);
    }
}
