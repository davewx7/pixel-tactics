using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusBar : MonoBehaviour
{
    Color[] _backgroundColors = null;

    [SerializeField]
    Image[] _backgrounds = null;

    [SerializeField]
    int _colorThreshold = 0;

    [SerializeField]
    Color[] _fillColors = null;

    [SerializeField]
    Image _fillImage = null;

    [SerializeField]
    Slider _slider = null;

    [SerializeField]
    RectTransform _rectTransform = null;

    Color _colorMult = new Color(1f, 1f, 1f, 1f);
    public Color colorMult {
        get { return _colorMult; }
        set {
            if(value != _colorMult) {
                _colorMult = value;
                UpdateBar(_currentVal, _currentMax);

                if(_backgrounds != null && _backgroundColors != null) {
                    for(int i = 0; i != _backgrounds.Length; ++i) {
                        _backgrounds[i].color = _backgroundColors[i]*_colorMult;
                    }
                }
            }
        }
    }

    int _currentVal = -1, _currentMax = -1;

    public int targetValue = -1;
    public int targetMax = -1;
    public int targetMaxColor = -1;

    void UpdateBar(int currentValue, int maxValue)
    {
        float barHeight = maxValue*0.7f + 2f;
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, -14f - barHeight);
        _rectTransform.sizeDelta = new Vector2(barHeight, _rectTransform.sizeDelta.y);

        float newValue = ((float)currentValue)/(float)maxValue;
        _slider.value = newValue;

        int colorIndex = 0;
        if(_colorThreshold > 0) {
            if(currentValue >= maxValue) {
                colorIndex = 2;
            } else if(currentValue >= maxValue - _colorThreshold) {
                colorIndex = 1;
            } else {
                colorIndex = 0;
            }
        } else {
            float r = newValue;
            if(targetMaxColor != -1) {
                r = ((float)currentValue)/(float)targetMaxColor;
            }

            if(r <= 0.3334f) {
                colorIndex = 2;
            } else if(r <= 0.6667f) {
                colorIndex = 1;
            } else {
                colorIndex = 0;
            }
        }

        if(colorIndex >= _fillColors.Length) {
            colorIndex = _fillColors.Length-1;
        }

        _fillImage.color = _fillColors[colorIndex]*_colorMult;
    }

    private void Awake()
    {
        if(_backgrounds != null) {
            _backgroundColors = new Color[_backgrounds.Length];
            for(int i = 0; i != _backgrounds.Length; ++i) {
                _backgroundColors[i] = _backgrounds[i].color;
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
        if(targetValue != -1 && targetMax != -1) {
            if(_currentMax == -1 || _currentVal == -1) {
                _currentVal = targetValue;
                _currentMax = targetMax;
                UpdateBar(_currentVal, _currentMax);
            } else if(_currentMax != targetMax || _currentVal != targetValue) {
                if(_currentMax < targetMax) {
                    ++_currentMax;
                } else if(_currentMax > targetMax) {
                    --_currentMax;
                }

                if(_currentVal < targetValue) {
                    ++_currentVal;
                } else if(_currentVal > targetValue) {
                    --_currentVal;
                }

                UpdateBar(_currentVal, _currentMax);
            }
        }
    }
}
