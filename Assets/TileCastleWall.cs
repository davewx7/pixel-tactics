using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileCastleWall : MonoBehaviour
{
    public SpriteRenderer renderer;

    private void OnEnable()
    {
        renderer.sortingOrder = -(int)(transform.localPosition.y*5f) + 1;
    }


    // Start is called before the first frame update
    void Start()
    {
        renderer.sortingOrder = -(int)(transform.position.y*5f) + 1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
