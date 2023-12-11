using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public static class REUnityHelper
{
    static readonly HashSet<string> loggedComponentObjects = new HashSet<string>();

    public static GameObject FindGameObject(string name)
    {
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var rootObject in rootObjects)
        {
            var foundObject = FindInHierarchy(rootObject, name);
            if (foundObject != null)
            {
                return foundObject;
            }
        }
        return null;
    }

    public static GameObject FindInHierarchy(GameObject rootObject, string name)
    {
        if (rootObject.name == name)
        {
            return rootObject;
        }
        foreach (Transform child in rootObject.transform)
        {
            var foundObject = FindInHierarchy(child.gameObject, name);
            if (foundObject != null)
            {
                return foundObject;
            }
        }
        return null;
    }

    public static void LogAllComponentsOnce(GameObject gameObject, string key)
    {
        if (loggedComponentObjects.Add(key))
        {
            LogAllComponents(gameObject);
        }
    }

    public static void LogAllComponents(GameObject gameObject)
    {
        var components = gameObject.GetComponents<Component>();
        var result = new StringBuilder();
        result.AppendLine($"\"{gameObject.name}\" has the following components:");
        foreach (var component in components)
        {
            result.AppendLine($"- {component.GetType().Name}");
        }
        RELogger.Log(result.ToString());
    }
}
