using UnityEngine;
using System.Collections;

public class CauldronEffects : MonoBehaviour
{
    [Header("Magical Respawn")]
    public Animator wizardAnimator;

    private SpriteRenderer _renderer;
    private Vector3 _originalPos;
    private Vector3 _originalScale;
    private Color _originalColor;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _originalPos = transform.localPosition;
        _originalScale = transform.localScale;
        if (_renderer != null) _originalColor = _renderer.color;
    }

    /// <summary>
    /// Начать нагрев котла (покраснение и тряска)
    /// </summary>
    [ContextMenu("Test Heating")]
    public void TestHeating() => StartHeating(1.5f);

    public void StartHeating(float duration)
    {
        StartCoroutine(HeatingRoutine(duration));
    }

    private IEnumerator HeatingRoutine(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            // 1. Покраснение
            if (_renderer != null)
                _renderer.color = Color.Lerp(_originalColor, new Color(1f, 0.3f, 0.3f), t);
            
            // 2. Тряска (максимальное отклонение 0.15)
            float shakeMagnitude = t * 0.15f;
            transform.localPosition = _originalPos + (Vector3)Random.insideUnitCircle * shakeMagnitude;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    [ContextMenu("Test Full Sequence")]
    public void TestFullSequence()
    {
        StartHeating(1.5f);
        Invoke(nameof(Explode), 1.5f);
    }

    /// <summary>
    /// Визуальный взрыв без использования префабов
    /// </summary>
    [ContextMenu("Test Explosion")]
    public void Explode()
    {
        StopAllCoroutines();
        
        // Создаем объект системы частиц программно
        GameObject expObj = new GameObject("DynamicExplosion");
        expObj.transform.position = transform.position + Vector3.up * 0.5f;

        ParticleSystem ps = expObj.AddComponent<ParticleSystem>();
        
        // ⭐ КРАДЕМ МАТЕРИАЛ У КОТЛА (точно будет работать)
        var psRenderer = expObj.GetComponent<ParticleSystemRenderer>();
        if (_renderer != null)
        {
            psRenderer.material = _renderer.material;
            psRenderer.sortingLayerName = _renderer.sortingLayerName;
        }
        psRenderer.sortingOrder = 100;

        // Настройка базовых параметров
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.25f); 
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360f); 
        main.startColor = new Color(1f, 0.5f, 0.2f, 0.9f); // ⭐ ОГНЕННЫЙ ОРАНЖЕВЫЙ
        main.gravityModifier = 0.6f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.loop = false;

        // Всплеск (Burst) - больше частиц для густоты
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 100) });

        // 1. Изменение размера со временем (уменьшение в ноль)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // 2. Изменение цвета и прозрачности (плавное исчезновение искр)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.4f, 0f), 0f), // Оранжевый старт
                new GradientColorKey(new Color(1f, 1f, 0.5f), 1f)  // Желтый финиш
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        // 3. Вращение в полете
        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        // 4. Шум (Noise) для хаотичности
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;

        // Форма сферы
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Текстура (если нет спрайта, Unity использует стандартный квадрат, что в пиксель-арте смотрится норм)
        var renderer = expObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        ps.Play();

        // Скрываем котел
        if (_renderer != null) _renderer.enabled = false;

        // ⭐ Восстановление через 0.7 сек (было 2.5)
        Invoke(nameof(ResetVisibleState), 0.7f);
    }

    public void ResetVisibleState()
    {
        if (_renderer != null)
        {
            // ⭐ НЕ включаем рендерер сразу, чтобы он не "телепортировался"
            if (wizardAnimator != null)
            {
                wizardAnimator.SetTrigger("Cast");
            }
            
            StartCoroutine(RespawnRoutine());
        }
        transform.localPosition = _originalPos;
    }

    private IEnumerator RespawnRoutine()
    {
        // ⭐ Увеличил задержку, чтобы маг успел взмахнуть палочкой
        yield return new WaitForSeconds(0.6f);

        if (_renderer != null)
        {
            _renderer.enabled = true;
            transform.localScale = _originalScale; 
            
            Color startColor = _originalColor;
            _renderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f); 

            SpawnPuff();

            float duration = 1.2f; // ⭐ Замедлил появление в 1.5 раза (было 0.8)
            float elapsed = 0;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(0f, startColor.a, t);
                _renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            _renderer.color = startColor;
        }
    }

    private void SpawnPuff()
    {
        GameObject puff = new GameObject("RespawnPuff");
        puff.transform.position = transform.position;
        ParticleSystem ps = puff.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSize = 0.5f;
        main.startColor = Color.white;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 20) });
        ps.Play();
    }
}
