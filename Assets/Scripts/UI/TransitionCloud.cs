using System;
using System.Collections;
using UnityEngine;

public class TransitionCloud : MonoBehaviour
{
    public Vector2 endPos;
    public float speed = 700f;
    public float delay = 0f;

    private RectTransform rect;
    private Vector2 originalPos;
    private float originalSpeed;
    private Coroutine moveRoutine;

    public event Action OnMoveComplete;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
        originalSpeed = speed;
    }

    /// <summary>
    /// Применить множитель к исходной скорости облака
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speed = originalSpeed * multiplier;
    }

    public void MoveIn()
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveCloud(rect.anchoredPosition, endPos));
    }

    public void MoveOut()
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveCloud(rect.anchoredPosition, originalPos));
    }

    private IEnumerator MoveCloud(Vector2 from, Vector2 to)
    {
        yield return new WaitForSeconds(delay);

        while ((rect.anchoredPosition - to).sqrMagnitude > 0.01f)
        {
            float distance = Vector2.Distance(rect.anchoredPosition, to);

            // Радиус замедления
            float slowDownRadius = 100f;
            float currentSpeed = speed;

            if (distance < slowDownRadius)
            {
                currentSpeed = Mathf.Lerp(10f, speed, distance / slowDownRadius);
            }

            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, to, currentSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        rect.anchoredPosition = to;
        moveRoutine = null;
        OnMoveComplete?.Invoke();
    }
}
