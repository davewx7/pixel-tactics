using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Tooltip : MonoBehaviour
{
    [SerializeField]
    CanvasScaler _scaler = null;

    [SerializeField]
    RectTransform _rectTransform = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    [SerializeField]
    Image _imageIcon = null;

    [HideInInspector]
    public Transform clientTransform = null;

    public Vector2 sizeDelta;

    public TooltipText.Options options = null;

    public string text {
        get { return _text.text; }
        set {
            float imageWidth = 0f;
            float imageHeight = 0f;
            if(options.icon != null) {
                imageWidth = options.iconSize.x + 8f;
                imageHeight = options.iconSize.y + 8f;
                _imageIcon.gameObject.SetActive(true);
                _imageIcon.GetComponent<RectTransform>().sizeDelta = options.iconSize;
                _imageIcon.sprite = options.icon;

                if(options.iconMaterial == null) {
                    _imageIcon.material = GameConfig.instance.inventorySlotMaterialByTier[0];
                } else {
                    _imageIcon.material = options.iconMaterial;
                }
            } else {
                _imageIcon.gameObject.SetActive(false);
            }

            _text.text = value;
            sizeDelta = _text.GetPreferredValues(_text.text) + new Vector2(imageWidth, 0f); //, _rectTransform.sizeDelta.x, _rectTransform.sizeDelta.y);
            if(sizeDelta.x + imageWidth > 500f) {
                sizeDelta = _text.GetPreferredValues(_text.text, 500f, sizeDelta.y);
                sizeDelta = new Vector2(500f + imageWidth, sizeDelta.y);
            }

            if(sizeDelta.y < imageHeight) {
                sizeDelta = new Vector2(sizeDelta.x, imageHeight);
            }

            _rectTransform.sizeDelta = sizeDelta; // new Vector2(_rectTransform.sizeDelta.x, values.y);
            _text.GetComponent<RectTransform>().anchorMin = new Vector2(imageWidth / sizeDelta.x, 0f);
        }
    }

    public void AlignWith(Vector2 pos, bool leftAlign=false)
    {
        _rectTransform.anchoredPosition = new Vector2(pos.x - (leftAlign ? 0f : _rectTransform.sizeDelta.x), -pos.y + _rectTransform.sizeDelta.y*0.5f);

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(clientTransform == null || clientTransform.gameObject.activeInHierarchy == false) {
            gameObject.SetActive(false);
        }
    }
}
