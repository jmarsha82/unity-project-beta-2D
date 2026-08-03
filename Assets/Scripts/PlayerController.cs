using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

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
    public InputAction moveForward;
    public InputAction lookPosition;
    public InputAction boostAction;
    public InputAction fireLaserAction;
    public InputAction activateShieldAction;
    public bool showMobileControlButtons = true;

    [Header("Boost")]
    public float boostForce = 6f;
    public float boostCooldown = 1.5f;
    public float boostDuration = 0.18f;

    [Header("Shield")]
    public float shieldDuration = 1.25f;
    public float shieldCooldown = 4f;
    public Color shieldColor = new Color(0.15f, 0.85f, 1f, 0.45f);

    [Header("Lasers")]
    public bool canFireLasers = true;
    public float laserSpeed = 14f;
    public float laserLifetime = 1.4f;
    public float laserCooldown = 0.18f;
    public float laserDamage = 1f;
    public float laserShrinkAmount = 0.18f;
    public float laserSpawnOffset = 0.9f;
    public Color laserColor = new Color(1f, 0.1f, 0.05f, 1f);

    [Header("Screen Wrap")]
    public bool wrapAroundCameraBounds = true;
    public Camera boundaryCamera;
    public float wrapMargin = 0.5f;

    [Header("Explosion")]
    public GameObject explosionEffect;
    public GameObject bounceEffectPrefab;
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

    [Header("Space Theme")]
    public bool useRocketSpriteTheme = true;
    public string rocketSpriteResourcePath = "Sprites/RocketShip";
    public Vector3 rocketSpriteScale = new Vector3(1.35f, 1.35f, 1f);
    public int rocketSpriteSortingOrder = 4;
    public bool hideOriginalShipRenderersWithRocket = true;

    [Header("Score and Time")]
    public float elapsedTime = 0f;
    public float score = 0f;
    public float scoreMultiplier = 10f;
    public int obstacleDestroyScore = 5;
    public string highScorePlayerPrefsKey = "HighScore";
    public int highScore = 0;
    public UIDocument uiDocument;
    public GameObject borderParent;

    Rigidbody2D rb;
    Camera cachedCamera;
    SpriteRenderer[] shipRenderers;
    readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    Label scoreText;
    Label highScoreText;
    Button restartButton;
    VisualElement mobileControls;
    SpriteRenderer rocketThemeRenderer;
    GameObject shieldVisual;
    Vector2 thrustDirection;
    bool isThrusting;
    bool isDestroyed;
    int bonusScore;
    float nextBoostTime;
    float boostEndTime;
    float nextShieldTime;
    float shieldEndTime;
    float nextLaserTime;

    void Start()
    {
        InitializeMobileInputActions();
        EnableMobileInputActions();
        InitializeComponents();
        SpaceThemeController.EnsureSpaceTheme(cachedCamera);
        InitializeVisuals();
        InitializeScoreUi();
    }

    void OnDisable()
    {
        DisableMobileInputActions();
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
        UpdateScore(Time.deltaTime);
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

            if (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            {
                TryFireLaser();
            }

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame)
            {
                TryActivateShield();
            }
        }

        ReadMobileActionButtons();

        if (allowMouseSteering)
        {
            if (moveForward != null && moveForward.IsPressed())
            {
                Camera cameraToUse = cachedCamera != null ? cachedCamera : Camera.main;
                if (cameraToUse != null)
                {
                    Vector3 mousePosition = cameraToUse.ScreenToWorldPoint(lookPosition.ReadValue<Vector2>());
                    thrustDirection = ((Vector2)(mousePosition - transform.position)).normalized;
                    isThrusting = true;
                }
            }

            if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
            {
                TryActivateShield();
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                TryFireLaser();
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

    void TryFireLaser()
    {
        if (!canFireLasers || Time.time < nextLaserTime)
        {
            return;
        }

        Vector2 fireDirection = thrustDirection.sqrMagnitude > 0.01f ? thrustDirection.normalized : (Vector2)transform.up;
        Vector3 spawnPosition = transform.position + (Vector3)(fireDirection * laserSpawnOffset);
        LaserProjectile.Create(spawnPosition, fireDirection, laserSpeed, laserLifetime, laserDamage, laserShrinkAmount, laserColor);
        nextLaserTime = Time.time + laserCooldown;
    }

    public void AwardObstacleDestroyed()
    {
        AddScore(obstacleDestroyScore);
    }

    public void AddScore(int points)
    {
        bonusScore += Mathf.Max(0, points);
        RefreshScore();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedCamera = boundaryCamera != null ? boundaryCamera : Camera.main;
        shipRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        CacheRendererColors();
    }

    void InitializeMobileInputActions()
    {
        if (moveForward == null)
        {
            moveForward = new InputAction("Move Forward", InputActionType.Button, "<Pointer>/press");
        }

        if (lookPosition == null)
        {
            lookPosition = new InputAction("Look Position", InputActionType.Value, "<Pointer>/position");
        }

        if (boostAction == null)
        {
            boostAction = new InputAction("Boost", InputActionType.Button);
        }

        if (fireLaserAction == null)
        {
            fireLaserAction = new InputAction("Fire Laser", InputActionType.Button);
        }

        if (activateShieldAction == null)
        {
            activateShieldAction = new InputAction("Activate Shield", InputActionType.Button);
        }
    }

    void EnableMobileInputActions()
    {
        moveForward.Enable();
        lookPosition.Enable();
        boostAction.Enable();
        fireLaserAction.Enable();
        activateShieldAction.Enable();
    }

    void DisableMobileInputActions()
    {
        moveForward?.Disable();
        lookPosition?.Disable();
        boostAction?.Disable();
        fireLaserAction?.Disable();
        activateShieldAction?.Disable();
    }

    void ReadMobileActionButtons()
    {
        if (boostAction != null && boostAction.WasPressedThisFrame())
        {
            TryBoost();
        }

        if (fireLaserAction != null && fireLaserAction.WasPressedThisFrame())
        {
            TryFireLaser();
        }

        if (activateShieldAction != null && activateShieldAction.WasPressedThisFrame())
        {
            TryActivateShield();
        }
    }

    void InitializeVisuals()
    {
        if (stylizeShipOnStart)
        {
            StylizeShip();
        }

        ApplyRocketTheme();
        CreateShieldVisual();
        SetBoosterActive(false);
    }

    void ApplyRocketTheme()
    {
        if (!useRocketSpriteTheme || string.IsNullOrWhiteSpace(rocketSpriteResourcePath))
        {
            return;
        }

        Sprite rocketSprite = Resources.Load<Sprite>(rocketSpriteResourcePath);
        if (rocketSprite == null)
        {
            return;
        }

        if (rocketThemeRenderer == null)
        {
            Transform existingRocket = transform.Find("Rocket Theme Sprite");
            rocketThemeRenderer = existingRocket != null
                ? existingRocket.GetComponent<SpriteRenderer>()
                : null;

            if (rocketThemeRenderer == null)
            {
                GameObject rocketVisual = new GameObject("Rocket Theme Sprite");
                rocketVisual.transform.SetParent(transform, false);
                rocketThemeRenderer = rocketVisual.AddComponent<SpriteRenderer>();
            }
        }

        rocketThemeRenderer.sprite = rocketSprite;
        rocketThemeRenderer.color = Color.white;
        rocketThemeRenderer.sortingOrder = rocketSpriteSortingOrder;
        rocketThemeRenderer.transform.localPosition = Vector3.zero;
        rocketThemeRenderer.transform.localRotation = Quaternion.identity;
        rocketThemeRenderer.transform.localScale = rocketSpriteScale;
        rocketThemeRenderer.enabled = true;

        if (hideOriginalShipRenderersWithRocket && shipRenderers != null)
        {
            foreach (SpriteRenderer renderer in shipRenderers)
            {
                if (renderer != null && renderer != rocketThemeRenderer && renderer.gameObject != boosterFlame)
                {
                    renderer.enabled = false;
                }
            }
        }

        shipRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    void InitializeScoreUi()
    {
        if (!PlayerPrefs.HasKey(highScorePlayerPrefsKey))
        {
            PlayerPrefs.SetInt(highScorePlayerPrefsKey, 0);
            PlayerPrefs.Save();
        }

        highScore = PlayerPrefs.GetInt(highScorePlayerPrefsKey, 0);

        VisualElement root = uiDocument != null ? uiDocument.rootVisualElement : null;
        scoreText = root != null ? root.Q<Label>("ScoreLabel") : null;
        highScoreText = root != null ? root.Q<Label>("HighScoreLabel") : null;
        restartButton = root != null ? root.Q<Button>("RestartButton") : null;
        if (restartButton != null)
        {
            restartButton.style.display = DisplayStyle.None;
            restartButton.clicked -= ReloadScene;
            restartButton.clicked += ReloadScene;
        }

        CreateMobileControlButtons(root);

        RefreshScore();
    }

    void CreateMobileControlButtons(VisualElement root)
    {
        if (!showMobileControlButtons || root == null || mobileControls != null)
        {
            return;
        }

        mobileControls = new VisualElement
        {
            name = "MobileControlButtons"
        };
        mobileControls.style.position = Position.Absolute;
        mobileControls.style.right = 16;
        mobileControls.style.bottom = 16;
        mobileControls.style.flexDirection = FlexDirection.Row;

        Button boostButton = CreateMobileButton("Boost", TryBoost);
        Button laserButton = CreateMobileButton("Laser", TryFireLaser);
        Button shieldButton = CreateMobileButton("Shield", TryActivateShield);
        laserButton.style.marginLeft = 10;
        shieldButton.style.marginLeft = 10;

        mobileControls.Add(boostButton);
        mobileControls.Add(laserButton);
        mobileControls.Add(shieldButton);
        root.Add(mobileControls);
    }

    Button CreateMobileButton(string text, System.Action action)
    {
        Button button = new Button(action)
        {
            text = text
        };
        button.name = "Mobile" + text + "Button";
        button.style.width = 82;
        button.style.height = 56;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.fontSize = 14;
        button.style.color = Color.white;
        button.style.backgroundColor = new Color(0.08f, 0.12f, 0.22f, 0.82f);
        button.style.borderTopColor = accentColor;
        button.style.borderRightColor = accentColor;
        button.style.borderBottomColor = accentColor;
        button.style.borderLeftColor = accentColor;
        button.style.borderTopWidth = 2;
        button.style.borderRightWidth = 2;
        button.style.borderBottomWidth = 2;
        button.style.borderLeftWidth = 2;
        return button;
    }

    void UpdateScore(float deltaTime)
    {
        elapsedTime += Mathf.Max(0f, deltaTime);
        RefreshScore();
    }

    void RefreshScore()
    {
        score = Mathf.FloorToInt(elapsedTime * scoreMultiplier) + bonusScore;
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        RefreshHighScoreLabel();
    }

    void RefreshHighScoreLabel()
    {
        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }
    }

    void SaveHighScoreIfNeeded()
    {
        int currentScore = Mathf.FloorToInt(score);
        if (currentScore <= highScore)
        {
            return;
        }

        highScore = currentScore;
        PlayerPrefs.SetInt(highScorePlayerPrefsKey, highScore);
        PlayerPrefs.Save();
        RefreshHighScoreLabel();
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

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        ExplodeAndDestroy();
        if (borderParent != null)
        {
            borderParent.SetActive(false);
        }
        if (restartButton != null)
        {
            restartButton.style.display = DisplayStyle.Flex;
        }

        if (bounceEffectPrefab != null && collision.contactCount > 0)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            GameObject bounceEffect = Instantiate(bounceEffectPrefab, contactPoint, Quaternion.identity);

            Destroy(bounceEffect, 1f);
        }
    }

    void ExplodeAndDestroy()
    {
        isDestroyed = true;
        SaveHighScoreIfNeeded();
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
        if (rocketThemeRenderer != null && rocketThemeRenderer.sprite != null)
        {
            return rocketThemeRenderer;
        }

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

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
