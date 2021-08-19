using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer _renderer;

    public Tile.Direction direction;

    public ProjectileType projectileType;

    public Vector3 targetPoint;

    AnimPlaying _anim;

    public bool hits = false;
    public bool finished = false;

    public void Impact()
    {

    }

    IEnumerator Run()
    {
        Tweener tween = transform.DOMove(Vector3.Lerp(transform.position, targetPoint, projectileType.distanceTravel), projectileType.flightTime);
        yield return tween.WaitForCompletion();

        finished = true;

        if(AnimInfo.IsValid(projectileType.animImpact)) {
            _anim = new AnimPlaying(projectileType.animImpact);
        } else {
            GameObject.Destroy(gameObject);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        float rotation = 0f;
        _anim = new AnimPlaying(projectileType.GetAnimation(direction, out rotation));
        transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        StartCoroutine(Run());
    }

    // Update is called once per frame
    void Update()
    {
        _anim.Step(Time.deltaTime);
        _renderer.sprite = _anim.sprite;

        if(finished && _anim.finished) {
            GameObject.Destroy(gameObject);
        }
    }
}
