using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float thrustForce = 2f;
    public float maxSpeed = 5f;
    public float rotationSpeed = 720f;
    public float idleDrag = 0.35f;
    public GameObject boosterFlame;

    [Header("Controls")]
    public bool allowMouseSteering = true;
    public bool allowKeyboardSteering = true;

    [Header("Boost")]
    public float boostForce = 6f;
    public float boostCooldown = 1.5f;
    public float boostDuration = 0.18f;

    [Header("Shield")]
    public float shieldDuration = 1.25f;
    public float shieldCooldown = 4f;
    public Color shieldColor = new Color(0.15f, 0.85f, 1f, 0.45f);

    [Header("Screen Wrap")]
    public bool wrapAroundCameraBounds = true;
    public Camera boundaryCamera;
    public float wrapMargin = 0.5f;

    [Header("Explosion")]
    public int explosionFragments = 14;
    public float explosionForce = 5.5f;
    public float explosionLifetime = 1.2f;
    public float destroyDelay = 0.05f;

    [Header("Visual Polish")]
    public bool stylizeShipOnStart = true;
    public bool tintBoosterByThrust = true;
    public Color hullColor = new Color(0.08f, 0.25f, 0.95f, 1f);
    public Color accentColor = new Color(0.95f, 0.85f, 0.2f, 1f);
    public Color engineColor = new Color(1f, 0.35f, 0.05f, 1f);

    Rigidbody2D rb;
    Camera cachedCamera;
    SpriteRenderer[] shipRenderers;
    readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    GameObject shieldVisual;
    Vector2 thrustDirection;
    bool isThrusting;
    bool isDestroyed;
    float nextBoostTime;
    float boostEndTime;
    float nextShieldTime;
    float shieldEndTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedCamera = boundaryCamera != null ? boundaryCamera : Camera.main;
        shipRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        CacheRendererColors();

        if (stylizeShipOnStart)
        {
            StylizeShip();
        }

        CreateShieldVisual();
        SetBoosterActive(false);
    }

    void Update()
    {
        if (isDestroyed)
        {
            return;
        }

        ReadInput();
        UpdateRotation();
        UpdateBooster();
        UpdateShieldVisual();
    }

    void FixedUpdate()
    {
        if (isDestroyed || rb == null)
        {
            return;
        }

        ApplyThrust();
        ClampVelocity();
        WrapAroundCamera();
    }

    void ReadInput()
    {
        thrustDirection = Vector2.zero;
        isThrusting = false;

        if (allowKeyboardSteering && Keyboard.current != null)
        {
            Vector2 keyboardDirection = ReadKeyboardDirection();
            if (keyboardDirection.sqrMagnitude > 0.01f)
            {
                thrustDirection = keyboardDirection.normalized;
                isThrusting = true;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryBoost();
            }

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame)
            {
                TryActivateShield();
            }
        }

        if (allowMouseSteering && Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                Camera cameraToUse = cachedCamera != null ? cachedCamera : Camera.main;
                if (cameraToUse != null)
                {
                    Vector3 mousePosition = cameraToUse.ScreenToWorldPoint(Mouse.current.position.value);
                    thrustDirection = ((Vector2)(mousePosition - transform.position)).normalized;
                    isThrusting = true;
                }
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                TryBoost();
            }

            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                TryActivateShield();
            }
        }
    }

    Vector2 ReadKeyboardDirection()
    {
        Vector2 direction = Vector2.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            direction.y += 1f;
        }
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            direction.y -= 1f;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            direction.x += 1f;
        }
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            direction.x -= 1f;
        }

        return direction;
    }

    void UpdateRotation()
    {
        if (thrustDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, thrustDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void ApplyThrust()
    {
        bool boosting = Time.time < boostEndTime;
        if (isThrusting || boosting)
        {
            Vector2 direction = thrustDirection.sqrMagnitude > 0.01f ? thrustDirection : transform.up;
            float force = thrustForce * (boosting ? 1.8f : 1f);
            rb.AddForce(direction * force, ForceMode2D.Force);
            rb.linearDamping = 0f;
        }
        else
        {
            rb.linearDamping = idleDrag;
        }
    }

    void ClampVelocity()
    {
        float currentMaxSpeed = Time.time < boostEndTime ? maxSpeed * 1.35f : maxSpeed;
        if (rb.linearVelocity.magnitude > currentMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }
    }

    void TryBoost()
    {
        if (rb == null || Time.time < nextBoostTime)
        {
            return;
        }

        Vector2 direction = thrustDirection.sqrMagnitude > 0.01f ? thrustDirection : transform.up;
        rb.AddForce(direction.normalized * boostForce, ForceMode2D.Impulse);
        boostEndTime = Time.time + boostDuration;
        nextBoostTime = Time.time + boostCooldown;
        SetBoosterActive(true);
    }

    void TryActivateShield()
    {
        if (Time.time < nextShieldTime)
        {
            return;
        }

        shieldEndTime = Time.time + shieldDuration;
        nextShieldTime = Time.time + shieldCooldown;
        UpdateShieldVisual();
    }

    void UpdateBooster()
    {
        bool boosterActive = isThrusting || Time.time < boostEndTime;
        SetBoosterActive(boosterActive);

        if (!tintBoosterByThrust || boosterFlame == null)
        {
            return;
        }

        SpriteRenderer boosterRenderer = boosterFlame.GetComponent<SpriteRenderer>();
        if (boosterRenderer != null)
        {
            boosterRenderer.color = Time.time < boostEndTime ? Color.white : engineColor;
        }
    }

    void UpdateShieldVisual()
    {
        if (shieldVisual == null)
        {
            return;
        }

        bool shieldActive = IsShieldActive();
        shieldVisual.SetActive(shieldActive);
        if (shieldActive)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.08f;
            shieldVisual.transform.localScale = Vector3.one * pulse;
        }
    }

    bool IsShieldActive()
    {
        return Time.time < shieldEndTime;
    }

    void WrapAroundCamera()
    {
        if (!wrapAroundCameraBounds)
        {
            return;
        }

        Camera cameraToUse = cachedCamera != null ? cachedCamera : Camera.main;
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed || IsShieldActive())
        {
            return;
        }

        ExplodeAndDestroy();
    }

    void ExplodeAndDestroy()
    {
        isDestroyed = true;
        SetBoosterActive(false);
        SpawnExplosionFragments();

        foreach (SpriteRenderer renderer in shipRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }

        if (rb != null)
        {
            rb.simulated = false;
        }

        Destroy(gameObject, destroyDelay);
    }

    void SpawnExplosionFragments()
    {
        SpriteRenderer sourceRenderer = FindBestFragmentSource();
        Sprite sourceSprite = sourceRenderer != null ? sourceRenderer.sprite : null;
        Color sourceColor = sourceRenderer != null ? sourceRenderer.color : engineColor;
        int fragmentCount = Mathf.Max(1, explosionFragments);

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = new GameObject("Player Explosion Fragment");
            fragment.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.35f);
            fragment.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);

            SpriteRenderer fragmentRenderer = fragment.AddComponent<SpriteRenderer>();
            fragmentRenderer.sprite = sourceSprite;
            fragmentRenderer.color = Color.Lerp(sourceColor, Random.value > 0.5f ? engineColor : accentColor, 0.7f);
            fragmentRenderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder + 2 : 2;

            Rigidbody2D fragmentBody = fragment.AddComponent<Rigidbody2D>();
            fragmentBody.gravityScale = 0f;
            fragmentBody.angularVelocity = Random.Range(-720f, 720f);
            fragmentBody.linearDamping = 0.25f;
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = Vector2.up;
            }
            fragmentBody.AddForce(direction * Random.Range(explosionForce * 0.45f, explosionForce), ForceMode2D.Impulse);

            PlayerExplosionFragment fade = fragment.AddComponent<PlayerExplosionFragment>();
            fade.Initialize(explosionLifetime);
        }
    }

    SpriteRenderer FindBestFragmentSource()
    {
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            return null;
        }

        foreach (SpriteRenderer renderer in shipRenderers)
        {
            if (renderer != null && renderer.sprite != null && renderer.gameObject != boosterFlame)
            {
                return renderer;
            }
        }

        return shipRenderers[0];
    }

    void CacheRendererColors()
    {
        originalColors.Clear();
        if (shipRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in shipRenderers)
        {
            if (renderer != null)
            {
                originalColors[renderer] = renderer.color;
            }
        }
    }

    void StylizeShip()
    {
        if (shipRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in shipRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            if (renderer.gameObject == boosterFlame || objectName.Contains("booster") || objectName.Contains("flame"))
            {
                renderer.color = engineColor;
            }
            else if (objectName.Contains("rudder") || objectName.Contains("command"))
            {
                renderer.color = accentColor;
            }
            else
            {
                renderer.color = Color.Lerp(hullColor, originalColors.TryGetValue(renderer, out Color original) ? original : Color.white, 0.25f);
            }
        }
    }

    void CreateShieldVisual()
    {
        shieldVisual = new GameObject("Shield Visual");
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one;

        SpriteRenderer shieldRenderer = shieldVisual.AddComponent<SpriteRenderer>();
        shieldRenderer.sprite = FindBestFragmentSource()?.sprite;
        shieldRenderer.color = shieldColor;
        shieldRenderer.sortingOrder = 5;
        shieldVisual.SetActive(false);
    }

    void SetBoosterActive(bool active)
    {
        if (boosterFlame != null && boosterFlame.activeSelf != active)
        {
            boosterFlame.SetActive(active);
        }
    }
}
