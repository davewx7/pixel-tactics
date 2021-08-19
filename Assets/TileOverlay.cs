using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileOverlay : MonoBehaviour
{
    public SpriteRenderer renderer;

    private void OnEnable()
    {
        renderer.sortingOrder = -(int)(transform.position.y*5f);
    }

    // Start is called before the first frame update
    void Start()
    {
        renderer.sortingOrder = -(int)(transform.position.y*5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
