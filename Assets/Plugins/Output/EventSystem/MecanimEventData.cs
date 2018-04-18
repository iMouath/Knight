using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class MecanimEventData : MonoBehaviour {
    public MecanimEventDataEntry[] data;

    public Object lastEdit;
}

[Serializable]
public class MecanimEventDataEntry {
    public Object animatorController;
    public MecanimEvent[] events;
    public int layer;
    public int stateNameHash;

    public MecanimEventDataEntry() {
        events = new MecanimEvent[0];
    }

    public MecanimEventDataEntry(MecanimEventDataEntry other) {
        animatorController = other.animatorController;
        layer = other.layer;
        stateNameHash = other.stateNameHash;

        if (other.events == null) {
            events = new MecanimEvent[0];
        }
        else {
            events = new MecanimEvent[other.events.Length];

            for (var i = 0; i < events.Length; i++) events[i] = new MecanimEvent(other.events[i]);
        }
    }
}