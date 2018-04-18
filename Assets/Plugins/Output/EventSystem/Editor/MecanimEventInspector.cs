using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(MecanimEventData))]
public class MecanimEventInspector : Editor {
    private AvatarPreviewWrapper avatarPreview;
    private AnimatorController controller;

    private bool controllerIsDitry;

    // Controller -> Layer -> State
    private Dictionary<AnimatorController, Dictionary<int, Dictionary<int, List<MecanimEvent>>>> data;

    private Motion previewedMotion;

    private bool PrevIKOnFeet;
    private AnimatorState state;
    private AnimatorStateMachine stateMachine;

    private void OnEnable() {
        LoadData();

        MecanimEventEditor.eventInspector = this;
    }

    private void OnDisable() {
        //SaveData();
        MecanimEventEditor.eventInspector = null;

        OnPreviewDisable();
    }

    private void OnDestroy() {
        OnPreviewDestroy();
    }

    public override void OnInspectorGUI() {
        if (GUILayout.Button("Open Event Editor")) {
            var editor = EditorWindow.GetWindow<MecanimEventEditor>();
            editor.TargetController = serializedObject.FindProperty("lastEdit").objectReferenceValue;
        }

        if (previewedMotion != null && previewedMotion is BlendTree && avatarPreview != null) {
            EditorGUILayout.Separator();
            GUILayout.Label("BlendTree Parameter(s)", GUILayout.ExpandWidth(true));

            var bt = previewedMotion as BlendTree;

            for (var i = 0; i < bt.GetRecursiveBlendParamCount(); i++) {
                var min = bt.GetRecursiveBlendParamMin(i);
                var max = bt.GetRecursiveBlendParamMax(i);

                var paramName = bt.GetRecursiveBlendParam(i);
                var value = Mathf.Clamp(avatarPreview.Animator.GetFloat(paramName), min, max);
                value = EditorGUILayout.Slider(paramName, value, min, max);
                avatarPreview.Animator.SetFloat(paramName, value);
            }
        }
    }

    public AnimatorController[] GetControllers() {
        return new List<AnimatorController>(data.Keys).ToArray();
    }

    public void AddController(AnimatorController controller) {
        if (!data.ContainsKey(controller))
            data[controller] = new Dictionary<int, Dictionary<int, List<MecanimEvent>>>();
    }

    public MecanimEvent[] GetEvents(AnimatorController controller, int layer, int stateNameHash) {
        try {
            return data[controller][layer][stateNameHash].ToArray();
        }
        catch {
            return new MecanimEvent[0];
        }
    }

    public void SetEvents(AnimatorController controller, int layer, int stateNameHash, MecanimEvent[] events) {
        if (!data.ContainsKey(controller))
            data[controller] = new Dictionary<int, Dictionary<int, List<MecanimEvent>>>();

        if (!data[controller].ContainsKey(layer)) data[controller][layer] = new Dictionary<int, List<MecanimEvent>>();

        if (!data[controller][layer].ContainsKey(stateNameHash))
            data[controller][layer][stateNameHash] = new List<MecanimEvent>();

        data[controller][layer][stateNameHash] = new List<MecanimEvent>(events);
    }

    public void InsertEventsCopy(AnimatorController controller, int layer, int stateNameHash, MecanimEvent[] events) {
        var allEvents = new List<MecanimEvent>(GetEvents(controller, layer, stateNameHash));

        foreach (var e in events) allEvents.Add(new MecanimEvent(e));

        SetEvents(controller, layer, stateNameHash, allEvents.ToArray());
    }

    public Dictionary<int, Dictionary<int, MecanimEvent[]>> GetEvents(AnimatorController controller) {
        try {
            var events = new Dictionary<int, Dictionary<int, MecanimEvent[]>>();

            foreach (var layer in data[controller].Keys) {
                events[layer] = new Dictionary<int, MecanimEvent[]>();

                foreach (var state in data[controller][layer].Keys) {
                    var stateEvents = new List<MecanimEvent>();

                    foreach (var elem in data[controller][layer][state]) stateEvents.Add(new MecanimEvent(elem));

                    events[layer][state] = stateEvents.ToArray();
                }
            }

            return events;
        }
        catch {
            return new Dictionary<int, Dictionary<int, MecanimEvent[]>>();
        }
    }

    public void InsertControllerEventsCopy(AnimatorController controller,
                                           Dictionary<int, Dictionary<int, MecanimEvent[]>> events) {
        try {
            foreach (var layer in events.Keys)
            foreach (var state in events[layer].Keys)
                InsertEventsCopy(controller, layer, state, events[layer][state]);
        }
        catch {
        }
    }

    public void SetPreviewMotion(Motion motion) {
        if (previewedMotion == motion)
            return;

        previewedMotion = motion;

        ClearStateMachine();

        if (avatarPreview == null) {
            avatarPreview = new AvatarPreviewWrapper(null, previewedMotion);
            avatarPreview.OnAvatarChangeFunc = OnPreviewAvatarChanged;
            PrevIKOnFeet = avatarPreview.IKOnFeet;
        }

        if (motion != null)
            CreateStateMachine();

        Repaint();
    }

    public float GetPlaybackTime() {
        if (avatarPreview != null)
            return avatarPreview.timeControl.normalizedTime;
        return 0;
    }

    public void SetPlaybackTime(float time) {
        if (avatarPreview != null)
            avatarPreview.timeControl.nextCurrentTime = Mathf.Lerp(avatarPreview.timeControl.startTime,
                                                                   avatarPreview.timeControl.stopTime,
                                                                   time);
        Repaint();
    }

    public bool IsPlaying() {
        return avatarPreview.timeControl.playing;
    }

    public void StopPlaying() {
        avatarPreview.timeControl.playing = false;
    }

    public override bool HasPreviewGUI() {
        return true;
    }

    public override void OnPreviewSettings() {
        if (avatarPreview != null)
            avatarPreview.DoPreviewSettings();
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background) {
        if (avatarPreview == null || previewedMotion == null)
            return;

        UpdateAvatarState();
        avatarPreview.DoAvatarPreview(r, background);
    }

    private void OnPreviewDisable() {
        previewedMotion = null;

        ClearStateMachine();
        if (avatarPreview != null) {
            avatarPreview.OnDestroy();
            avatarPreview = null;
        }
    }

    private void OnPreviewDestroy() {
        ClearStateMachine();
        if (avatarPreview != null) {
            avatarPreview.OnDestroy();
            avatarPreview = null;
        }
    }

    private void OnPreviewAvatarChanged() {
        ResetStateMachine();
    }

    private void CreateStateMachine() {
        if (avatarPreview == null || avatarPreview.Animator == null)
            return;

        if (controller == null) {
            controller = new AnimatorController();
            controller.hideFlags = HideFlags.DontSave;
            controller.AddLayer("preview");
            stateMachine = controller.layers[0].stateMachine;

            CreateParameters();
            state = stateMachine.AddState("preview");
            state.motion = previewedMotion;
            state.iKOnFeet = avatarPreview.IKOnFeet;
            state.hideFlags = HideFlags.DontSave;
            stateMachine.hideFlags = HideFlags.DontSave;

            AnimatorController.SetAnimatorController(avatarPreview.Animator, controller);

            controller.AppendOnAnimatorControllerDirtyCallback(ControllerDitry);

            controllerIsDitry = false;
        }

        if (AnimatorControllerExtension.GetEffectiveAnimatorController(avatarPreview.Animator) != controller)
            AnimatorController.SetAnimatorController(avatarPreview.Animator, controller);
    }

    private void CreateParameters() {
        if (previewedMotion is BlendTree) {
            var blendTree = previewedMotion as BlendTree;

            for (var j = 0; j < blendTree.GetRecursiveBlendParamCount(); j++)
                controller.AddParameter(blendTree.GetRecursiveBlendParam(j), AnimatorControllerParameterType.Float);
        }
    }

    private void ClearStateMachine() {
        if (avatarPreview != null && avatarPreview.Animator != null)
            AnimatorController.SetAnimatorController(avatarPreview.Animator, null);

        if (controller != null) controller.RemoveOnAnimatorControllerDirtyCallback(ControllerDitry);

        DestroyImmediate(controller);
        //Object.DestroyImmediate(this.stateMachine);
        DestroyImmediate(state);
        stateMachine = null;
        controller = null;
        state = null;
    }

    public void ResetStateMachine() {
        ClearStateMachine();
        CreateStateMachine();
    }

    private void ControllerDitry() {
        controllerIsDitry = true;
    }

    private void UpdateAvatarState() {
        if (Event.current.type != EventType.Repaint) return;

        var animator = avatarPreview.Animator;
        if (animator) {
            if (controllerIsDitry) {
                avatarPreview.ResetPreviewInstance();
                ResetStateMachine();
            }

            if (PrevIKOnFeet != avatarPreview.IKOnFeet) {
                PrevIKOnFeet = avatarPreview.IKOnFeet;
                var rootPosition = avatarPreview.Animator.rootPosition;
                var rootRotation = avatarPreview.Animator.rootRotation;
                ResetStateMachine();
                avatarPreview.Animator.UpdateWrapper(avatarPreview.timeControl.currentTime);
                avatarPreview.Animator.UpdateWrapper(0f);
                avatarPreview.Animator.rootPosition = rootPosition;
                avatarPreview.Animator.rootRotation = rootRotation;
            }

//			if (avatarPreview.Animator != null)
//			{
//				BlendTree blendTree = previewedMotion as BlendTree;
//
//				if (blendTree != null)
//				{
//					for (int i = 0; i < blendTree.GetRecursiveBlendParamCount(); i++)
//					{
//						string recurvieBlendParameter = blendTree.GetRecursiveBlendParam(i);
//						float inputBlendValue = blendTree.GetInputBlendVal(recurvieBlendParameter);
//						avatarPreview.Animator.SetFloat(recurvieBlendParameter, inputBlendValue);
//					}
//				}
//			}

            avatarPreview.timeControl.loop = true;

            var num = 1f;
            var num2 = 0f;
            if (animator.layerCount > 0) {
                var currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
                num = currentAnimatorStateInfo.length;
                num2 = currentAnimatorStateInfo.normalizedTime;
            }

            avatarPreview.timeControl.startTime = 0f;
            avatarPreview.timeControl.stopTime = num;
            avatarPreview.timeControl.Update();

            var num3 = avatarPreview.timeControl.deltaTime;
            if (float.IsInfinity(num3))
                num3 = 0;

            if (!previewedMotion.isLooping)
                if (num2 >= 1f) {
                    num3 -= num;
                }
                else {
                    if (num2 < 0f) num3 += num;
                }

            animator.UpdateWrapper(num3);
        }
    }

    private void LoadData() {
        var dataSource = target as MecanimEventData;

        data = new Dictionary<AnimatorController, Dictionary<int, Dictionary<int, List<MecanimEvent>>>>();

        if (dataSource.data == null || dataSource.data.Length == 0)
            return;

        foreach (var entry in dataSource.data) {
            var animatorController = entry.animatorController as AnimatorController;

            if (animatorController == null)
                return;

            if (!data.ContainsKey(animatorController))
                data[animatorController] = new Dictionary<int, Dictionary<int, List<MecanimEvent>>>();

            if (!data[animatorController].ContainsKey(entry.layer))
                data[animatorController][entry.layer] = new Dictionary<int, List<MecanimEvent>>();

            var events = new List<MecanimEvent>();

            if (entry.events != null)
                foreach (var e in entry.events)
                    events.Add(new MecanimEvent(e));

            data[animatorController][entry.layer][entry.stateNameHash] = events;
        }
    }

    public void SaveData() {
        var targetData = target as MecanimEventData;
        Undo.RecordObject(target, "Mecanim Event Data");

        var entries = new List<MecanimEventDataEntry>();

        foreach (var controller in data.Keys)
        foreach (var layer in data[controller].Keys)
        foreach (var stateNameHash in data[controller][layer].Keys) {
            if (data[controller][layer][stateNameHash].Count == 0)
                continue;

            if (!IsValidState(controller.GetInstanceID(), layer, stateNameHash)) continue;

            var entry = new MecanimEventDataEntry();
            entry.animatorController = controller;
            entry.layer = layer;
            entry.stateNameHash = stateNameHash;
            entry.events = data[controller][layer][stateNameHash].ToArray();
            ;

            entries.Add(entry);
        }

        targetData.data = entries.ToArray();

        EditorUtility.SetDirty(target);
    }

    public void SaveLastEditController(Object controller) {
        serializedObject.FindProperty("lastEdit").objectReferenceValue = controller;
        ;
    }

    private bool IsValidState(int controllerId, int layer, int stateNameHash) {
        if (!IsValidControllerId(controllerId))
            return false;

        if (!IsValidLayer(controllerId, layer))
            return false;

        var controller = EditorUtility.InstanceIDToObject(controllerId) as AnimatorController;
        AnimatorStateMachine sm;
        if (controller.layers[layer].syncedLayerIndex != -1)
            sm = controller.layers[controller.layers[layer].syncedLayerIndex].stateMachine;
        else
            sm = controller.layers[layer].stateMachine;

        return FindState(sm, stateNameHash);
    }


    private bool IsValidControllerId(int controllerId) {
        var controller = EditorUtility.InstanceIDToObject(controllerId) as AnimatorController;

        if (controller == null)
            return false;

        return true;
    }

    private bool IsValidLayer(int controllerId, int layer) {
        var controller = EditorUtility.InstanceIDToObject(controllerId) as AnimatorController;

        if (controller == null)
            return false;

        if (layer >= 0 && layer < controller.layers.Length)
            return true;
        return false;
    }

    private bool FindState(AnimatorStateMachine baseSm, int namehash) {
        return FindStateRecursively(baseSm, baseSm, namehash);
    }

    private bool FindStateRecursively(AnimatorStateMachine baseSm, AnimatorStateMachine stateMachine, int nameHash) {
        foreach (var childState in stateMachine.states)
            if (childState.state.GetFullPathHash(baseSm) == nameHash)
                return true;

        foreach (var childStateMachine in stateMachine.stateMachines)
            if (FindStateRecursively(baseSm, childStateMachine.stateMachine, nameHash))
                return true;

        return false;
    }
}