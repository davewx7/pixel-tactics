using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderworldGate : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer _renderer = null;

    [SerializeField]
    Sprite _spriteAbove = null, _spriteBelow = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _renderer.sprite = GameController.instance.underworldDisplayed ? _spriteBelow : _spriteAbove;
    }
}
