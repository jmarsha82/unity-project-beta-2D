using UnityEngine;

public sealed class PlayerExplosionFragment : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Color startColor;
    float lifetime = 1f;
    float spawnTime;

    public void Initialize(float fragmentLifetime)
    {
        lifetime = Mathf.Max(0.05f, fragmentLifetime);
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }

        spawnTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float age = Mathf.Clamp01((Time.time - spawnTime) / lifetime);
        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, age);
        spriteRenderer.color = color;
        transform.localScale *= 1f + Time.deltaTime * 0.35f;
    }
}
