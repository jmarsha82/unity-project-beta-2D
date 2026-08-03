using UnityEngine;

public sealed class LaserProjectile : MonoBehaviour
{
    float damage = 1f;
    float shrinkAmount = 0.18f;
    float spawnTime;
    float lifetime = 1.4f;

    public static LaserProjectile Create(
        Vector3 position,
        Vector2 direction,
        float speed,
        float projectileLifetime,
        float projectileDamage,
        float projectileShrinkAmount,
        Color color)
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
        GameObject laserObject = new GameObject("Player Laser");
        laserObject.transform.position = position;
        laserObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, safeDirection);
        laserObject.transform.localScale = new Vector3(0.12f, 0.65f, 1f);

        SpriteRenderer renderer = laserObject.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = 12;

        Rigidbody2D body = laserObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = safeDirection * speed;

        BoxCollider2D collider = laserObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.45f, 1f);

        LaserProjectile projectile = laserObject.AddComponent<LaserProjectile>();
        projectile.Initialize(projectileLifetime, projectileDamage, projectileShrinkAmount);
        return projectile;
    }

    public void Initialize(float projectileLifetime, float projectileDamage, float projectileShrinkAmount)
    {
        lifetime = Mathf.Max(0.05f, projectileLifetime);
        damage = Mathf.Max(0f, projectileDamage);
        shrinkAmount = Mathf.Max(0f, projectileShrinkAmount);
        spawnTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        float age = Mathf.Clamp01((Time.time - spawnTime) / lifetime);
        transform.localScale = new Vector3(0.12f + age * 0.06f, 0.65f * (1f - age * 0.45f), 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Obstacle obstacle = other.GetComponentInParent<Obstacle>();
        if (obstacle == null)
        {
            return;
        }

        obstacle.ApplyLaserHit(damage, shrinkAmount);
        Destroy(gameObject);
    }
}
