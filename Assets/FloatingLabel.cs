using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingLabel : MonoBehaviour
{
    public TMPro.TextMeshProUGUI text;

    float _ttl = 5f;
    float _velocity = 1.0f;
    float _fadeTime = 1f;
    Color _color;

    private void Start()
    {
        _color = text.color;
    }

    private void Update()
    {
        _ttl -= Time.deltaTime;
        if(_ttl < _fadeTime) {
            float a = _ttl/_fadeTime;
            text.color = new Color(_color.r, _color.g, _color.b, _color.a*a);
        }

        transform.position += Vector3.up*Time.deltaTime*_velocity;

        if(_ttl < 0f) {
            GameObject.Destroy(gameObject);
        }
    }
}
