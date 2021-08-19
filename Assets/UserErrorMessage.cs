using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserErrorMessage : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI _text = null;

    float _ttl = 0f;

    public void Show(string msg)
    {
        _text.text = msg;
        _ttl = 3f;
        gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        _ttl -= Time.deltaTime;
        if(_ttl < 0f) {
            gameObject.SetActive(false);
        }
    }
}
