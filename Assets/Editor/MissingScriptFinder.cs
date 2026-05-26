using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Check/Find Missing Scripts In Scene")]
    public static void FindMissingScriptsInOpenScenes()
    {
        List<GameObject> results = new List<GameObject>();
        StringBuilder logBuilder = new StringBuilder();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                CheckGameObjectRecursive(root.transform, scene.name, results, logBuilder);
            }
        }

        if (results.Count == 0)
        {
            Selection.objects = new Object[0];
            Debug.Log("[MissingScriptFinder] No missing scripts found in open scenes.");
            EditorUtility.DisplayDialog("Find Missing Scripts", "No missing scripts found in open scenes.", "OK");
            return;
        }

        Selection.objects = results.ToArray();
        Debug.LogWarning(
            "[MissingScriptFinder] Found " + results.Count + " GameObject(s) with missing scripts. They have been selected:\n" +
            logBuilder);
        EditorUtility.DisplayDialog("Find Missing Scripts", "Found " + results.Count + " GameObject(s) with missing scripts. They have been selected. See Console for details.", "OK");
    }

    private static void CheckGameObjectRecursive(
        Transform transform,
        string sceneName,
        List<GameObject> results,
        StringBuilder logBuilder)
    {
        int missingCount = GetMissingScriptCount(transform.gameObject);
        if (missingCount > 0)
        {
            results.Add(transform.gameObject);
            logBuilder.Append("Scene: ")
                .Append(sceneName)
                .Append(" | Missing: ")
                .Append(missingCount)
                .Append(" | Path: ")
                .Append(GetHierarchyPath(transform))
                .AppendLine();
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            CheckGameObjectRecursive(transform.GetChild(i), sceneName, results, logBuilder);
        }
    }

    private static int GetMissingScriptCount(GameObject gameObject)
    {
        int count = 0;
        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
            {
                count++;
            }
        }

        return count;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        Stack<string> names = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names.ToArray());
    }
}
