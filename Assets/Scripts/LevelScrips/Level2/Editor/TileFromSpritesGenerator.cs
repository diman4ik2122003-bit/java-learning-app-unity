#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class TileFromSpritesGenerator
{
    [MenuItem("Tools/Generate Tiles from Sprites")]
    static void GenerateTiles()
    {
        string spritesFolder = "Assets/Tiles/Sprites";
        string outputFolder = "Assets/Tiles/TileAssets";
        
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        
        // Находим все текстуры
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesFolder });
        
        int count = 0;
        
        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Загружаем все спрайты из текстуры
            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            
            foreach (Object obj in sprites)
            {
                if (obj is Sprite sprite)
                {
                    // Создаём Tile
                    Tile tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    
                    // Определяем является ли тайл стеной
                    string spriteName = sprite.name.ToLower();
                    if (spriteName.Contains("wall") || spriteName.Contains("door"))
                    {
                        tile.colliderType = Tile.ColliderType.Grid;
                    }
                    
                    // Сохраняем Tile
                    string tilePath = $"{outputFolder}/{sprite.name}_Tile.asset";
                    AssetDatabase.CreateAsset(tile, tilePath);
                    
                    count++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Created {count} tile assets!");
    }
}
#endif
