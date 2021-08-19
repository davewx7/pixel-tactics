using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GenericCommand : GameCommand
{
    public static GenericCommand currentCommand = null;

    public int commandId = -1;

    public void Finish()
    {
        finished = true;
        if(currentCommand == this) {
            currentCommand = null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        currentCommand = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class GenericCommandInfo
{
    static int currentId = 1;

    int _id;

    public int id {
        get { return _id; }
    }

    public GenericCommandInfo()
    {
        _id = currentId++;
    }

    public bool running {
        get {
            return GenericCommand.currentCommand != null && GenericCommand.currentCommand.commandId == _id;
        }
    }

    public void Finish()
    {
        if(running) {
            GenericCommand.currentCommand.Finish();
        }
    }
}