using System;
using System.Text;
using UnityEngine;
using System.Reflection;
using HutongGames.PlayMaker;
using System.Collections.Generic;

public static class REUnityPlayMakerHelper
{
    static readonly REReflectionHelper.FieldInfo<ActionData, List<FsmGameObject>> gameObjectParamsField;
    static readonly REReflectionHelper.FieldInfo<FsmGameObject, GameObject> valueField;

    static REUnityPlayMakerHelper()
    {
        gameObjectParamsField = REReflectionHelper.GetFieldInfo<ActionData, List<FsmGameObject>>("fsmGameObjectParams", REReflectionHelper.EzBindingFlags.PrivateProtected);
        valueField = REReflectionHelper.GetFieldInfo<FsmGameObject, GameObject>("value", REReflectionHelper.EzBindingFlags.PrivateProtected);
    }

    public static void IterateFSMGameObjects(GameObject gameObject, int fsmIndex, Action<FsmState, GameObject> callback)
    {
        var fsmComponents = gameObject.GetComponents<PlayMakerFSM>();
        if (InvalidFSMBound(gameObject, fsmComponents, fsmIndex))
        {
            return;
        }
        var fsmComponent = fsmComponents[fsmIndex];
        foreach (var state in fsmComponent.FsmStates)
        {
            IterateFSMStateGameObjects(state, callback);
        }
    }

    public static void IterateFSMStateGameObjects(FsmState fsmState, Action<FsmState, GameObject> callback)
    {
        var actionData = fsmState.ActionData;
        var fsmGameObjectParams = gameObjectParamsField.GetValue(actionData);
        foreach (var fsmGameObject in fsmGameObjectParams)
        {
            var gameObject = valueField.GetValue(fsmGameObject);
            if (gameObject != null)
            {
                callback(fsmState, gameObject);
            }
        }
    }

    public static FsmState FindFSMState(GameObject gameObject, string stateName)
    {
        return FindFSMState(gameObject, 0, stateName);
    }

    public static FsmState FindFSMState(GameObject gameObject, int fsmIndex, string stateName)
    {
        var fsmComponents = gameObject.GetComponents<PlayMakerFSM>();
        if (InvalidFSMBound(gameObject, fsmComponents, fsmIndex))
        {
            return null;
        }
        foreach (var state in fsmComponents[fsmIndex].FsmStates)
        {
            if (state.Name == stateName)
            {
                return state;
            }
        }
        return null;
    }

    private static bool InvalidFSMBound(GameObject gameObject, PlayMakerFSM[] fsmComponents, int fsmIndex)
    {
        if (fsmIndex < 0 || fsmIndex >= fsmComponents.Length)
        {
            var errorMessage = $"[ERROR] \"{gameObject.name}\": {nameof(fsmIndex)} ({fsmIndex}) out of range. (Total: {fsmComponents.Length}).";
            var errorKey = $"{gameObject.name}_{fsmIndex}";
            RELogger.LogWithCooldown(errorMessage, errorKey, 30);
            return true;
        }
        return false;
    }

    public static void DumpFSMStates(GameObject gameObject)
    {
        var prefix = "";
        var stringBuilder = new StringBuilder();
        var fsmComponents = gameObject.GetComponents<PlayMakerFSM>();
        stringBuilder.AppendLine($"\"{gameObject.name}\" has the following FSMs:");
        for (int index = 0; index < fsmComponents.Length; index++)
        {
            var fsmComponent = fsmComponents[index];
            stringBuilder.Append(prefix);
            stringBuilder.AppendLine($"=> PlayMakerFSM[{index}]:");
            foreach (var state in fsmComponent.FsmStates)
            {
                stringBuilder.AppendLine($"- {state.Name}");
            }
            prefix = "\n";
        }
        RELogger.LogOnce(stringBuilder.ToString(), $"{gameObject.name}_states");
    }
}
