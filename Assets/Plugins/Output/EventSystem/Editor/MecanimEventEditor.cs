using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class MecanimEventEditor : EditorWindow {
    public static MecanimEventInspector eventInspector;

    public static MecanimEvent clipboard;
    public static MecanimEvent[] stateClipboard;
    public static Dictionary<int, Dictionary<int, MecanimEvent[]>> controllerClipboard;

    private static readonly int timelineHash = "timelinecontrol".GetHashCode();

    private Vector2 controllerPanelScrollPos;
    private AnimatorController controllerToAdd;

    private List<MecanimEvent> displayEvents;

    private bool enableTempPreview;

    private Vector2 eventPanelScrollPos;

    private int hotEventKey;

    private Vector2 layerPanelScrollPos;

    private AnimatorControllerLayer[] layers;

    private int selectedController;
    private int selectedEvent;
    private int selectedLayer;
    private int selectedState;

    private Vector2 statePanelScrollPos;

    private AnimatorController targetController;
    private MecanimEvent targetEvent;
    private AnimatorState targetState;
    private AnimatorStateMachine targetStateMachine;
    private float tempPreviewPlaybackTime;

    public MecanimEventEditor() {
        PlaybackTime = 0.0f;
    }

    public float PlaybackTime { get; private set; }

    public Object TargetController {
        set { targetController = value as AnimatorController; }
    }

    private static void Init() {
        GetWindow<MecanimEventEditor>();
    }

    private void OnEnable() {
        minSize = new Vector2(850, 320);
    }

    private void OnDisable() {
        MecanimEventEditorPopup.Destroy();

        if (eventInspector != null) {
            eventInspector.SetPreviewMotion(null);
            eventInspector.SaveData();
        }
    }

    private void OnInspectorUpdate() {
        Repaint();
    }

    public void DelEvent(MecanimEvent e) {
        if (displayEvents != null) {
            displayEvents.Remove(e);
            SaveState();
        }
    }

    private void SortEvents() {
        if (displayEvents != null)
            displayEvents.Sort(
                delegate(MecanimEvent a, MecanimEvent b) { return a.normalizedTime.CompareTo(b.normalizedTime); }
            );
    }

    private void Reset() {
        displayEvents = null;

        targetController = null;
        targetStateMachine = null;
        targetState = null;
        targetEvent = null;

        selectedLayer = 0;
        selectedState = 0;
        selectedEvent = 0;

        MecanimEventEditorPopup.Destroy();
    }

    public KeyValuePair<string, EventConditionParamTypes>[] GetConditionParameters() {
        var ret = new List<KeyValuePair<string, EventConditionParamTypes>>();
        if (targetController != null)
            foreach (var animatorParam in targetController.parameters)
                switch (animatorParam.type) {
                    case AnimatorControllerParameterType.Float: // float
                        ret.Add(new KeyValuePair<string, EventConditionParamTypes>(
                                    animatorParam.name, EventConditionParamTypes.Float));
                        break;
                    case AnimatorControllerParameterType.Int: // int
                        ret.Add(new KeyValuePair<string, EventConditionParamTypes>(
                                    animatorParam.name, EventConditionParamTypes.Int));
                        break;
                    case AnimatorControllerParameterType.Bool: // bool
                        ret.Add(new KeyValuePair<string, EventConditionParamTypes>(
                                    animatorParam.name, EventConditionParamTypes.Boolean));
                        break;
                }

        return ret.ToArray();
    }

    private void SaveState() {
        if (targetController != null && targetState != null)
            eventInspector.SetEvents(targetController, selectedLayer, targetState.GetFullPathHash(targetStateMachine),
                                     displayEvents.ToArray());
    }

    private void DrawControllerPanel() {
        GUILayout.BeginVertical(GUILayout.Width(200));

        // controller to add field.
        GUILayout.BeginHorizontal();
        {
            controllerToAdd =
                EditorGUILayout.ObjectField(controllerToAdd, typeof(AnimatorController), false) as AnimatorController;

            EditorGUI.BeginDisabledGroup(controllerToAdd == null);

            if (GUILayout.Button("Add", GUILayout.ExpandWidth(true), GUILayout.Height(16)))
                eventInspector.AddController(controllerToAdd);

            EditorGUI.EndDisabledGroup();

            //GUILayout.Button("Del", EditorStyles.toolbarButton, GUILayout.Width(38), GUILayout.Height(16));

            GUILayout.Space(4);
        }
        GUILayout.EndHorizontal();

        // controller list

        GUILayout.BeginVertical("Box");
        controllerPanelScrollPos = GUILayout.BeginScrollView(controllerPanelScrollPos);

        var controllers = eventInspector.GetControllers();

        var controllerNames = new string[controllers.Length];

        for (var i = 0; i < controllers.Length; i++) controllerNames[i] = controllers[i].name;

        selectedController = GUILayout.SelectionGrid(selectedController, controllerNames, 1);

        if (selectedController >= 0 && selectedController < controllers.Length) {
            targetController = controllers[selectedController];

            eventInspector.SaveLastEditController(targetController);
        }
        else {
            targetController = null;
            targetStateMachine = null;
            targetState = null;
            targetEvent = null;
        }


        GUILayout.EndScrollView();
        GUILayout.EndVertical();


        GUILayout.EndVertical();
    }

    private void DrawLayerPanel() {
        GUILayout.BeginVertical(GUILayout.Width(200));

        if (targetController != null) {
            var layerCount = targetController.layers.Length;
            GUILayout.Label(layerCount + " layer(s) in selected controller");

            if (Event.current.type == EventType.Layout || layers == null) layers = targetController.layers;

            GUILayout.BeginVertical("Box");
            layerPanelScrollPos = GUILayout.BeginScrollView(layerPanelScrollPos);

            var layerNames = new string[layerCount];

            for (var layer = 0; layer < layerCount; layer++) layerNames[layer] = "[" + layer + "]" + layers[layer].name;

            selectedLayer = GUILayout.SelectionGrid(selectedLayer, layerNames, 1);

            if (selectedLayer >= 0 && selectedLayer < layerCount) {
                if (layers[selectedLayer].syncedLayerIndex != -1)
                    targetStateMachine = layers[layers[selectedLayer].syncedLayerIndex].stateMachine;
                else
                    targetStateMachine = layers[selectedLayer].stateMachine;
            }
            else {
                targetStateMachine = null;
                targetState = null;
                targetEvent = null;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        else {
            GUILayout.Label("No layer available.");
        }

        GUILayout.EndVertical();
    }

    private void DrawStatePanel() {
        GUILayout.BeginVertical(GUILayout.Width(200));

        if (targetStateMachine != null) {
            var availableStates = GetStatesRecursive(targetStateMachine);
            var stateNames = new List<string>();

            foreach (var s in availableStates) stateNames.Add(s.name);

            GUILayout.Label(availableStates.Count + " state(s) in selected layer.");

            GUILayout.BeginVertical("Box");
            statePanelScrollPos = GUILayout.BeginScrollView(statePanelScrollPos);

            selectedState = GUILayout.SelectionGrid(selectedState, stateNames.ToArray(), 1);

            if (selectedState >= 0 && selectedState < availableStates.Count) {
                targetState = availableStates[selectedState];
            }
            else {
                targetState = null;
                targetEvent = null;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        else {
            GUILayout.Label("No state machine available.");
        }

        GUILayout.EndVertical();
    }

    private void DrawEventPanel() {
        GUILayout.BeginVertical();

        if (targetState != null) {
            displayEvents =
                new List<MecanimEvent>(eventInspector.GetEvents(targetController, selectedLayer,
                                                                targetState.GetFullPathHash(targetStateMachine)));
            SortEvents();

            GUILayout.Label(displayEvents.Count + " event(s) in this state.");

            var eventNames = new List<string>();

            foreach (var e in displayEvents)
                eventNames.Add(string.Format("{3}{0}({1})@{2}", e.functionName, e.parameter,
                                             e.normalizedTime.ToString("0.0000"), e.isEnable ? "" : "[DISABLED]"));

            GUILayout.BeginVertical("Box");
            eventPanelScrollPos = GUILayout.BeginScrollView(eventPanelScrollPos);

            selectedEvent = GUILayout.SelectionGrid(selectedEvent, eventNames.ToArray(), 1);

            if (selectedEvent >= 0 && selectedEvent < displayEvents.Count)
                targetEvent = displayEvents[selectedEvent];
            else
                targetEvent = null;

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        else {
            GUILayout.Label("No event.");
        }

        GUILayout.EndVertical();
    }

    private void DrawTimelinePanel() {
        if (!enableTempPreview)
            PlaybackTime = eventInspector.GetPlaybackTime();


        GUILayout.BeginVertical();
        {
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(20);

                PlaybackTime = Timeline(PlaybackTime);

                GUILayout.Space(10);
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Tools")) {
                    var menu = new GenericMenu();

                    GenericMenu.MenuFunction2 callback = delegate(object obj) {
                        var id = (int) obj;

                        switch (id) {
                            case 1: {
                                stateClipboard = eventInspector.GetEvents(
                                    targetController, selectedLayer, targetState.GetFullPathHash(targetStateMachine));
                                break;
                            }

                            case 2: {
                                eventInspector.InsertEventsCopy(targetController, selectedLayer,
                                                                targetState.GetFullPathHash(targetStateMachine),
                                                                stateClipboard);
                                break;
                            }

                            case 3: {
                                controllerClipboard = eventInspector.GetEvents(targetController);
                                break;
                            }

                            case 4: {
                                eventInspector.InsertControllerEventsCopy(targetController, controllerClipboard);
                                break;
                            }
                        }
                    };

                    if (targetState == null)
                        menu.AddDisabledItem(new GUIContent("Copy All Events From Selected State"));
                    else
                        menu.AddItem(new GUIContent("Copy All Events From Selected State"), false, callback, 1);

                    if (targetState == null || stateClipboard == null || stateClipboard.Length == 0)
                        menu.AddDisabledItem(new GUIContent("Paste All Events To Selected State"));
                    else
                        menu.AddItem(new GUIContent("Paste All Events To Selected State"), false, callback, 2);

                    if (targetController == null)
                        menu.AddDisabledItem(new GUIContent("Copy All Events From Selected Controller"));
                    else
                        menu.AddItem(new GUIContent("Copy All Events From Selected Controller"), false, callback, 3);

                    if (targetController == null || controllerClipboard == null || controllerClipboard.Count == 0)
                        menu.AddDisabledItem(new GUIContent("Paste All Events To Selected Controller"));
                    else
                        menu.AddItem(new GUIContent("Paste All Events To Selected Controller"), false, callback, 4);


                    menu.ShowAsContext();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Add", GUILayout.Width(80))) {
                    var newEvent = new MecanimEvent();
                    newEvent.normalizedTime = PlaybackTime;
                    newEvent.functionName = "MessageName";
                    newEvent.paramType = MecanimEventParamTypes.None;

                    displayEvents.Add(newEvent);
                    SortEvents();

                    SetActiveEvent(newEvent);

                    MecanimEventEditorPopup.Show(this, newEvent, GetConditionParameters());
                }

                if (GUILayout.Button("Del", GUILayout.Width(80))) DelEvent(targetEvent);

                EditorGUI.BeginDisabledGroup(targetEvent == null);

                if (GUILayout.Button("Copy", GUILayout.Width(80))) clipboard = new MecanimEvent(targetEvent);

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(clipboard == null);

                if (GUILayout.Button("Paste", GUILayout.Width(80))) {
                    var newEvent = new MecanimEvent(clipboard);
                    displayEvents.Add(newEvent);
                    SortEvents();

                    SetActiveEvent(newEvent);
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(targetEvent == null);

                if (GUILayout.Button("Edit", GUILayout.Width(80)))
                    MecanimEventEditorPopup.Show(this, targetEvent, GetConditionParameters());

                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Save", GUILayout.Width(80))) eventInspector.SaveData();

                if (GUILayout.Button("Close", GUILayout.Width(80))) Close();
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        if (enableTempPreview) {
            eventInspector.SetPlaybackTime(tempPreviewPlaybackTime);
            eventInspector.StopPlaying();
        }
        else {
            eventInspector.SetPlaybackTime(PlaybackTime);
        }

        SaveState();
    }

    private void OnGUI() {
        if (eventInspector == null) {
            Reset();
            ShowNotification(new GUIContent("Select a MecanimEventData object first."));
            return;
        }

        RemoveNotification();

        GUILayout.BeginHorizontal();
        {
            EditorGUI.BeginChangeCheck();

            DrawControllerPanel();

            DrawLayerPanel();

            DrawStatePanel();

            if (EditorGUI.EndChangeCheck()) MecanimEventEditorPopup.Destroy();

            DrawEventPanel();
        }
        GUILayout.EndHorizontal();

        if (targetState != null && targetState.motion != null)
            eventInspector.SetPreviewMotion(targetState.motion);
        else
            eventInspector.SetPreviewMotion(null);

        GUILayout.Space(5);

        GUILayout.BeginHorizontal(GUILayout.MaxHeight(100));
        {
            DrawTimelinePanel();
        }
        GUILayout.EndHorizontal();
    }

    private float Timeline(float time) {
        var rect = GUILayoutUtility.GetRect(500, 10000, 50, 50);

        var timelineId = GUIUtility.GetControlID(timelineHash, FocusType.Passive, rect);

        var thumbRect = new Rect(rect.x + rect.width * time - 5, rect.y + 2, 10, 10);

        var e = Event.current;

        switch (e.type) {
            case EventType.Repaint:
                var lineRect = new Rect(rect.x, rect.y + 10, rect.width, 1.5f);
                DrawTimeLine(lineRect, time);
                GUI.skin.horizontalSliderThumb.Draw(thumbRect, new GUIContent(), timelineId);
                break;

            case EventType.MouseDown:
                if (thumbRect.Contains(e.mousePosition)) {
                    GUIUtility.hotControl = timelineId;
                    e.Use();
                }

                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == timelineId) {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }

                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == timelineId) {
                    var guiPos = e.mousePosition;
                    var clampedX = Mathf.Clamp(guiPos.x, rect.x, rect.x + rect.width);
                    time = (clampedX - rect.x) / rect.width;

                    e.Use();
                }

                break;
        }

        if (displayEvents != null) {
            foreach (var me in displayEvents) {
                if (me == targetEvent)
                    continue;

                DrawEventKey(rect, me);
            }

            if (targetEvent != null)
                DrawEventKey(rect, targetEvent);
        }

        return time;
    }

    private void DrawTimeLine(Rect rect, float currentFrame) {
        if (Event.current.type != EventType.Repaint) return;

        HandleUtilityWrapper.handleWireMaterial.SetPass(0);
        var c = new Color(1f, 0f, 0f, 0.75f);
        GL.Color(c);

        GL.Begin(GL.LINES);
        GL.Vertex3(rect.x, rect.y, 0);
        GL.Vertex3(rect.x + rect.width, rect.y, 0);

        GL.Vertex3(rect.x, rect.y + 25, 0);
        GL.Vertex3(rect.x + rect.width, rect.y + 25, 0);


        for (var i = 0; i <= 100; i += 1)
            if (i % 10 == 0) {
                GL.Vertex3(rect.x + rect.width * i / 100f, rect.y, 0);
                GL.Vertex3(rect.x + rect.width * i / 100f, rect.y + 15, 0);
            }
            else if (i % 5 == 0) {
                GL.Vertex3(rect.x + rect.width * i / 100f, rect.y, 0);
                GL.Vertex3(rect.x + rect.width * i / 100f, rect.y + 10, 0);
            }
            else {
                GL.Vertex3(rect.x + rect.width * i / 100f, rect.y, 0);
                GL.Vertex3(rect.x + rect.width * i / 100f, rect.y + 5, 0);
            }

        c = new Color(1.0f, 1.0f, 1.0f, 0.75f);
        GL.Color(c);

        GL.Vertex3(rect.x + rect.width * currentFrame, rect.y, 0);
        GL.Vertex3(rect.x + rect.width * currentFrame, rect.y + 20, 0);

        GL.End();
    }

    private void SetActiveEvent(MecanimEvent key) {
        var i = displayEvents.IndexOf(key);
        if (i >= 0) {
            selectedEvent = i;
            targetEvent = key;
        }
    }

    private void DrawEventKey(Rect rect, MecanimEvent key) {
        var keyTime = key.normalizedTime;

        var keyRect = new Rect(rect.x + rect.width * keyTime - 3, rect.y + 25, 6, 18);

        var eventKeyCtrl = key.GetHashCode();

        var e = Event.current;

        switch (e.type) {
            case EventType.Repaint:
                var savedColor = GUI.color;

                if (targetEvent == key)
                    GUI.color = Color.red;
                else
                    GUI.color = Color.green;

                GUI.skin.button.Draw(keyRect, new GUIContent(), eventKeyCtrl);

                GUI.color = savedColor;

                if (hotEventKey == eventKeyCtrl || hotEventKey == 0 && keyRect.Contains(e.mousePosition)) {
                    var labelString = string.Format("{0}({1})@{2}", key.functionName, key.parameter,
                                                    key.normalizedTime.ToString("0.0000"));
                    var size = EditorStyles.largeLabel.CalcSize(new GUIContent(labelString));

                    var infoRect = new Rect(rect.x + rect.width * keyTime - size.x / 2, rect.y + 50, size.x, size.y);
                    EditorStyles.largeLabel.Draw(infoRect, new GUIContent(labelString), eventKeyCtrl);
                }

                break;

            case EventType.MouseDown:
                if (keyRect.Contains(e.mousePosition)) {
                    hotEventKey = eventKeyCtrl;
                    enableTempPreview = true;
                    tempPreviewPlaybackTime = key.normalizedTime;

                    SetActiveEvent(key);

                    if (e.clickCount > 1)
                        MecanimEventEditorPopup.Show(this, key, GetConditionParameters());

                    e.Use();
                }

                break;

            case EventType.MouseDrag:
                if (hotEventKey == eventKeyCtrl) {
                    if (e.button == 0) {
                        var guiPos = e.mousePosition;
                        var clampedX = Mathf.Clamp(guiPos.x, rect.x, rect.x + rect.width);
                        key.normalizedTime = (clampedX - rect.x) / rect.width;
                        tempPreviewPlaybackTime = key.normalizedTime;

                        SetActiveEvent(key);
                    }

                    e.Use();
                }

                break;

            case EventType.MouseUp:
                if (hotEventKey == eventKeyCtrl) {
                    hotEventKey = 0;
                    enableTempPreview = false;
                    eventInspector.SetPlaybackTime(PlaybackTime); // reset to original time

                    if (e.button == 1)
                        MecanimEventEditorPopup.Show(this, key, GetConditionParameters());

                    e.Use();
                }

                break;
        }
    }

    private List<AnimatorState> GetStates(AnimatorStateMachine sm) {
        var stateArray = new List<AnimatorState>();
        foreach (var childState in sm.states) stateArray.Add(childState.state);

        return stateArray;
    }

    private List<AnimatorState> GetStatesRecursive(AnimatorStateMachine sm) {
        var list = new List<AnimatorState>();
        list.AddRange(GetStates(sm));

        foreach (var childStateMachine in sm.stateMachines)
            list.AddRange(GetStatesRecursive(childStateMachine.stateMachine));

        return list;
    }
}