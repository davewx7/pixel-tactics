using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialEffect : MonoBehaviour
{
    public float timeToLive = -1.0f;
    float _expireTime = -1.0f;

    // Start is called before the first frame update
    public virtual void Start()
    {
        if(timeToLive > 0f) {
            _expireTime = Time.time + timeToLive;
        }
    }

    // Update is called once per frame
    public virtual void Update()
    {
        if(_expireTime > 0f && Time.time > _expireTime) {
            GameObject.Destroy(gameObject);
        }
    }
}
