using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

[System.Serializable]
public struct NetworkedCommand
{
    public int seq;
    public int id;
    public string data;
    public long timestamp;
}

public class GameCommand : MonoBehaviour
{
    public int id = -1;
    public string debugDescription {
        get {
            return this.GetType().Name;
        }
    }

    public bool finished = false;
    public float startExecuteTime = 0f;
    public int startExecuteFrame = 0;

    //If this command was de-serialized from the network.
    public bool runningFromNetwork = false;

    //If the command should just be skipped.
    public virtual bool elide {
        get {
            return false;
        }
    }

    public bool TryRunImmediately()
    {
        using(s_profileGameCommand.Auto()) {
            finished = elide || RunImmediately();
            return finished;
        }
    }

    //Runs the command immediately without using a co-routine if possible.
    //The command will return true if it ran immediately and is now finished.
    public virtual bool RunImmediately()
    {
        return false;
    }

    public virtual string Serialize() { return ""; }
    public virtual void Deserialize(string data) { }

    static ProfilerMarker s_profileGameCommand = new ProfilerMarker("GameCommand.TryRunImmediately");


    public void Upload()
    {
        if(GameController.instance.spectating) {
            return;
        }

        GameController.instance.gameState.seq++;

        string path = string.Format("/games/{0}/moves/round{1}/turn{2}", GameController.instance.gameState.guid, GameController.instance.gameState.nround, GameController.instance.gameState.nturn);
        string payload = Glowwave.Json.ToJson(ToNetworkedCommand());
        CloudInterface.instance.PushData(path, payload);
    }

    public NetworkedCommand ToNetworkedCommand()
    {
        if(id < 0 || id >= GameController.instance.networkableCommands.Count) {
            Debug.LogError("Invalid network command: " + id + " " + this.GetType().Name);
        }

        NetworkedCommand result = new NetworkedCommand() {
            seq = GameController.instance.gameState.seq,
            id = id,
            data = Serialize(),
            timestamp = Glowwave.Json.firebaseTimestampLongPlaceholder,
        };

        return result;
    }

    public static GameCommand FromNetworkedCommand(NetworkedCommand cmd)
    {
        GameCommand result = GameController.instance.InstantiateNetworkCommand(cmd.id);
        result.Deserialize(cmd.data);
        result.runningFromNetwork = true;
        return result;
    }
}
