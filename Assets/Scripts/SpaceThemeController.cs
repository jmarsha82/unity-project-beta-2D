using UnityEngine;

public sealed class SpaceThemeController : MonoBehaviour
{
    public const string BackgroundObjectName = "Space Theme Backdrop";
    public const string DefaultBackgroundResourcePath = "Sprites/StarrySpaceBackground";

    public string backgroundSpriteResourcePath = DefaultBackgroundResourcePath;
    public int sortingOrder = -100;
    public Color cameraBackgroundColor = new Color(0.01f, 0.015f, 0.05f, 1f);

    void Start()
    {
        EnsureSpaceTheme(Camera.main, backgroundSpriteResourcePath, sortingOrder, cameraBackgroundColor);
    }

    public static GameObject EnsureSpaceTheme(Camera cameraToUse)
    {
        return EnsureSpaceTheme(cameraToUse, DefaultBackgroundResourcePath, -100, new Color(0.01f, 0.015f, 0.05f, 1f));
    }

    public static GameObject EnsureSpaceTheme(Camera cameraToUse, string backgroundResourcePath, int sortingOrder, Color cameraBackgroundColor)
    {
        if (cameraToUse == null)
        {
            cameraToUse = Camera.main;
        }

        if (cameraToUse != null)
        {
            cameraToUse.clearFlags = CameraClearFlags.SolidColor;
            cameraToUse.backgroundColor = cameraBackgroundColor;
        }

        GameObject existingBackdrop = GameObject.Find(BackgroundObjectName);
        if (existingBackdrop != null)
        {
            ScaleBackdropToCamera(existingBackdrop, cameraToUse);
            return existingBackdrop;
        }

        Sprite backgroundSprite = Resources.Load<Sprite>(backgroundResourcePath);
        if (backgroundSprite == null)
        {
            return null;
        }

        GameObject backdrop = new GameObject(BackgroundObjectName);
        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = sortingOrder;
        renderer.color = Color.white;

        ScaleBackdropToCamera(backdrop, cameraToUse);
        return backdrop;
    }

    static void ScaleBackdropToCamera(GameObject backdrop, Camera cameraToUse)
    {
        if (backdrop == null || cameraToUse == null || !cameraToUse.orthographic)
        {
            return;
        }

        SpriteRenderer renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        float cameraHeight = cameraToUse.orthographicSize * 2f;
        float cameraWidth = cameraHeight * cameraToUse.aspect;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);
        backdrop.transform.position = new Vector3(cameraToUse.transform.position.x, cameraToUse.transform.position.y, 5f);
        backdrop.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
