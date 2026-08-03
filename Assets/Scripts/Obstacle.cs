using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSize = 0.5f;
    public float maxSize = 1.5f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float maxSpinSpeed = 10f;

    [Header("Laser Damage")]
    public float laserHitsToDestroy = 4f;
    public float minExplodeSize = 0.28f;
    [Range(0.05f, 0.8f)] public float defaultLaserShrinkAmount = 0.18f;

    [Header("Respawn")]
    public bool spawnReplacementOnDestroy = true;
    public float respawnInset = 1f;
    public float playerAvoidRadius = 2.5f;
    public int respawnPlacementAttempts = 12;
    public float replacementSpawnDelay = 0f;

    [Header("Explosion")]
    public int explosionFragments = 10;
    public float explosionForce = 4.5f;
    public float explosionLifetime = 0.9f;

    [Header("Visual Polish")]
    public bool stylizeAsteroidOnStart = true;
    public bool useAsteroidSpriteTheme = true;
    public string asteroidSpriteResourcePath = "Sprites/Asteroid";
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
    float currentLaserHealth;
    bool isExploding;

    void Start()
    {
        ClearSurfaceDetails();

        float randomSize = Random.Range(minSize, maxSize);
        baseScale = new Vector3(randomSize, randomSize, 1);
        transform.localScale = baseScale;
        currentLaserHealth = Mathf.Max(1f, laserHitsToDestroy);

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            ApplyAsteroidThemeSprite();
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

    public void ApplyLaserHit(float damage, float shrinkAmount)
    {
        if (isExploding)
        {
            return;
        }

        float safeDamage = Mathf.Max(0.1f, damage);
        float safeShrinkAmount = shrinkAmount > 0f ? shrinkAmount : defaultLaserShrinkAmount;
        currentLaserHealth -= safeDamage;

        float scaleMultiplier = Mathf.Clamp01(1f - safeShrinkAmount);
        baseScale = new Vector3(
            Mathf.Max(minExplodeSize, baseScale.x * scaleMultiplier),
            Mathf.Max(minExplodeSize, baseScale.y * scaleMultiplier),
            1f);
        transform.localScale = baseScale;

        if (currentLaserHealth <= 0f || Mathf.Min(baseScale.x, baseScale.y) <= minExplodeSize + 0.001f)
        {
            ExplodeAndRespawn();
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

    void ExplodeAndRespawn()
    {
        if (isExploding)
        {
            return;
        }

        AwardPlayerScore();
        SpawnReplacementObstacle();
        isExploding = true;
        SpawnExplosionFragments();
        Destroy(gameObject);
    }

    void AwardPlayerScore()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.AwardObstacleDestroyed();
        }
    }

    void SpawnReplacementObstacle()
    {
        if (!spawnReplacementOnDestroy)
        {
            return;
        }

        GameObject replacement = Instantiate(gameObject);
        replacement.name = "Obstacle";
        Obstacle replacementObstacle = replacement.GetComponent<Obstacle>();
        if (replacementObstacle != null)
        {
            replacementObstacle.PrepareRespawn(GetRandomInteriorSpawnPosition(), replacementSpawnDelay);
        }
    }

    void PrepareRespawn(Vector3 spawnPosition, float delay)
    {
        isExploding = false;
        currentLaserHealth = Mathf.Max(1f, laserHitsToDestroy);
        transform.position = spawnPosition;
        transform.localScale = Vector3.one;
        ClearSurfaceDetails();

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.simulated = delay <= 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (delay > 0f)
        {
            Invoke(nameof(ActivateRespawnedObstacle), delay);
        }
    }

    void ActivateRespawnedObstacle()
    {
        gameObject.SetActive(true);
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.simulated = true;
        }
    }

    Vector3 GetRandomInteriorSpawnPosition()
    {
        Camera cameraToUse = boundaryCamera != null ? boundaryCamera : Camera.main;
        if (cameraToUse == null)
        {
            return GetFallbackInteriorSpawnPosition();
        }

        Vector3 minBounds = cameraToUse.ViewportToWorldPoint(Vector3.zero);
        Vector3 maxBounds = cameraToUse.ViewportToWorldPoint(Vector3.one);
        float z = transform.position.z;
        float minX = Mathf.Min(minBounds.x, maxBounds.x) + respawnInset;
        float maxX = Mathf.Max(minBounds.x, maxBounds.x) - respawnInset;
        float minY = Mathf.Min(minBounds.y, maxBounds.y) + respawnInset;
        float maxY = Mathf.Max(minBounds.y, maxBounds.y) - respawnInset;

        if (minX > maxX)
        {
            float centerX = (minBounds.x + maxBounds.x) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }
        if (minY > maxY)
        {
            float centerY = (minBounds.y + maxBounds.y) * 0.5f;
            minY = centerY;
            maxY = centerY;
        }

        Transform player = FindPlayerTransform();
        int attempts = Mathf.Max(1, respawnPlacementAttempts);
        Vector3 fallbackPosition = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), z);

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), z);
            fallbackPosition = candidate;
            if (player == null || Vector2.Distance(candidate, player.position) >= playerAvoidRadius)
            {
                return candidate;
            }
        }

        if (player != null)
        {
            Vector2 awayFromPlayer = ((Vector2)fallbackPosition - (Vector2)player.position).normalized;
            if (awayFromPlayer.sqrMagnitude <= 0.01f)
            {
                awayFromPlayer = Random.insideUnitCircle.normalized;
            }

            Vector2 pushedPosition = (Vector2)player.position + awayFromPlayer * playerAvoidRadius;
            fallbackPosition = new Vector3(
                Mathf.Clamp(pushedPosition.x, minX, maxX),
                Mathf.Clamp(pushedPosition.y, minY, maxY),
                z);
        }

        return fallbackPosition;
    }

    Vector3 GetFallbackInteriorSpawnPosition()
    {
        Transform player = FindPlayerTransform();
        Vector2 position = Random.insideUnitCircle * 4f;
        if (player != null && Vector2.Distance(position, player.position) < playerAvoidRadius)
        {
            Vector2 awayFromPlayer = (position - (Vector2)player.position).normalized;
            if (awayFromPlayer.sqrMagnitude <= 0.01f)
            {
                awayFromPlayer = Vector2.up;
            }

            position = (Vector2)player.position + awayFromPlayer * playerAvoidRadius;
        }

        return new Vector3(position.x, position.y, transform.position.z);
    }

    Transform FindPlayerTransform()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player != null ? player.transform : null;
    }

    void SpawnExplosionFragments()
    {
        Sprite sprite = spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite : RuntimeSpriteUtility.WhiteSprite;
        Color color = spriteRenderer != null ? spriteRenderer.color : baseColor;
        int fragmentCount = Mathf.Max(1, explosionFragments);

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = new GameObject("Asteroid Explosion Fragment");
            fragment.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.25f);
            fragment.transform.localScale = Vector3.one * Random.Range(0.07f, 0.2f);
            fragment.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer fragmentRenderer = fragment.AddComponent<SpriteRenderer>();
            fragmentRenderer.sprite = sprite;
            fragmentRenderer.color = Color.Lerp(color, Color.white, Random.Range(0.1f, 0.35f));
            fragmentRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 3 : 3;

            Rigidbody2D fragmentBody = fragment.AddComponent<Rigidbody2D>();
            fragmentBody.gravityScale = 0f;
            fragmentBody.linearDamping = 0.3f;
            fragmentBody.angularVelocity = Random.Range(-540f, 540f);
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = Vector2.up;
            }
            fragmentBody.AddForce(direction * Random.Range(explosionForce * 0.4f, explosionForce), ForceMode2D.Impulse);

            PlayerExplosionFragment fade = fragment.AddComponent<PlayerExplosionFragment>();
            fade.Initialize(explosionLifetime);
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

    void ApplyAsteroidThemeSprite()
    {
        if (!useAsteroidSpriteTheme || string.IsNullOrWhiteSpace(asteroidSpriteResourcePath) || spriteRenderer == null)
        {
            return;
        }

        Sprite asteroidSprite = Resources.Load<Sprite>(asteroidSpriteResourcePath);
        if (asteroidSprite != null)
        {
            spriteRenderer.sprite = asteroidSprite;
        }
    }

    void ClearSurfaceDetails()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Asteroid Surface Detail")
            {
                Destroy(child.gameObject);
            }
        }
    }
}
