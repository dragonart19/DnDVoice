using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class SceneIntegrityTests
    {
        [Test]
        public void ProjectScenesContainNoMissingMonoBehaviours()
        {
            var missing = new List<string>();
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            foreach (var guid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/_Recovery/"))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        CollectMissingScripts(root, path, missing);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            Assert.That(missing, Is.Empty, string.Join("\n", missing));
        }

        private static void CollectMissingScripts(GameObject gameObject, string scenePath, List<string> missing)
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (count > 0)
            {
                missing.Add($"{scenePath}: {GetHierarchyPath(gameObject)} ({count} script mancanti)");
            }

            foreach (Transform child in gameObject.transform)
            {
                CollectMissingScripts(child.gameObject, scenePath, missing);
            }
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            var path = gameObject.name;
            var parent = gameObject.transform.parent;
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return path;
        }
    }
}
