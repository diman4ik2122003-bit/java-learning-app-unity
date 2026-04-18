using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class NpcGridMovementController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    
    [Header("Grid Snapping")]
    [Tooltip("Отключи для NPC которые не привязаны к тайловой сетке")]
    public bool snapToGrid = true;
    
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    
    [Header("Grid Reference")]
    public Grid grid;
    
    [Header("Collision")]
    public Tilemap[] collisionTilemaps;
    
    private Vector2Int gridPosition;
    private bool isMoving = false;
    
    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (snapToGrid)
        {
            if (grid == null)
            {
                grid = FindFirstObjectByType<Grid>();
                if (grid != null)
                    Debug.Log($"Grid found at: {grid.transform.position}");
                else
                    Debug.LogError("Grid not found! Create a Grid GameObject in the scene.");
            }
            
            if (collisionTilemaps == null || collisionTilemaps.Length == 0)
                AutoFindCollisionTilemaps();
            
            gridPosition = WorldToGrid(transform.position);
            SnapToGrid();
            
            Debug.Log($"Player initialized at world: {transform.position}, grid: {gridPosition}");
        }
        // snapToGrid=false → остаёмся на месте где стоим в сцене
    }
    
    void AutoFindCollisionTilemaps()
    {
        Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        var collisionList = new System.Collections.Generic.List<Tilemap>();
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
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        if (grid != null)
        {
            Vector3 cellCenter = grid.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
            return new Vector3(cellCenter.x, cellCenter.y, transform.position.z);
        }
        else
        {
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
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            return new Vector2Int(cellPos.x, cellPos.y);
        }
        else
        {
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
        if (!snapToGrid || collisionTilemaps == null || collisionTilemaps.Length == 0)
            return true;
        
        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);
        foreach (Tilemap tilemap in collisionTilemaps)
        {
            if (tilemap == null) continue;
            if (tilemap.GetTile(tilePos) != null)
            {
                Debug.Log($"Blocked by {tilemap.name} at {gridPos}");
                return false;
            }
        }
        return true;
    }
    
    public IEnumerator MoveRight(int cells) { yield return Move(new Vector2Int(cells, 0)); }
    public IEnumerator MoveLeft(int cells)  { yield return Move(new Vector2Int(-cells, 0)); }
    public IEnumerator MoveUp(int cells)    { yield return Move(new Vector2Int(0, cells)); }
    public IEnumerator MoveDown(int cells)  { yield return Move(new Vector2Int(0, -cells)); }

    /// <summary>
    /// Перемещает прямо в world-позицию (для NPC в свободном режиме).
    /// </summary>
    public IEnumerator MoveToWorldPosition(Vector3 targetWorldPos)
    {
        if (isMoving) yield break;
        isMoving = true;

        float dx = targetWorldPos.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.01f && spriteRenderer != null)
            spriteRenderer.flipX = dx < 0;

        if (animator != null) animator.SetBool("isWalking", true);

        Vector3 startPos = transform.position;
        float dist = Vector3.Distance(startPos, targetWorldPos);
        float duration = dist / moveSpeed;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / duration);
            yield return null;
        }

        transform.position = targetWorldPos;
        isMoving = false;

        if (animator != null) animator.SetBool("isWalking", false);
    }
    
    IEnumerator Move(Vector2Int direction)
    {
        if (isMoving)
        {
            Debug.LogWarning("Already moving!");
            yield break;
        }
        
        isMoving = true;
        
        if (direction.x != 0 && spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;
        
        if (animator != null)
            animator.SetBool("isWalking", true);

        if (!snapToGrid)
        {
            // Свободный режим — direction = юниты в world space
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(direction.x, direction.y, 0);

            float dist = Vector3.Distance(startPos, endPos);
            float duration = dist / moveSpeed;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            transform.position = endPos;
        }
        else
        {
            // Режим сетки
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
                float dist = Vector3.Distance(startPos, endPos);
                float duration = dist / moveSpeed;
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
        }
        
        isMoving = false;
        
        if (animator != null)
            animator.SetBool("isWalking", false);
    }
    
    public Vector2Int GetGridPosition() => gridPosition;
    
    public void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
        if (snapToGrid) SnapToGrid();
        Debug.Log($"SetGridPosition: {pos} -> World: {transform.position}");
    }

    public void SetLogicalGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
    }
}
