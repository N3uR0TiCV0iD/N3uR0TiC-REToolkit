using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace GameHack
{
    public static class GameHack
    {
        static readonly Dictionary<string, GameObject> trackedGameObjects = new Dictionary<string, GameObject>();

        public static void OnSceneLoaded(Scene scene)
        {
            RELogger.Log($"OnSceneLoaded({scene.name})");
            if (scene.name == "...")
            {

            }
        }

        public static void OnAnimatorPlay(GameObject gameObject, string state)
        {
            RELogger.LogOnce($"OnAnimatorPlay({gameObject.name}, {state})", $"{gameObject.name}_{state}");
        }

        public static void OnObjectSpawn(ref GameObject gameObject)
        {
            RELogger.LogOnce($"Spawned: {gameObject.name} ({gameObject.tag})", gameObject.name);
        }
    }
}
