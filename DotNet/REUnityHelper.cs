using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using UnityObject = UnityEngine.Object;

public static class REUnityHelper
{
    static readonly HashSet<string> loggedComponentObjects = new HashSet<string>();

    private class SceneInspector : MonoBehaviour
    {
        static GameObject instance;

        Vector2 scrollPosition;
        Vector2 selectedObjectScroll;
        GameObject selectedGameObject;
        Dictionary<GameObject, bool> expandedObjects = new Dictionary<GameObject, bool>();

        public static void AttachToScene()
        {
            if (instance != null)
            {
                return;
            }
            instance = new GameObject("REUnityHelper.SceneInspector");
            instance.AddComponent<SceneInspector>();
            UnityObject.DontDestroyOnLoad(instance);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, Screen.height - 20));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(350), GUILayout.Height(Screen.height - 20));

            var scene = SceneManager.GetActiveScene();
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                DrawGameObject(gameObject, 0);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            if (selectedGameObject != null)
            {
                DrawSelectedObjectDetails();
            }
        }

        private void DrawGameObject(GameObject gameObject, int level)
        {
            GUILayout.BeginHorizontal();

            //Add indentation
            GUILayout.Space(level * 20);

            var isExpanded = DrawExpandControl(gameObject);

            DrawActiveToggle(gameObject);

            if (GUILayout.Button(gameObject.name, GUILayout.Width(200)))
            {
                selectedGameObject = gameObject;
            }

            GUILayout.EndHorizontal();

            if (isExpanded)
            {
                //Don't change to "var"
                foreach (Transform child in gameObject.transform)
                {
                    DrawGameObject(child.gameObject, level + 1);
                }
            }
        }

        private bool DrawExpandControl(GameObject gameObject)
        {
            if (gameObject.transform.childCount == 0)
            {
                //Placeholder for alignment
                GUILayout.Space(25);
                return false;
            }
            return DrawExpandButton(gameObject);
        }

        private bool DrawExpandButton(GameObject gameObject)
        {
            bool isExpanded = expandedObjects.ContainsKey(gameObject) && expandedObjects[gameObject];
            if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(25)))
            {
                expandedObjects[gameObject] = !isExpanded;
            }
            return isExpanded;
        }

        private void DrawActiveToggle(GameObject gameObject)
        {
            bool isActive = GUILayout.Toggle(gameObject.activeSelf, "", GUILayout.Width(20));
            if (isActive != gameObject.activeSelf)
            {
                gameObject.SetActive(isActive);
            }
        }

        private void DrawSelectedObjectDetails()
        {
            const int y = 10;
            const int width = 400;
            var height = Screen.height - 20;
            var x = Screen.width - width - 10;  // Align to top-right

            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.Label($"{selectedGameObject.name} Components:", "BoldLabel");

            selectedObjectScroll = GUILayout.BeginScrollView(selectedObjectScroll, GUILayout.Width(width), GUILayout.Height(height - 40));

            foreach (var component in selectedGameObject.GetComponents<Component>())
            {
                GUILayout.BeginHorizontal();

                if (component is Behaviour behaviour)
                {
                    DrawEnableToggle(behaviour);
                }
                else
                {
                    //Placeholder for alignment
                    GUILayout.Space(20);
                }

                GUILayout.Label(component.GetType().Name, GUILayout.Width(200));

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEnableToggle(Behaviour behaviour)
        {
            bool isEnabled = GUILayout.Toggle(behaviour.enabled, "", GUILayout.Width(20));
            if (isEnabled != behaviour.enabled)
            {
                behaviour.enabled = isEnabled;
            }
        }
    }

    public static void AddSceneInspector()
    {
        SceneInspector.AttachToScene();
    }

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

    public static void ApplyRandomScale(Transform target, float min, float max)
    {
        float randomScale = Random.Range(min, max);
        target.localScale = Vector3.one * randomScale;
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
