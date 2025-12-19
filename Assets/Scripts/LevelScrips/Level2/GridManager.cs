// GridManager.cs
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    
    [Header("Grid Settings")]
    public int gridWidth = 16;
    public int gridHeight = 12;
    public float cellSize = 1f;
    
    [Header("Visual Debug")]
    public bool showGrid = true;
    public Color gridColor = new Color(1, 1, 1, 0.1f);
    
    [Header("Tilemap")]
    public GameObject groundTilePrefab;
    public GameObject wallTilePrefab;
    
    private int[,] grid; // 0 = empty, 1 = walkable, 2 = wall
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        InitializeGrid();
    }
    
    void InitializeGrid()
    {
        grid = new int[gridWidth, gridHeight];
        
        // По умолчанию всё walkable
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = 1;
            }
        }
    }
    
    public bool IsWalkable(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridWidth || 
            gridPos.y < 0 || gridPos.y >= gridHeight)
        {
            return false;
        }
        
        return grid[gridPos.x, gridPos.y] == 1;
    }
    
    public void SetWall(Vector2Int gridPos)
    {
        if (IsInBounds(gridPos))
        {
            grid[gridPos.x, gridPos.y] = 2;
        }
    }
    
    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && 
               pos.y >= 0 && pos.y < gridHeight;
    }
    
    // Конвертация координат
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize - (gridWidth * cellSize) / 2f + cellSize / 2f,
            gridPos.y * cellSize - (gridHeight * cellSize) / 2f + cellSize / 2f,
            0
        );
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        float offsetX = (gridWidth * cellSize) / 2f - cellSize / 2f;
        float offsetY = (gridHeight * cellSize) / 2f - cellSize / 2f;
        
        return new Vector2Int(
            Mathf.RoundToInt((worldPos.x + offsetX) / cellSize),
            Mathf.RoundToInt((worldPos.y + offsetY) / cellSize)
        );
    }
    
    void OnDrawGizmos()
    {
        if (!showGrid) return;
        
        Gizmos.color = gridColor;
        
        float startX = -(gridWidth * cellSize) / 2f;
        float startY = -(gridHeight * cellSize) / 2f;
        
        // Вертикальные линии
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(startX + x * cellSize, startY, 0);
            Vector3 end = new Vector3(startX + x * cellSize, startY + gridHeight * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }
        
        // Горизонтальные линии
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = new Vector3(startX, startY + y * cellSize, 0);
            Vector3 end = new Vector3(startX + gridWidth * cellSize, startY + y * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}
