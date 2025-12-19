using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class GridMovementController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    
    [Header("Grid Reference")]
    public Grid grid; // ← Ссылка на Grid компонент
    
    [Header("Collision")]
    public Tilemap[] collisionTilemaps;
    
    private Vector2Int gridPosition;
    private bool isMoving = false;
    
    void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Автопоиск Grid
        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid != null)
            {
                Debug.Log($"Grid found at: {grid.transform.position}");
            }
            else
            {
                Debug.LogError("Grid not found! Create a Grid GameObject in the scene.");
            }
        }
        
        // Автопоиск коллизионных Tilemap
        if (collisionTilemaps == null || collisionTilemaps.Length == 0)
        {
            AutoFindCollisionTilemaps();
        }
        
        gridPosition = WorldToGrid(transform.position);
        SnapToGrid();
        
        Debug.Log($"Player initialized at world: {transform.position}, grid: {gridPosition}");
    }
    
    void AutoFindCollisionTilemaps()
    {
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        System.Collections.Generic.List<Tilemap> collisionList = new System.Collections.Generic.List<Tilemap>();
        
        foreach (Tilemap tilemap in allTilemaps)
        {
            if (tilemap.GetComponent<TilemapCollider2D>() != null)
            {
                collisionList.Add(tilemap);
                Debug.Log($"Auto-found collision tilemap: {tilemap.name}");
            }
        }
        
        collisionTilemaps = collisionList.ToArray();
    }
    
    // ⭐ Правильная конвертация через Grid
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        if (grid != null)
        {
            // Используем встроенный метод Grid
            Vector3 cellCenter = grid.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
            return new Vector3(cellCenter.x, cellCenter.y, transform.position.z);
        }
        else
        {
            // Fallback если нет Grid
            float cellSize = 1f;
            return new Vector3(
                gridPos.x * cellSize + cellSize / 2f,
                gridPos.y * cellSize + cellSize / 2f,
                transform.position.z
            );
        }
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (grid != null)
        {
            // Используем встроенный метод Grid
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            return new Vector2Int(cellPos.x, cellPos.y);
        }
        else
        {
            // Fallback
            float cellSize = 1f;
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / cellSize),
                Mathf.FloorToInt(worldPos.y / cellSize)
            );
        }
    }
    
    void SnapToGrid()
    {
        transform.position = GridToWorld(gridPosition);
    }
    
    bool IsWalkable(Vector2Int gridPos)
    {
        if (collisionTilemaps == null || collisionTilemaps.Length == 0)
        {
            return true;
        }
        
        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);
        
        foreach (Tilemap tilemap in collisionTilemaps)
        {
            if (tilemap == null) continue;
            
            TileBase tile = tilemap.GetTile(tilePos);
            
            if (tile != null)
            {
                Debug.Log($"Blocked by {tilemap.name} at {gridPos}");
                return false;
            }
        }
        
        return true;
    }
    
    public IEnumerator MoveRight(int cells)
    {
        yield return Move(new Vector2Int(cells, 0));
    }
    
    public IEnumerator MoveLeft(int cells)
    {
        yield return Move(new Vector2Int(-cells, 0));
    }
    
    public IEnumerator MoveUp(int cells)
    {
        yield return Move(new Vector2Int(0, cells));
    }
    
    public IEnumerator MoveDown(int cells)
    {
        yield return Move(new Vector2Int(0, -cells));
    }
    
    IEnumerator Move(Vector2Int direction)
    {
        if (isMoving)
        {
            Debug.LogWarning("Already moving!");
            yield break;
        }
        
        isMoving = true;
        
        Vector2Int currentPos = gridPosition;
        Vector2Int step = new Vector2Int(
            direction.x != 0 ? (int)Mathf.Sign(direction.x) : 0,
            direction.y != 0 ? (int)Mathf.Sign(direction.y) : 0
        );
        
        int cellsToMove = Mathf.Abs(direction.x + direction.y);
        
        for (int i = 0; i < cellsToMove; i++)
        {
            Vector2Int nextPos = currentPos + step;
            
            if (!IsWalkable(nextPos))
            {
                Debug.Log($"Can't move to {nextPos} - blocked!");
                break;
            }
            
            Vector3 startPos = transform.position;
            Vector3 endPos = GridToWorld(nextPos);
            
            float duration = 1f / moveSpeed;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            
            transform.position = endPos;
            currentPos = nextPos;
        }
        
        gridPosition = currentPos;
        isMoving = false;
    }
    
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
    
    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
        SnapToGrid();
        Debug.Log($"SetGridPosition: {pos} -> World: {transform.position}");
    }
}
