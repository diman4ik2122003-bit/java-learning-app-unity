using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private GridMovementController gridMovement;
    private Vector2Int startGridPosition;
    private bool startPositionInitialized = false;
    
    void Awake()
    {
        gridMovement = GetComponent<GridMovementController>();
        if (gridMovement == null)
        {
            gridMovement = gameObject.AddComponent<GridMovementController>();
        }
    }
    
    void Start()
    {
        StartCoroutine(InitializeStart());
    }
    
    IEnumerator InitializeStart()
    {
        yield return new WaitForEndOfFrame();
        
        // Сохраняем стартовую позицию только если она ещё не была установлена
        if (!startPositionInitialized)
        {
            startGridPosition = gridMovement.GetGridPosition();
            startPositionInitialized = true;
            Debug.Log($"Player start position initialized: {startGridPosition}");
        }
    }
    
    // === API для JavaCodeExecutor ===
    public IEnumerator MoveRightCoroutine(int cells)
    {
        yield return gridMovement.MoveRight(cells);
    }
    
    public IEnumerator MoveLeftCoroutine(int cells)
    {
        yield return gridMovement.MoveLeft(cells);
    }
    
    public IEnumerator MoveUpCoroutine(int cells)
    {
        yield return gridMovement.MoveUp(cells);
    }
    
    public IEnumerator MoveDownCoroutine(int cells)
    {
        yield return gridMovement.MoveDown(cells);
    }
    
    // === API для старого CodeExecutor ===
    public void MoveRight(float cells)
    {
        StartCoroutine(MoveRightCoroutine(Mathf.RoundToInt(cells)));
    }
    
    public void MoveLeft(float cells)
    {
        StartCoroutine(MoveLeftCoroutine(Mathf.RoundToInt(cells)));
    }
    
    public void MoveUp(float cells)
    {
        StartCoroutine(MoveUpCoroutine(Mathf.RoundToInt(cells)));
    }
    
    public void MoveDown(float cells)
    {
        StartCoroutine(MoveDownCoroutine(Mathf.RoundToInt(cells)));
    }
    
    public void Jump(float height)
    {
        Debug.Log("Jump is not available in top-down mode");
    }
    
    // === Управление состоянием ===
    public void ResetState()
    {
        Debug.Log($"ResetState called. Returning to: {startGridPosition}");
        
        StopAllCoroutines();
        gridMovement.StopAllCoroutines();
        
        // Возвращаемся на стартовую позицию
        gridMovement.SetGridPosition(startGridPosition);
    }
    
    // ⭐ Новый метод для установки стартовой позиции
    public void SetStartPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = gridMovement.WorldToGrid(worldPos);
        startGridPosition = gridPos;
        startPositionInitialized = true;
        
        gridMovement.SetGridPosition(gridPos);
        
        Debug.Log($"Start position set to: World={worldPos}, Grid={gridPos}");
    }
    
    public Vector2Int GetGridPosition()
    {
        return gridMovement.GetGridPosition();
    }
}
