using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GigaceeTools
{
    public static class MissingFinder
    {
        private const string Category = "Tools/Gigacee/Missing Finder/";

        [MenuItem(Category + "Find Missing in Current Stage", priority = 0)]
        public static void FindMissingInCurrentStage()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            string context;
            IEnumerable<GameObject> gameObjects;

            if (prefabStage == null)
            {
                context = SceneManager.GetActiveScene().path;

                gameObjects = Object
                    .FindObjectsOfType<GameObject>(true)
                    .Where(go
                        => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go))
                        && (go.hideFlags == HideFlags.None));
            }
            else
            {
                context = prefabStage.assetPath;

                gameObjects = prefabStage
                    .prefabContentsRoot
                    .GetComponentsInChildren<Transform>(true)
                    .Select(x => x.gameObject);
            }

            FindMissing(context, gameObjects, false);

            Debug.Log("The process has been completed.");
        }

        [MenuItem(Category + "Find Missing in Enabled Scenes", priority = 1)]
        public static void FindMissingInEnabledScenes()
        {
            string currentScenePath = SceneManager.GetActiveScene().path;

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                EditorSceneManager.OpenScene(scene.path);
                FindMissingInCurrentStage();
            }

            EditorSceneManager.OpenScene(currentScenePath);

            Debug.Log("All processes have been completed.");
        }

        [MenuItem(Category + "Find Missing in All Scenes", priority = 2)]
        public static void FindMissingInAllScenes()
        {
            string currentScenePath = SceneManager.GetActiveScene().path;

            foreach (string path in AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath))
            {
                EditorSceneManager.OpenScene(path);
                FindMissingInCurrentStage();
            }

            EditorSceneManager.OpenScene(currentScenePath);

            Debug.Log("All processes have been completed.");
        }

        [MenuItem(Category + "Find Missing in Prefab Assets", priority = 3)]
        public static void FindMissingInPrefabAssets()
        {
            foreach (string path in AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath))
            {
                IEnumerable<GameObject> gameObjects = AssetDatabase
                    .LoadAssetAtPath<Transform>(path)
                    .GetComponentsInChildren<Transform>()
                    .Select(x => x.gameObject);

                FindMissing(path, gameObjects, true);
            }

            Debug.Log("The process has been completed.");
        }

        private static void FindMissing(string context, IEnumerable<GameObject> gameObjects, bool isPrefabAsset)
        {
            foreach (GameObject go in gameObjects)
            {
                foreach (Component component in go.GetComponents<Component>())
                {
                    if (!component)
                    {
                        Debug.LogWarning(
                            $"Missing component found!\n{context} - {GetFullPath(go)}",
                            isPrefabAsset ? go.transform.root.gameObject : go
                        );

                        continue;
                    }

                    SerializedProperty property = new SerializedObject(component).GetIterator();

                    PropertyInfo objRefValueMethod = typeof(SerializedProperty).GetProperty(
                        "objectReferenceStringValue",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }

                        var objectReferenceStringValue = string.Empty;

                        if (objRefValueMethod != null)
                        {
                            objectReferenceStringValue = (string)objRefValueMethod
                                .GetGetMethod(true)
                                .Invoke(property, new object[] { });
                        }

                        if (HasMissingReference(property, objectReferenceStringValue))
                        {
                            Debug.LogWarning(
                                $"Missing reference found!\n{context} - {GetFullPath(go)} - {component.GetType().Name} - {ObjectNames.NicifyVariableName(property.name)}",
                                isPrefabAsset ? go.transform.root.gameObject : go
                            );
                        }
                    }
                }
            }
        }

        private static string GetFullPath(GameObject go)
        {
            Transform parent = go.transform.parent;

            return parent == null ? go.name : GetFullPath(parent.gameObject) + "/" + go.name;
        }

        private static bool HasMissingReference(SerializedProperty property, string reference)
        {
            return (property.objectReferenceValue == null)
                && ((property.objectReferenceInstanceIDValue != 0) || reference.StartsWith("Missing"));
        }
    }
}
