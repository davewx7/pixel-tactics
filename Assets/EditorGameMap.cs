using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class EditorGameMap : MonoBehaviour
{
    [SerializeField]
    Tile _tilePrefab;

    [SerializeField]
    GameMap _map;

    public Loc dimensions;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR

        if(EditorApplication.isPlaying) {
            return;
        }

        if(dimensions != _map.dimensions) {
            _map.dimensions = dimensions;

            Debug.Log("recreate map...");

            List<Tile> newTiles = new List<Tile>();
            for(int y = 0; y != _map.dimensions.y; ++y) {
                for(int x = 0; x != _map.dimensions.x; ++x) {
                    Loc loc = new Loc(x, y, _map.depth);
                    Tile newTile = null;
                    foreach(Tile t in _map.tiles) {
                        if(t != null && t.loc == loc) {
                            newTile = t;
                            break;
                        }
                    }

                    if(newTile == null) {
                        newTile = (PrefabUtility.InstantiatePrefab(_tilePrefab.gameObject) as GameObject).GetComponent<Tile>();
                        newTile.transform.SetParent(transform);
                        newTile.map = _map;
                        newTile.loc = loc;
                    }

                    newTiles.Add(newTile);
                }
            }

            foreach(Tile t in _map.tiles) {
                if(t != null && newTiles.Contains(t) == false) {
                    GameObject.DestroyImmediate(t.gameObject);
                }
            }

            _map.tiles = newTiles;
            Debug.Log("map recreated: " + _map.tiles.Count);
            EditorUtility.SetDirty(_map);

            foreach(Tile t in _map.tiles) {
                t.CalculatePosition();
            }
        }
#endif
    }
}
