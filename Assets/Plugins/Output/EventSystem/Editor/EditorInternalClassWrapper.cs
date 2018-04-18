using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AvatarPreviewWrapper {
    #region Reflection

    private static Type realType;

    private static ConstructorInfo method_ctor;
    private static PropertyInfo property_OnAvatarChangeFunc;
    private static PropertyInfo property_IKOnFeet;
    private static PropertyInfo property_Animator;
    private static MethodInfo method_DoPreviewSettings;
    private static MethodInfo method_OnDestroy;
    private static MethodInfo method_DoAvatarPreview;
    private static MethodInfo method_ResetPreviewInstance;

//	private static MethodInfo method_CalculatePreviewGameObject;
    private static FieldInfo field_timeControl;


    public static void InitType() {
        if (realType == null) {
            var assembly = Assembly.GetAssembly(typeof(Editor));
            realType = assembly.GetType("UnityEditor.AvatarPreview");

            method_ctor = realType.GetConstructor(new[] {typeof(Animator), typeof(Motion)});
            property_OnAvatarChangeFunc = realType.GetProperty("OnAvatarChangeFunc");
            property_IKOnFeet = realType.GetProperty("IKOnFeet");
            property_Animator = realType.GetProperty("Animator");
            method_DoPreviewSettings = realType.GetMethod("DoPreviewSettings");
            method_OnDestroy = realType.GetMethod("OnDestroy");
            method_DoAvatarPreview = realType.GetMethod("DoAvatarPreview", new[] {typeof(Rect), typeof(GUIStyle)});
            method_ResetPreviewInstance = realType.GetMethod("ResetPreviewInstance");
//			method_CalculatePreviewGameObject = realType.GetMethod("CalculatePreviewGameObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            field_timeControl = realType.GetField("timeControl");
        }
    }

    #endregion

    #region Wrapper

    private readonly object instance;

    public delegate void OnAvatarChange();

    public AvatarPreviewWrapper(Animator previewObjectInScene, Motion objectOnSameAsset) {
        InitType();

        instance = method_ctor.Invoke(new object[] {previewObjectInScene, objectOnSameAsset});
    }

    public Animator Animator {
        get { return property_Animator.GetValue(instance, null) as Animator; }
    }

    public bool IKOnFeet {
        get { return (bool) property_IKOnFeet.GetValue(instance, null); }
    }

    public OnAvatarChange OnAvatarChangeFunc {
        set {
            property_OnAvatarChangeFunc.SetValue(
                instance, Delegate.CreateDelegate(property_OnAvatarChangeFunc.PropertyType, value.Target, value.Method),
                null);
        }
    }

    public void DoPreviewSettings() {
        method_DoPreviewSettings.Invoke(instance, null);
    }

    public void OnDestroy() {
        method_OnDestroy.Invoke(instance, null);
    }

    public void DoAvatarPreview(Rect rect, GUIStyle background) {
        method_DoAvatarPreview.Invoke(instance, new object[] {rect, background});
    }

    public void ResetPreviewInstance() {
        method_ResetPreviewInstance.Invoke(instance, null);
    }

//	public static GameObject CalculatePreviewGameobject (Animator selectedAnimator, Motion motion, ModelImporterAnimationType animationType) {
//		InitType();
//		return (GameObject)method_CalculatePreviewGameObject.Invoke(null, new object[] { selectedAnimator, motion, animationType });
//	}

    public TimeControlWrapper timeControl {
        get { return new TimeControlWrapper(field_timeControl.GetValue(instance)); }
    }

    #endregion
}

public class TimeControlWrapper {
    private static Type realType;

    private static FieldInfo field_currentTime;
    private static FieldInfo field_loop;
    private static FieldInfo field_startTime;
    private static FieldInfo field_stopTime;
    private static MethodInfo method_Update;
    private static PropertyInfo property_deltaTime;
    private static PropertyInfo property_normalizedTime;
    private static PropertyInfo property_playing;
    private static PropertyInfo property_nextCurrentTime;
    private readonly object instance;

    public TimeControlWrapper(object realTimeControl) {
        InitType();
        instance = realTimeControl;
    }

    public float currentTime {
        get { return (float) field_currentTime.GetValue(instance); }
        set { field_currentTime.SetValue(instance, value); }
    }

    public bool loop {
        get { return (bool) field_loop.GetValue(instance); }
        set { field_loop.SetValue(instance, value); }
    }

    public float startTime {
        get { return (float) field_startTime.GetValue(instance); }
        set { field_startTime.SetValue(instance, value); }
    }

    public float stopTime {
        get { return (float) field_stopTime.GetValue(instance); }
        set { field_stopTime.SetValue(instance, value); }
    }

    public float deltaTime {
        get { return (float) property_deltaTime.GetValue(instance, null); }
        set { property_deltaTime.SetValue(instance, value, null); }
    }

    public float normalizedTime {
        get { return (float) property_normalizedTime.GetValue(instance, null); }
        set { property_normalizedTime.SetValue(instance, value, null); }
    }

    public bool playing {
        get { return (bool) property_playing.GetValue(instance, null); }
        set { property_playing.SetValue(instance, value, null); }
    }

    public float nextCurrentTime {
        set { property_nextCurrentTime.SetValue(instance, value, null); }
    }

    public static void InitType() {
        if (realType == null) {
            var assembly = Assembly.GetAssembly(typeof(Editor));
            realType = assembly.GetType("UnityEditor.TimeControl");

            field_currentTime = realType.GetField("currentTime");
            field_loop = realType.GetField("loop");
            field_startTime = realType.GetField("startTime");
            field_stopTime = realType.GetField("stopTime");
            method_Update = realType.GetMethod("Update");
            property_deltaTime = realType.GetProperty("deltaTime");
            property_normalizedTime = realType.GetProperty("normalizedTime");
            property_playing = realType.GetProperty("playing");
            property_nextCurrentTime = realType.GetProperty("nextCurrentTime");
        }
    }

    public void Update() {
        method_Update.Invoke(instance, null);
    }
}

public static class HandleUtilityWrapper {
    private static Type realType;
    private static PropertyInfo s_property_handleWireMaterial;

    public static Material handleWireMaterial {
        get {
            InitType();
            return s_property_handleWireMaterial.GetValue(null, null) as Material;
        }
    }

    private static void InitType() {
        if (realType == null) {
            var assembly = Assembly.GetAssembly(typeof(Editor));
            realType = assembly.GetType("UnityEditor.HandleUtility");

            s_property_handleWireMaterial = realType.GetProperty("handleWireMaterial",
                                                                 BindingFlags.Static | BindingFlags.NonPublic |
                                                                 BindingFlags.Public);
        }
    }
}

public static class AnimatorExtension {
    private static Type realType;

    private static MethodInfo method_Update;

    public static void InitType() {
        if (realType == null) {
            realType = typeof(Animator);

            method_Update =
                realType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }
    }


    public static void UpdateWrapper(this Animator animator, float diff) {
        InitType();

        method_Update.Invoke(animator, new object[] {diff});
    }
}

public static class BlendTreeExtension {
    public static int GetRecursiveBlendParamCount(this BlendTree bt) {
        var val = bt.GetType()
            .GetProperty("recursiveBlendParameterCount",
                         BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic |
                         BindingFlags.Public).GetValue(bt, new object[] { });
        return (int) val;
    }

    public static string GetRecursiveBlendParam(this BlendTree bt, int index) {
        var val = bt.GetType()
            .GetMethod("GetRecursiveBlendParameter",
                       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(bt, new object[] {index});
        return (string) val;
    }

    public static float GetRecursiveBlendParamMax(this BlendTree bt, int index) {
        var val = bt.GetType()
            .GetMethod("GetRecursiveBlendParameterMax",
                       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(bt, new object[] {index});
        return (float) val;
    }

    public static float GetRecursiveBlendParamMin(this BlendTree bt, int index) {
        var val = bt.GetType()
            .GetMethod("GetRecursiveBlendParameterMin",
                       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(bt, new object[] {index});
        return (float) val;
    }

    public static float GetInputBlendVal(this BlendTree bt, string blendValueName) {
        var val = bt.GetType()
            .GetMethod("GetInputBlendValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(bt, new object[] {blendValueName});
        return (float) val;
    }
}

public static class AnimatorControllerExtension {
    private static Type realType;
    private static MethodInfo method_GetEffectiveAnimatorController;
    private static FieldInfo field_OnAnimatorControllerDirty;

    public static void InitType() {
        if (realType == null) {
            realType = typeof(AnimatorController);

            method_GetEffectiveAnimatorController = realType.GetMethod("GetEffectiveAnimatorController",
                                                                       BindingFlags.Static | BindingFlags.NonPublic |
                                                                       BindingFlags.Public);
            field_OnAnimatorControllerDirty = realType.GetField("OnAnimatorControllerDirty",
                                                                BindingFlags.Instance | BindingFlags.NonPublic |
                                                                BindingFlags.Public);
        }
    }

    public static AnimatorController GetEffectiveAnimatorController(Animator animator) {
        InitType();
        object val = (AnimatorController) method_GetEffectiveAnimatorController.Invoke(null, new object[] {animator});
        return (AnimatorController) val;
    }

    public static void AppendOnAnimatorControllerDirtyCallback(this AnimatorController controller, Action callback) {
        InitType();
        var oldCallback = (Action) field_OnAnimatorControllerDirty.GetValue(controller);
        var newCallback = (Action) Delegate.Combine(oldCallback, new Action(callback));

        field_OnAnimatorControllerDirty.SetValue(controller, newCallback);
    }

    public static void RemoveOnAnimatorControllerDirtyCallback(this AnimatorController controller, Action callback) {
        InitType();
        var oldCallback = (Action) field_OnAnimatorControllerDirty.GetValue(controller);
        var newCallback = (Action) Delegate.Remove(oldCallback, new Action(callback));

        field_OnAnimatorControllerDirty.SetValue(controller, newCallback);
    }
}

public static class AnimatorStateExtension {
    public static int GetFullPathHash(this AnimatorState state, AnimatorStateMachine parentSM) {
        var pathElems = new List<string>();

        if (GetFullPathRecursively(parentSM, state, pathElems)) {
            var fullPath = string.Join(".", pathElems.ToArray());
            return Animator.StringToHash(fullPath);
        }

        Debug.LogError("Do not find state path");
        return -1;
    }

    private static bool GetFullPathRecursively(AnimatorStateMachine parentSM, AnimatorState state,
                                               List<string> pathElems) {
        for (var i = 0; i < parentSM.states.Length; i++)
            if (parentSM.states[i].state == state) {
                pathElems.Add(parentSM.name);
                pathElems.Add(state.name);

                return true;
            }

        for (var i = 0; i < parentSM.stateMachines.Length; i++)
            if (GetFullPathRecursively(parentSM.stateMachines[i].stateMachine, state, pathElems)) {
                pathElems.Insert(0, parentSM.name);
                return true;
            }

        return false;
    }
}