using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Loc
{
    readonly public static Loc invalid = invalidLoc;

    public override bool Equals(object obj)
    {
        if(!(obj is Loc))
            return false;

        Loc other = (Loc)obj;

        return _loc == other._loc && _depth == other._depth;
    }

    public override int GetHashCode()
    {
        return x + y*257 + depth*429827;
    }

    static Loc invalidLoc {
        get { return new Loc(new Vector2Int(0, 0), 0); }
    }

    public Loc(int x, int y, int depth = 1)
    {
        _loc = new Vector2Int(x, y);
        _depth = depth;
    }

    public Loc(Vector2Int loc)
    {
        _loc = loc;
        _depth = 1;
    }

    public Loc(Vector2Int loc, int depth)
    {
        _loc = loc;
        _depth = depth;
    }

    public Loc[] adjacent {
        get {
            return Tile.AdjacentLocs(this);
        }
    }

    public bool IsAdjacent(Loc other)
    {
        return Tile.DistanceBetween(this, other) == 1;
    }

    public override string ToString()
    {
        if(_depth == 1) {
            return string.Format("({0},{1})", x, y);
        } else if(_depth == 2) {
            return string.Format("({0},{1},UNDER)", x, y);
        } else if(_depth == 0) {
            return "(null loc)";
        } else {
            return string.Format("(bad loc: x={0},y={1},depth={2})", x, y, _depth);
        }
    }

    public bool valid {
        get { return _depth != 0; }
    }

    public int x {
        get { return _loc.x; }
    }

    public int y {
        get { return _loc.y; }
    }

    public int depth {
        get { return _depth; }
    }

    public bool underworld {
        get { return _depth == 2; }
    }

    public bool overworld {
        get { return _depth == 1; }
    }

    public Loc toUnderworld {
        get {
            return new Loc(x, y, 2);
        }
    }

    public Loc toOverworld {
        get {
            return new Loc(x, y, 1);
        }
    }

    public Vector2Int vecloc {
        get {
            return _loc;
        }
    }

    public Vector2 pos {
        get { return new Vector2(_loc.x, _loc.y); }
    }

    public Vector3 pos3 {
        get { return new Vector3(_loc.x, _loc.y, 0f); }
    }

    static public bool operator==(Loc a, Loc b)
    {
        return a._loc == b._loc && a._depth == b._depth;
    }

    static public bool operator !=(Loc a, Loc b)
    {
        return a._loc != b._loc || a._depth != b._depth;
    }

    static public Loc operator+(Loc a, Loc b)
    {
        return new Loc() {
            _loc = a._loc + b._loc,
            _depth = a._depth,
        };
    }

    public LocsIter range {
        get { return new LocsIter(this); }
    }


    [SerializeField]
    Vector2Int _loc;

    [SerializeField]
    int _depth;
}

public class LocsIter : IEnumerable<Loc>
{
    Loc _dim;
    int _depth;
    public LocsIter(Loc dim)
    {
        _dim = dim;
        _depth = _dim.depth;
        if(_depth <= 0) {
            _depth = 1;
        }
    }

    public IEnumerator<Loc> GetEnumerator()
    {
        for(int y = 0; y != _dim.y; ++y) {
            for(int x = 0; x != _dim.x; ++x) {
                yield return new Loc(new Vector2Int(x, y), _depth);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}