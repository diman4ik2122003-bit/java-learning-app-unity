using UnityEngine;

public class GoalAnimation : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    
    private Vector3 startScale;
    
    void Start()
    {
        startScale = transform.localScale;
    }
    
    void Update()
    {
        // вращение
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // пульсация
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = startScale * scale;
    }
}
