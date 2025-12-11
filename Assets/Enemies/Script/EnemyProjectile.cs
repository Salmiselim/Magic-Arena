using UnityEngine;

/// <summary>
/// Projectile fired by ranged enemies
/// Attach to projectile prefab (sphere, arrow, fireball, etc.)
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 10;
    public float speed = 15f;
    public float lifetime = 5f;
    public bool destroyOnHit = true;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    public AudioClip flySound; // Looping sound while flying

    [Header("Visual")]
    public TrailRenderer trail;

    private Vector3 direction;
    private AudioSource audioSource;
    private bool hasHit = false;

    void Start()
    {
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);

        // Play fly sound
        if (flySound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = flySound;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.Play();
        }
    }

    void Update()
    {
        // Move forward
        transform.position += direction * speed * Time.deltaTime;
    }

    /// <summary>
    /// Set the direction this projectile should fly
    /// </summary>
    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;

        // Rotate to face direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Check if hit player
        if (other.CompareTag("Player"))
        {
            // Damage player
            // PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(damage);
            // }

            Debug.Log($"💥 Projectile hit player for {damage} damage!");

            HitEffect(other.transform.position);

            if (destroyOnHit)
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }
        // Hit environment/obstacles
        else if (!other.CompareTag("Enemy"))
        {
            HitEffect(transform.position);

            if (destroyOnHit)
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Spawn hit effect and play sound
    /// </summary>
    void HitEffect(Vector3 position)
    {
        // Spawn visual effect
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Play hit sound
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, position);
        }
    }
}