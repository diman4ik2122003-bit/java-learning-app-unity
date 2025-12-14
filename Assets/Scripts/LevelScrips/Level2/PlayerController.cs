using UnityEngine;
using System.Collections;  // ← добавь эту строку

public class PlayerController : MonoBehaviour
{
    private Vector2 startPosition;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    // Старые методы (оставь для совместимости)
    public void MoveRight(float distance)
    {
        transform.position += Vector3.right * distance;
    }

    public void MoveLeft(float distance)
    {
        transform.position += Vector3.left * distance;
    }

    public void Jump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    public void ResetState()
    {
        transform.position = startPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
    }

    // Новые IEnumerator методы для анимации
    public IEnumerator MoveRightCoroutine(int distance)
    {
        float targetX = transform.position.x + distance;
        
        while (transform.position.x < targetX)
        {
            transform.position += Vector3.right * 5f * Time.deltaTime;
            yield return null;
        }
        
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
    }

    public IEnumerator MoveLeftCoroutine(int distance)
    {
        float targetX = transform.position.x - distance;
        
        while (transform.position.x > targetX)
        {
            transform.position += Vector3.left * 5f * Time.deltaTime;
            yield return null;
        }
        
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
    }

    public IEnumerator JumpCoroutine(int height)
    {
        float startY = transform.position.y;
        float targetY = startY + height;
        float time = 0;
        float duration = 0.5f;
        
        // Прыжок вверх
        while (time < duration)
        {
            time += Time.deltaTime;
            float newY = Mathf.Lerp(startY, targetY, time / duration);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }
        
        // Падение вниз
        time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float newY = Mathf.Lerp(targetY, startY, time / duration);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }
        
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
    }

    void Update()
    {
        // Закомментировано для теста
    }
}
