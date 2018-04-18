using System.Collections.Generic;
using UnityEngine;

public class MecanimEventEmitterWithData : MonoBehaviour {
    public Animator animator;

    public Object animatorController;
    public MecanimEventData data;
    public MecanimEventEmitTypes emitType = MecanimEventEmitTypes.Default;

    private readonly Dictionary<int, Dictionary<int, AnimatorStateInfo>> lastStates =
        new Dictionary<int, Dictionary<int, AnimatorStateInfo>>();

    private Dictionary<int, Dictionary<int, Dictionary<int, List<MecanimEvent>>>> loadedData;

    private void Start() {
        if (animator == null) {
            Debug.LogWarning(string.Format("GameObject:{0} cannot find animator component.", transform.name));
            enabled = false;
            return;
        }

        if (animatorController == null) {
            Debug.LogWarning("Please assgin animator in editor. Add emitter at runtime is not currently supported.");
            enabled = false;
            return;
        }

        if (data == null) {
            enabled = false;
            return;
        }

        loadedData = MecanimEventManager.LoadData(data);
    }

    private void Update() {
        var events =
            MecanimEventManager.GetEvents(loadedData, lastStates, animatorController.GetInstanceID(), animator);

        foreach (var e in events) {
            MecanimEvent.SetCurrentContext(e);

            switch (emitType) {
                case MecanimEventEmitTypes.Upwards:
                    if (e.paramType != MecanimEventParamTypes.None)
                        SendMessageUpwards(e.functionName, e.parameter, SendMessageOptions.DontRequireReceiver);
                    else
                        SendMessageUpwards(e.functionName, SendMessageOptions.DontRequireReceiver);
                    break;

                case MecanimEventEmitTypes.Broadcast:
                    if (e.paramType != MecanimEventParamTypes.None)
                        BroadcastMessage(e.functionName, e.parameter, SendMessageOptions.DontRequireReceiver);
                    else
                        BroadcastMessage(e.functionName, SendMessageOptions.DontRequireReceiver);
                    break;

                default:
                    if (e.paramType != MecanimEventParamTypes.None)
                        SendMessage(e.functionName, e.parameter, SendMessageOptions.DontRequireReceiver);
                    else
                        SendMessage(e.functionName, SendMessageOptions.DontRequireReceiver);
                    break;
            }
        }
    }
}