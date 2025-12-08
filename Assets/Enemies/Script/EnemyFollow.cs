using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    [Header("References")]
    private Transform player;
    private Animator anim;
    private EnemyHealthBar healthBar;       // NEW: Reference to health bar

    [Header("UI")]
    public GameObject healthBarPrefab;      // NEW: Assign in Inspector

    [Header("Runtime Stats (Set by EnemyData)")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    private bool isActive = false;
    private float lastAttackTime;

    // Reference to the data that created this enemy
    [HideInInspector]
    public EnemyData enemyData;

    void Start()
    {
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentHealth = maxHealth;

        // NEW: Spawn health bar
        SpawnHealthBar();
    }

    // NEW: Create health bar above enemy head
    void SpawnHealthBar()
    {
        if (healthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            healthBar = healthBarObj.GetComponent<EnemyHealthBar>();

            if (healthBar != null)
            {
                healthBar.SetTarget(transform);
                healthBar.UpdateHealth(currentHealth, maxHealth);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No health bar prefab assigned to enemy!");
        }
        // Play spawn sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemySpawn(transform.position);
        }
    }

    void Update()
    {
        if (!isActive) return;
        if (player == null) return;

        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(player.position.x, 0, player.position.z)
        );
        // 🟥 ATTACK if in range and cooldown ready
        if (distance <= attackRange)
        {
            if (anim != null)
            {
                anim.SetBool("isWalking", false);
                anim.SetBool("isAttacking", true);
            }
            FacePlayer();

            // Attack on cooldown
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
            return;
        }

        // 🟩 FOLLOW PLAYER
        if (anim != null)
        {
            anim.SetBool("isAttacking", false);
            anim.SetBool("isWalking", true);
        }
        MoveTowardsPlayer();
        FacePlayer();
    }

    void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    void FacePlayer()
    {
        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0;
        if (lookPos != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookPos),
                10f * Time.deltaTime
            );
        }
    }

    void PerformAttack()
    {
        // This is called when enemy should deal damage
        Debug.Log($"{(enemyData != null ? enemyData.enemyName : "Enemy")} attacks for {attackDamage} damage!");

        // Example: Damage player
        // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        // if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
    }

    /// <summary>
    /// Initialize enemy with data from ScriptableObject
    /// </summary>
    public void Initialize(EnemyData data)
    {
        enemyData = data;
        moveSpeed = data.moveSpeed;
        attackRange = data.attackRange;
        maxHealth = data.maxHealth;
        currentHealth = data.maxHealth;
        attackDamage = data.attackDamage;
        attackCooldown = data.attackCooldown;

        // Apply custom material if provided
        if (data.skinMaterial != null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material = data.skinMaterial;
            }
        }
    }

    /// <summary>
    /// Take damage and check if dead
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!isActive) return;

        currentHealth -= damage;

        Debug.Log($"{(enemyData != null ? enemyData.enemyName : "Enemy")} took {damage} damage! HP: {currentHealth}/{maxHealth}");
        // Play hit sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyHit(transform.position);
        }
        
        // NEW: Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handle enemy death
    /// </summary>
    void Die()
    {
        isActive = false;
        // Play death sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDeath(transform.position);
        }
        
        // Play death animation if you have one
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        // Award rewards
        if (enemyData != null)
        {
            Debug.Log($"Enemy died! +{enemyData.scoreValue} score, +{enemyData.goldValue} gold");
            // GameManager.Instance.AddScore(enemyData.scoreValue);
            // GameManager.Instance.AddGold(enemyData.goldValue);
        }

        // Notify wave manager
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnEnemyDied(this);
        }

        // NEW: Destroy health bar
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        // Destroy after delay (for death animation)
        Destroy(gameObject, 10f);
    }

    /// <summary>
    /// Activate enemy after spawn animation
    /// </summary>
    public void Activate()
    {
        isActive = true;
    }

    /// <summary>
    /// Check if enemy is alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}

/// <summary>
/// Helper component to destroy an enemy after a delay
/// This runs on a separate GameObject so it won't be affected by disabling the enemy script
/// </summary>
public class DeathTimer : MonoBehaviour
{
    private GameObject targetToDestroy;
    private float countdown;

    public void Initialize(GameObject target, float delay)
    {
        targetToDestroy = target;
        countdown = delay;
        Debug.Log($"⏲️ DeathTimer created for {target.name}, will destroy in {delay}s");
    }

    void Update()
    {
        countdown -= Time.deltaTime;

        if (countdown <= 0f)
        {
            if (targetToDestroy != null)
            {
                Debug.Log($"🗑️ DeathTimer destroying {targetToDestroy.name}");
                Destroy(targetToDestroy);
            }
            Destroy(gameObject); // Destroy this timer too
        }
    }
}