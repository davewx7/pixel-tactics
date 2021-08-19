using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileCastleWallCollection : MonoBehaviour
{
    public TileCastleWall[] castleWalls;

    public TileCastleWall WallBL { get { return castleWalls[0]; } }
    public TileCastleWall WallBR { get { return castleWalls[1]; } }
    public TileCastleWall WallL { get { return castleWalls[2]; } }
    public TileCastleWall WallR { get { return castleWalls[3]; } }
    public TileCastleWall WallTL { get { return castleWalls[4]; } }
    public TileCastleWall WallTR { get { return castleWalls[5]; } }

    public void CalculatePosition()
    {
        for(int i = 0; i != castleWalls.Length; ++i) {
            TileCastleWall wall = castleWalls[i];
            wall.gameObject.SetActive(false);
            wall.renderer.sortingOrder = -(int)(transform.position.y*10.0f) - i;
        }
    }
}
