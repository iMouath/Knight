using UnityEngine;

public enum MecanimEventEmitTypes {
    Default,
    Upwards,
    Broadcast
}

public class MecanimEventEmitter : MonoBehaviour {
    public Animator animator;

    public Object animatorController;
    public MecanimEventEmitTypes emitType = MecanimEventEmitTypes.Default;

    private void Start() {
        if (animator == null) {
            Debug.LogWarning("Do not find animator component.");
            enabled = false;
            return;
        }

        if (animatorController == null) {
            Debug.LogWarning("Please assgin animator in editor. Add emitter at runtime is not currently supported.");
            enabled = false;
        }
    }

    private void Update() {
        var events = MecanimEventManager.GetEvents(animatorController.GetInstanceID(), animator);

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