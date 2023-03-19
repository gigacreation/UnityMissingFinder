using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace GigaCreation.Tools.MissingFinder.Editor
{
    public static class MissingFinderMenuItems
    {
        private const int CategoryPriority = 20000;
        private const string Category = "Tools/GIGA CREATION/Missing Finder/";

        [MenuItem(Category + "Find Missing in Current Scene", priority = CategoryPriority)]
        public static void FindMissingInCurrentScene()
        {
            int numOfMissing = FindMissingInScene();

            Debug.Log($"{numOfMissing} missing found.");
        }

        [MenuItem(Category + "Find Missing in Enabled Scenes", priority = CategoryPriority + 1)]
        public static void FindMissingInEnabledScenes()
        {
            var numOfMissing = 0;
            string currentScenePath = SceneManager.GetActiveScene().path;

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                EditorSceneManager.OpenScene(scene.path);
                numOfMissing += FindMissingInScene();
            }

            EditorSceneManager.OpenScene(currentScenePath);

            Debug.Log($"{numOfMissing} missing found.");
        }

        [MenuItem(Category + "Find Missing in All Scenes", priority = CategoryPriority + 2)]
        public static void FindMissingInAllScenes()
        {
            var numOfMissing = 0;
            string currentScenePath = SceneManager.GetActiveScene().path;

            foreach (string path in AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath))
            {
                EditorSceneManager.OpenScene(path);
                numOfMissing += FindMissingInScene();
            }

            EditorSceneManager.OpenScene(currentScenePath);

            Debug.Log($"{numOfMissing} missing found.");
        }

        [MenuItem(Category + "Find Missing in Current Prefab Stage", priority = CategoryPriority + 3)]
        public static void FindMissingInCurrentPrefabStage()
        {
            int numOfMissing = FindMissingInPrefabStage();

            Debug.Log($"{numOfMissing} missing found.");
        }

        [MenuItem(Category + "Find Missing in All Prefab Assets", priority = CategoryPriority + 4)]
        public static void FindMissingInAllPrefabAssets()
        {
            int numOfMissing = AssetDatabase
                .FindAssets("t:Prefab")
                .Select(guid =>
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    IEnumerable<GameObject> gameObjects = AssetDatabase
                        .LoadAssetAtPath<Transform>(assetPath)
                        .GetComponentsInChildren<Transform>()
                        .Select(x => x.gameObject);

                    return FindMissingInGameObjects(gameObjects, assetPath, true);
                })
                .Sum();

            Debug.Log($"{numOfMissing} missing found.");
        }

        private static int FindMissingInScene()
        {
            IEnumerable<GameObject> gameObjectsInCurrentScene = Object
                .FindObjectsOfType<GameObject>(true)
                .Where(go
                    => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go))
                    && (go.hideFlags == HideFlags.None));

            return FindMissingInGameObjects(gameObjectsInCurrentScene, SceneManager.GetActiveScene().path, false);
        }

        private static int FindMissingInPrefabStage()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage == null)
            {
                Debug.LogError("This is not a Prefab stage.");
                return 0;
            }

            IEnumerable<GameObject> gameObjectsInCurrentPrefabStage = prefabStage
                .prefabContentsRoot
                .GetComponentsInChildren<Transform>(true)
                .Select(x => x.gameObject);

            return FindMissingInGameObjects(gameObjectsInCurrentPrefabStage, prefabStage.assetPath, false);
        }

        private static int FindMissingInGameObjects(
            IEnumerable<GameObject> gameObjects, string context, bool isPrefabAsset
        )
        {
            var numOfMissing = 0;

            foreach (GameObject go in gameObjects)
            {
                foreach (Component comp in go.GetComponents<Component>())
                {
                    if (!comp)
                    {
                        numOfMissing++;

                        Debug.LogWarning(
                            $"Missing component found!\n{context} - {GetFullPath(go)}",
                            isPrefabAsset ? go.transform.root.gameObject : go
                        );

                        continue;
                    }

                    SerializedProperty property = new SerializedObject(comp).GetIterator();

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
                            numOfMissing++;

                            Debug.LogWarning(
                                $"Missing reference found!\n{context} - {GetFullPath(go)} - {comp.GetType().Name} - {ObjectNames.NicifyVariableName(property.name)}",
                                isPrefabAsset ? go.transform.root.gameObject : go
                            );
                        }
                    }
                }
            }

            return numOfMissing;
        }

        private static bool HasMissingReference(SerializedProperty property, string reference)
        {
            return (property.objectReferenceValue == null)
                && ((property.objectReferenceInstanceIDValue != 0) || reference.StartsWith("Missing"));
        }

        private static string GetFullPath(GameObject go)
        {
            Transform parent = go.transform.parent;

            return parent == null ? go.name : GetFullPath(parent.gameObject) + "/" + go.name;
        }
    }
}
