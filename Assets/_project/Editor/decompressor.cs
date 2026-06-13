using UnityEditor;
using UnityEngine;

public class UncompressTextures : Editor
{
    [MenuItem("Tools/Uncompress All Textures")]
    static void UncompressAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        Debug.Log("All textures set to Uncompressed!");
    }
}
