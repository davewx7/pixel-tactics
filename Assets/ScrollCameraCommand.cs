using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class ScrollCameraCommandInfo
{
    public Loc target;
}


public class ScrollCameraCommand : GameCommand
{
    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<ScrollCameraCommandInfo>(data); }

    [SerializeField]
    GameController _controller;

    [SerializeField]
    Camera _camera;

    public bool scrollToFog = false;

    public bool waitForCompletion = true;

    [HideInInspector]
    public bool instant = false;

    public ScrollCameraCommandInfo info;

    public Vector3 offset = Vector3.zero;

    public static void CameraLookAt(Loc loc)
    {
        Vector3 targetPos = Tile.LocToPos(loc);
        targetPos.z = Camera.main.transform.localPosition.z;

        Camera.main.transform.localPosition = targetPos;
    }

    IEnumerator Execute()
    {
        Vector3 targetPos = Tile.LocToPos(info.target) + offset;
        targetPos.z = _camera.transform.localPosition.z;
        float distance = Vector3.Distance(_camera.transform.localPosition, targetPos);

        float travelSpeed = 12f;
        float maxTravelTime = 0.5f;

        float duration = Mathf.Min(maxTravelTime, distance / travelSpeed);
        if(instant) {
            duration = 0f;
        }

        var tween = _camera.transform.DOLocalMove(targetPos, duration);

        if(waitForCompletion) {
            yield return tween.WaitForCompletion();
        }

        finished = true;
    }

    public override bool elide {
        get {
            Tile tile = GameController.instance.map.GetTile(info.target);
            if(GameController.instance.IsLocNearCenterOfScreen(info.target) || (tile.fogged && !scrollToFog)) {
                //Loc is already on screen so just abort.
                return true;
            }

            return false;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        if(elide) {
            finished = true;
            return;
        }
        StartCoroutine(Execute());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
