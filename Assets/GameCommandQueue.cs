using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class GameCommandQueue : MonoBehaviour
{
    List<GameCommand> _queue = new List<GameCommand>();

    public void QueueCommand(GameCommand cmd)
    {
        if(GameController.instance.localPlayerTurn) {
            GameController.instance.ClearCommandHints(cmd);
        }

        _queue.Add(cmd);
    }

    public bool empty {
        get { return _queue.Count == 0; }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    int _nframe = 0;

    public static float elapsedTime = 0.0f;

    static ProfilerMarker s_profilePumpQueueObject = new ProfilerMarker("PumpQueueIteration");


    public void PumpQueue()
    {
        while(_queue.Count > 0 && (_queue[0].finished || (_queue[0].gameObject.activeSelf == false && _queue[0].TryRunImmediately()))) {
            using(s_profilePumpQueueObject.Auto()) {
                bool isActive = _queue[0].gameObject.activeSelf;


                if(isActive) {
                    elapsedTime += (Time.time - _queue[0].startExecuteTime);
                }

                GameController.instance.gameState.stateid++;

                GameObject.Destroy(_queue[0].gameObject);
                _queue.RemoveAt(0);

                if(isActive && GameController.instance.localPlayerTurn) {
                    GameController.instance.RefreshUnitDisplayed();
                }
            }
        }

        if(_queue.Count > 0 && _queue[0].gameObject.activeSelf == false) {
            _queue[0].gameObject.SetActive(true);
            _queue[0].startExecuteTime = Time.time;
            _queue[0].startExecuteFrame = _nframe;
        }

    }

    // Update is called once per frame
    void Update()
    {
        ++_nframe;

        if(GameConfig.modalDialog == 0) {
            PumpQueue();
        }
    }
}
