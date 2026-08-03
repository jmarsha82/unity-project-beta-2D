using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 1.5f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float maxSpinSpeed = 10f;

    [Header("Visual Polish")]
    public bool stylizeAsteroidOnStart = true;
    public int surfaceDetailCount = 4;
    public Color[] asteroidPalette =
    {
        new Color(0.48f, 0.42f, 0.38f, 1f),
        new Color(0.62f, 0.45f, 0.28f, 1f),
        new Color(0.35f, 0.52f, 0.58f, 1f),
        new Color(0.54f, 0.34f, 0.62f, 1f)
    };
    public Color surfaceShadowColor = new Color(0.12f, 0.09f, 0.08f, 0.55f);

    [Header("Pulse")]
    public bool pulseScale = true;
    [Range(0f, 0.5f)] public float pulseAmount = 0.08f;
    public float pulseSpeed = 2f;

    [Header("Screen Wrap")]
    public bool wrapAroundCameraBounds = true;
    public Camera boundaryCamera;
    public float wrapMargin = 0.5f;

    [Header("Lifetime")]
    public float lifetime = 0f;
    public float fadeOutDuration = 0.75f;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Vector3 baseScale;
    Color baseColor;
    float spawnTime;
    float destroyTime;

    void Start()
    {
        float randomSize = Random.Range(minSize, maxSize);
        baseScale = new Vector3(randomSize, randomSize, 1);
        transform.localScale = baseScale;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            if (stylizeAsteroidOnStart)
            {
                StylizeAsteroid();
            }
        }

        spawnTime = Time.time;
        destroyTime = lifetime > 0f ? spawnTime + lifetime : 0f;

        float randomSpeed = Random.Range(minSpeed, maxSpeed) / randomSize;
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection.sqrMagnitude <= 0.01f)
        {
            randomDirection = Vector2.up;
        }

        if (rb != null)
        {
            rb.AddForce(randomDirection * randomSpeed);

            float randomTorque = Random.Range(-maxSpinSpeed, maxSpinSpeed);
            rb.AddTorque(randomTorque);
        }
    }

    void Update()
    {
        Pulse();
        WrapAroundCamera();
        FadeAndExpire();
    }

    void Pulse()
    {
        if (!pulseScale || pulseAmount <= 0f || pulseSpeed <= 0f)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin((Time.time - spawnTime) * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
        transform.localScale = baseScale * pulse;
    }

    void WrapAroundCamera()
    {
        if (!wrapAroundCameraBounds)
        {
            return;
        }

        Camera cameraToUse = boundaryCamera != null ? boundaryCamera : Camera.main;
        if (cameraToUse == null)
        {
            return;
        }

        Vector3 minBounds = cameraToUse.ViewportToWorldPoint(Vector3.zero);
        Vector3 maxBounds = cameraToUse.ViewportToWorldPoint(Vector3.one);
        Vector3 position = transform.position;
        float originalZ = position.z;

        if (position.x < minBounds.x - wrapMargin)
        {
            position.x = maxBounds.x + wrapMargin;
        }
        else if (position.x > maxBounds.x + wrapMargin)
        {
            position.x = minBounds.x - wrapMargin;
        }

        if (position.y < minBounds.y - wrapMargin)
        {
            position.y = maxBounds.y + wrapMargin;
        }
        else if (position.y > maxBounds.y + wrapMargin)
        {
            position.y = minBounds.y - wrapMargin;
        }

        position.z = originalZ;
        transform.position = position;
    }

    void FadeAndExpire()
    {
        if (lifetime <= 0f)
        {
            return;
        }

        float remainingTime = destroyTime - Time.time;
        if (remainingTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null && fadeOutDuration > 0f)
        {
            Color color = baseColor;
            color.a = baseColor.a * Mathf.Clamp01(remainingTime / fadeOutDuration);
            spriteRenderer.color = color;
        }
    }

    void StylizeAsteroid()
    {
        if (asteroidPalette != null && asteroidPalette.Length > 0)
        {
            baseColor = asteroidPalette[Random.Range(0, asteroidPalette.Length)];
            spriteRenderer.color = baseColor;
        }

        int detailsToCreate = Mathf.Max(0, surfaceDetailCount);
        for (int i = 0; i < detailsToCreate; i++)
        {
            GameObject detail = new GameObject("Asteroid Surface Detail");
            detail.transform.SetParent(transform, false);
            detail.transform.localPosition = Random.insideUnitCircle * 0.28f;
            float detailScale = Random.Range(0.12f, 0.28f);
            detail.transform.localScale = new Vector3(detailScale, detailScale * Random.Range(0.55f, 0.85f), 1f);
            detail.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer detailRenderer = detail.AddComponent<SpriteRenderer>();
            detailRenderer.sprite = spriteRenderer.sprite;
            detailRenderer.color = surfaceShadowColor;
            detailRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }
}
