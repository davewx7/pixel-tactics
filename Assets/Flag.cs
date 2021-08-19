using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer _renderer;

    public FlagType flagType = null;

    AnimPlaying _currentAnim;

    int _team = -1;

    public int team {
        get { return _team; }
        set {
            if(value != _team) {
                _team = value;

                Team team = GameController.instance.teams[value].team;
                if(team.flagType != null) {
                    flagType = team.flagType;
                    Start();
                }

                TeamColoring tc = team.coloring;
                Vector3 hsv = tc.hsv;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(block);
                block.SetFloat("_TeamColorHue", hsv.x);
                block.SetFloat("_TeamColorSaturation", hsv.y);

                _renderer.SetPropertyBlock(block);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _currentAnim = new AnimPlaying(flagType.anim);
    }

    // Update is called once per frame
    void Update()
    {
        _currentAnim.Step(Time.deltaTime);
        _renderer.sprite = _currentAnim.sprite;
    }
}
