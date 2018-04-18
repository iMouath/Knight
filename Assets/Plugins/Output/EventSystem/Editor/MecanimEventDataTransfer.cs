using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MecanimEventDataTransfer : EditorWindow {
    private Vector2 leftWindowScroll;
    private Vector2 rightWindowScroll;

    private Dictionary<Object, bool> toggleTable = new Dictionary<Object, bool>();

    private MecanimEventData transferFrom;
    private MecanimEventData transferTo;

    [MenuItem("Window/Mecanim Event Data Transfer")]
    public static void Init() {
        GetWindow<MecanimEventDataTransfer>();
    }

    private void OnGUI() {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUI.BeginChangeCheck();

            transferFrom =
                EditorGUILayout.ObjectField("Transfer From", transferFrom, typeof(MecanimEventData), false) as
                    MecanimEventData;

            if (EditorGUI.EndChangeCheck()) toggleTable = new Dictionary<Object, bool>();

            transferTo =
                EditorGUILayout.ObjectField("Transfer To", transferTo, typeof(MecanimEventData), false) as
                    MecanimEventData;
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical("Window", GUILayout.MaxWidth(position.width / 2));
            leftWindowScroll = EditorGUILayout.BeginScrollView(leftWindowScroll);
            {
                DisplayDataSource();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Window", GUILayout.MaxWidth(position.width / 2));
            rightWindowScroll = EditorGUILayout.BeginScrollView(rightWindowScroll);
            {
                DisplayDataDest();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(transferFrom == null || transferTo == null ||
                                         transferFrom.GetInstanceID() == transferTo.GetInstanceID());

            if (GUILayout.Button("Transfer", GUILayout.Width(80), GUILayout.Height(30))) Transfer();

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(30))) Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DisplayDataSource() {
        if (transferFrom == null)
            return;

        var data = transferFrom.data;

        if (data == null)
            return;

        var controllers = new Dictionary<Object, DisplayInfo>();

        foreach (var entry in data) {
            if (entry == null || entry.animatorController == null)
                continue;

            if (!controllers.ContainsKey(entry.animatorController))
                controllers[entry.animatorController] = new DisplayInfo();

            if (entry.events != null)
                controllers[entry.animatorController].eventCount += entry.events.Length;
        }

        EditorGUILayout.BeginVertical();


        foreach (var controller in controllers.Keys) {
            if (!toggleTable.ContainsKey(controller)) toggleTable[controller] = false;

            EditorGUILayout.BeginHorizontal();

            toggleTable[controller] =
                GUILayout.Toggle(toggleTable[controller], controller.name, GUILayout.ExpandWidth(true));

            GUILayout.Label("(" + controllers[controller].eventCount + ")", GUILayout.ExpandWidth(false));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DisplayDataDest() {
        if (transferTo == null)
            return;

        var data = transferTo.data;

        if (data == null)
            return;

        var controllers = new Dictionary<Object, DisplayInfo>();

        foreach (var entry in data) {
            if (entry == null || entry.animatorController == null)
                continue;

            if (!controllers.ContainsKey(entry.animatorController))
                controllers[entry.animatorController] = new DisplayInfo();

            if (entry.events != null)
                controllers[entry.animatorController].eventCount += entry.events.Length;
        }

        EditorGUILayout.BeginVertical();


        foreach (var controller in controllers.Keys) {
            if (!toggleTable.ContainsKey(controller)) toggleTable[controller] = false;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                DeleteEntry(controller);

            GUILayout.Label(controller.name, GUILayout.ExpandWidth(true));

            GUILayout.Label("(" + controllers[controller].eventCount + ")", GUILayout.ExpandWidth(false));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void Transfer() {
        if (transferFrom.data == null || transferFrom.data.Length == 0)
            return;

        var toggleAny = false;
        foreach (var f in toggleTable.Values)
            if (f) {
                toggleAny = true;
                break;
            }

        if (!toggleAny)
            return;


        if (transferTo.data == null)
            transferTo.data = new MecanimEventDataEntry[0];

        var destEntries = new List<MecanimEventDataEntry>(transferTo.data);

        for (var i = 0; i < destEntries.Count;)
            if (toggleTable.ContainsKey(destEntries[i].animatorController) &&
                toggleTable[destEntries[i].animatorController])
                destEntries.RemoveAt(i);
            else
                i++;

        foreach (var srcEntry in transferFrom.data)
            if (toggleTable.ContainsKey(srcEntry.animatorController) &&
                toggleTable[srcEntry.animatorController])
                destEntries.Add(new MecanimEventDataEntry(srcEntry));

        transferTo.data = destEntries.ToArray();

        EditorUtility.SetDirty(transferTo);

        Repaint();
    }

    private void DeleteEntry(Object controller) {
        Undo.RecordObject(transferTo, "Mecanim Event Data");

        if (transferTo.data == null)
            transferTo.data = new MecanimEventDataEntry[0];

        var destEntries = new List<MecanimEventDataEntry>(transferTo.data);

        for (var i = 0; i < destEntries.Count;)
            if (destEntries[i] != null && destEntries[i].animatorController == controller)
                destEntries.RemoveAt(i);
            else
                i++;

        transferTo.data = destEntries.ToArray();

        EditorUtility.SetDirty(transferTo);
        Repaint();
    }

    private class DisplayInfo {
        public int eventCount;
    }
}