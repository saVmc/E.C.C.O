using UnityEngine;
using UnityEditor;

public static class EditorCursorTextureFixer
{
    [MenuItem("Tools/Texture Utilities/Fix Cursor Texture Import Settings (Selected)")]
    public static void FixSelectedTextures()
    {
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            FixImporter(path);
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Cursor Texture Fixer", "Processed selected textures.", "OK");
    }

    [MenuItem("Tools/Texture Utilities/Fix Cursor Texture Import Settings (Folder)")]
    public static void FixTexturesInFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Select Assets Folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(folder))
            return;
        if (!folder.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a folder inside the project's Assets folder.", "OK");
            return;
        }

        string relative = "Assets" + folder.Substring(Application.dataPath.Length);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { relative });
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            FixImporter(path);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Cursor Texture Fixer", "Processed textures in folder.", "OK");
    }

    private static void FixImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }
}