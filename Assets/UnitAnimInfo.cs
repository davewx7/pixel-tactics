using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimType { Stand, Idle, Other, Defend, Die, Cast }

public enum UnitGender { Male, Female, None }

[System.Serializable]
public struct AnimMatch
{
    public UnitGender gender;
    public Tile.Direction direction;
    public AnimType animType;
    public string tag;
    public string zombietype;

    public bool IsEqual(AnimMatch m)
    {
        return gender == m.gender && direction == m.direction && animType == m.animType && tag == m.tag && zombietype == m.zombietype;
    }

    public int MatchQuality(AnimMatch m)
    {
        int result = 0;
        if((string.IsNullOrEmpty(zombietype) && string.IsNullOrEmpty(m.zombietype)) || m.zombietype == zombietype) {
            result += 100000;
        }

        if(m.gender == gender) {
            result += 10000;
        } else if(gender == UnitGender.None) {
            result += 1;
        }

        if(m.animType == animType) {
            result += 1000;
        } else if(m.animType == AnimType.Stand || animType == AnimType.Stand) {
            result += 500;
        }

        if(m.tag == tag) {
            result += 100;
        }

        if(m.direction == direction) {
            result += 20;
        } else if(Tile.GetDirectionEastify(m.direction) == direction) {
            result += 8;
        } else if(Tile.GetDirectionNorthSouth(m.direction) == direction) {
            result += 2;
        }

        return result;
    }

}

[System.Serializable]
public struct AnimationEvent
{
    public string eventName;
    public float time;
}

[System.Serializable]
public class AnimInfo
{
    public static bool IsValid(AnimInfo anim)
    {
        return anim != null && anim.sprites != null && anim.sprites.Length > 0;
    }

    public AnimMatch matchInfo;

    public AnimType animType {  get { return matchInfo.animType; } }
    public Sprite[] sprites;
    public float duration = 1f;
    public bool reverse = false;
    public bool cycle = false;

    public AnimationEvent[] events;

    public void EnsureSpriteIndexValid(int n)
    {
        if(sprites == null || sprites.Length <= n) {
            Sprite[] newSprites = new Sprite[n+1];
            if(sprites != null) {
                for(int i = 0; i != sprites.Length; ++i) {
                    newSprites[i] = sprites[i];
                }
            }

            sprites = newSprites;
        }
    }

    public float GetEventTiming(string eventName, float defaultValue)
    {
        foreach(AnimationEvent e in events) {
            if(e.eventName == eventName) {
                return e.time;
            }
        }

        return defaultValue;
    }

    public bool valid {
        get { return sprites.Length > 0; }
    }
}

[System.Serializable]
public class AnimPlaying
{
    AnimInfo _anim;
    public AnimInfo anim { get { return _anim; } }
    float _time = 0f;
    public float timePlaying = 0f;
    public AnimPlaying(AnimInfo info)
    {
        _anim = info;
        if(_anim.duration <= 0f) {
            _anim.duration = 1f;
        }
    }

    public AnimType animType { get { return _anim.animType; } }

    public void Step(float t)
    {
        t *= GameController.timeScale;
        timePlaying += t;
        _time += t;
        while(_anim.cycle && _time >= _anim.duration) {
            _time -= _anim.duration;
        }
    }

    public bool finished {
        get {
            return _anim.cycle == false && _time >= _anim.duration;
        }
    }

    public Sprite sprite {
        get {
            int numFrames = _anim.sprites.Length + (_anim.reverse ? (_anim.sprites.Length-1) : 0);
            float timePerFrame = _anim.duration/numFrames;
            int nframe = (int)(_time/timePerFrame);
            if(_anim.reverse && nframe >= _anim.sprites.Length) {
                nframe -= _anim.sprites.Length;
                nframe = _anim.sprites.Length - nframe - 2;
            }

            if(nframe >= _anim.sprites.Length) {
                nframe = _anim.sprites.Length-1;
            }

            if(nframe < 0) {
                nframe = 0;
            }

            return _anim.sprites[nframe];
        }
    }
}

[CreateAssetMenu(menuName = "Wesnoth/UnitAnimInfo")]
public class UnitAnimInfo : GWScriptableObject
{
}