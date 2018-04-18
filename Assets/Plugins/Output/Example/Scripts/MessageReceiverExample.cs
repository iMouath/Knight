using UnityEngine;

public class MessageReceiverExample : MonoBehaviour {
    private string msg;

    private void OnIdleUpdate(float param) {
        msg = string.Format("OnIdleUpdate received with parameter: {0} {1}", param.GetType(), param);

        // You can also get event context by accessing ...
        // MecanimEvent.Context

        //Debug.Log(MecanimEvent.Context.stateHash);
    }

    private void OnLeftFootGrounded(int param) {
        msg = string.Format("OnLeftFootGrounded received with parameter: {0} {1}", param.GetType(), param);
    }

    private void OnRightFootGrounded(int param) {
        msg = string.Format("OnRightFootGrounded received with parameter: {0} {1}", param.GetType(), param);
    }

    private void OnJumpGrounded(string param) {
        msg = string.Format("OnJumpGrounded received with parameter: {0} {1}", param.GetType(), param);
    }

    private void OnJumpStarted(bool param) {
        msg = string.Format("OnJumpGrounded received with parameter: {0} {1}", param.GetType(), param);
    }

    private void JumpEndCritical() {
        msg = "JumpEndCritical received with no parameter";
    }

    private void OnWaved() {
        msg = "OnWaved received";
    }

    private void OnGUI() {
        GUI.Label(new Rect(20, 40, 600, 20), msg);
    }
}