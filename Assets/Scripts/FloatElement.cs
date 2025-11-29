using UnityEngine;

public class FloatElement : MonoBehaviour
{
    public Vector3 floatOffset = Vector3.up * 10f;
    public float floatSpeed = 1f;
    public float phaseOffset = 0f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition = startPos + floatOffset * Mathf.Sin(Time.time * floatSpeed + phaseOffset);
    }
}
