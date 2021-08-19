using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TileEdit : MonoBehaviour
{
    [SerializeField]
    Tile _tile;

    // Start is called before the first frame update
    void Start()
    {
        _tile.CalculatePosition();
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if(UnityEditor.EditorApplication.isPlaying == false) {
            _tile.CalculatePosition();
        }
#endif
    }
}
