using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackWarning : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer _renderer = null;

    [SerializeField]
    Sprite _diplomacyIcon = null;

    public Unit unit;

    public Loc loc;

    bool _diplomacy = false;
    public void SetDiplomacy()
    {
        _renderer.sprite = _diplomacyIcon;
        _diplomacy = true;
    }

    //theoretical means the attack warning only
    //is valid for movements without enemy zocs.
    public bool theoretical = false;

    // Start is called before the first frame update
    void Start()
    {
        transform.localPosition = Tile.LocToPos(loc) + new Vector3(-0.3f, -0.2f, 0f);

        if(theoretical) {
            _renderer.color = new Color(0.5f, 0.5f, 0.5f);
        } else {
            Update();
            _renderer.color = new Color(1f, 1f, 1f, 0.2f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(unit == null) {
            GameObject.Destroy(gameObject);
            return;
        }

        if(unit.gameObject.activeSelf == false) {
            _renderer.color = Color.clear;
        }
        else if(theoretical == false) {
            float t = Time.time - Mathf.Floor(Time.time);
            _renderer.color = t < 0.5f ? Color.white : (_diplomacy ? Color.blue : Color.red);
        } else {
            _renderer.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }
}
