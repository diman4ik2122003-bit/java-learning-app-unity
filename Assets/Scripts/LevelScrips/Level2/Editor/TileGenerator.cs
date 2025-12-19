#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TileGenerator
{
    [MenuItem("Tools/Generate Pixel Tiles")]
    static void Generate()
    {
        string folder = "Assets/Tiles/Sprites";
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        
        // Ground tile (трава)
        CreateTileSprite("Ground", 16, 16, (x, y) => {
            float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
            return new Color(0.3f + noise * 0.1f, 0.5f + noise * 0.15f, 0.2f + noise * 0.05f, 1);
        });
        
        // Wall tile (каменная стена)
        CreateTileSprite("Wall", 16, 16, (x, y) => {
            float noise = Mathf.PerlinNoise(x * 0.4f, y * 0.4f);
            return new Color(0.4f + noise * 0.05f, 0.4f + noise * 0.05f, 0.45f + noise * 0.05f, 1);
        });
        
        // Water tile (вода)
        CreateTileSprite("Water", 16, 16, (x, y) => {
            float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
            return new Color(0.2f, 0.4f + noise * 0.1f, 0.7f + noise * 0.1f, 1);
        });
        
        // Path tile (дорожка)
        CreateTileSprite("Path", 16, 16, (x, y) => {
            float noise = Mathf.PerlinNoise(x * 0.5f, y * 0.5f);
            return new Color(0.6f + noise * 0.05f, 0.5f + noise * 0.05f, 0.4f + noise * 0.05f, 1);
        });
        
        AssetDatabase.Refresh();
        Debug.Log("✅ Tiles generated in " + folder);
    }
    
    static void CreateTileSprite(string name, int width, int height, System.Func<int, int, Color> colorFunc)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, colorFunc(x, y));
            }
        }
        tex.Apply();
        
        string path = $"Assets/Tiles/Sprites/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        
        AssetDatabase.ImportAsset(path);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16; // важно!
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }
    }
}
#endif
