using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
//using UnityEditorInternal;

[CustomEditor(typeof(MecanimEventEmitterWithData))]
public class MecanimEventEmitterWithDataInspector : Editor {
    private SerializedProperty animator;
    private SerializedProperty controller;
    private SerializedProperty data;
    private SerializedProperty emitType;

    private void OnEnable() {
        controller = serializedObject.FindProperty("animatorController");
        animator = serializedObject.FindProperty("animator");
        emitType = serializedObject.FindProperty("emitType");
        data = serializedObject.FindProperty("data");
    }

    public override void OnInspectorGUI() {
        serializedObject.UpdateIfDirtyOrScript();

        EditorGUILayout.PropertyField(animator);

        if (animator.objectReferenceValue != null) {
            var animatorController =
                AnimatorControllerExtension.GetEffectiveAnimatorController((Animator) animator.objectReferenceValue);
            controller.objectReferenceValue = animatorController;
        }
        else {
            controller.objectReferenceValue = null;
        }

        EditorGUILayout.ObjectField("AnimatorController", controller.objectReferenceValue, typeof(AnimatorController),
                                    false);

        EditorGUILayout.PropertyField(emitType);

        EditorGUILayout.PropertyField(data);

        serializedObject.ApplyModifiedProperties();
    }
}