using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class RestoreChineseCharacters : EditorWindow
{
    [MenuItem("Tools/Restore Chinese Characters in Scripts")]
    public static void ShowWindow()
    {
        GetWindow<RestoreChineseCharacters>("Restore Chinese Characters");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Restore Chinese Characters"))
        {
            RestoreScriptsInProject(Application.dataPath);
            EditorUtility.DisplayDialog("Restore Completed", "All C# scripts have been restored to UTF-8 encoding.", "OK");
        }
    }
    
    [MenuItem("Assets/Check/编码Ansi-> UTF-8")]
    private static void ReadAnsiText()
    {
        // 获取当前在Unity编辑器中选中的对象
        UnityEngine.Object selectedObject = Selection.activeObject;

        if (selectedObject != null && selectedObject is TextAsset)
        {
            TextAsset textAsset = selectedObject as TextAsset;

            string assetPath = AssetDatabase.GetAssetPath(textAsset);

            Encoding encoding = Encoding.GetEncoding(936);

            string text = File.ReadAllText(assetPath, encoding);

            System.IO.File.WriteAllText(assetPath, text, Encoding.UTF8);

            AssetDatabase.Refresh();
        }
    }

    private static void RestoreScriptsInProject(string directory)
    {
        // Get all C# files in the directory and subdirectories
        string[] scriptFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
        
        foreach (var file in scriptFiles)
        {
            try
            {
                // Read the content of the file with the default encoding
                string content = File.ReadAllText(file, Encoding.Default);

                File.WriteAllText(file, content, Encoding.UTF8);
                Debug.Log($"Restored: {file}");
                // // If the file contains Chinese characters, rewrite it with UTF-8 encoding
                // if (content.Contains("一") || content.Contains("中") || content.Contains("文")) // Simplified check for Chinese characters
                // {
                //     // Write the content back to the file using UTF-8 encoding
                //     File.WriteAllText(file, content, Encoding.UTF8);
                //     Debug.Log($"Restored: {file}");
                // }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process file: {file}. Error: {ex.Message}");
            }
        }
    }
}
