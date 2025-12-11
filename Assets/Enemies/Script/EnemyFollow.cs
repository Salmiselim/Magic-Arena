using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    [Header("References")]
    private Transform player;
    private Animator anim;
    private EnemyHealthBar healthBar;
    private AudioSource audioSource;

    [Header("UI")]
    public GameObject healthBarPrefab;

    [Header("Runtime Stats (Set by EnemyData)")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    private bool isActive = false;
    private float lastAttackTime;
    private float nextWalkSoundTime;

    // Reference to the data that created this enemy
    [HideInInspector]
    public EnemyData enemyData;

    void Start()
    {
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentHealth = maxHealth;

        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 20f;
        audioSource.volume = 0.8f; // Ensure volume is reasonable

        // Spawn health bar (but DON'T play spawn sound yet)
        SpawnHealthBar();
    }

    // Create health bar above enemy head
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

        // DON'T play spawn sound here - it will be played after Initialize()
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

        // Play footstep sounds periodically
        if (Time.time >= nextWalkSoundTime && enemyData != null && enemyData.walkSounds != null && enemyData.walkSounds.Length > 0)
        {
            PlayRandomSound(enemyData.walkSounds, 0.5f);
            nextWalkSoundTime = Time.time + enemyData.walkSoundInterval;
        }
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

        // Play attack sound
        if (enemyData != null && enemyData.attackSounds != null && enemyData.attackSounds.Length > 0)
        {
            PlayRandomSound(enemyData.attackSounds, 0.7f);
        }

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

        // NOW play spawn sound after enemyData is set
        Debug.Log($"🔊 Playing spawn sound for {data.enemyName}");
        PlaySound(data.spawnSound);
    }

    /// <summary>
    /// Take damage and check if dead
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!isActive) return;

        currentHealth -= damage;

        Debug.Log($"{(enemyData != null ? enemyData.enemyName : "Enemy")} took {damage} damage! HP: {currentHealth}/{maxHealth}");

        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        // Play hit sound
        if (enemyData != null && enemyData.hitSounds != null && enemyData.hitSounds.Length > 0)
        {
            PlayRandomSound(enemyData.hitSounds);
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

        // Play death animation
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        // Play death sound
        PlaySound(enemyData?.deathSound);

        // Award rewards
        if (enemyData != null)
        {
            Debug.Log($"Enemy died! +{enemyData.scoreValue} score, +{enemyData.goldValue} gold");
        }

        // Notify wave manager
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnEnemyDied(this);
        }

        // Destroy health bar
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

    /// <summary>
    /// Play a single sound
    /// </summary>
    void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.Log($"⚠️ {gameObject.name}: Tried to play sound but clip is NULL");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: AudioSource is NULL!");
            return;
        }

        Debug.Log($"🔊 {gameObject.name} playing sound: {clip.name} at volume {volumeScale}");
        audioSource.PlayOneShot(clip, volumeScale);
    }

    /// <summary>
    /// Play random sound from array
    /// </summary>
    void PlayRandomSound(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.Log($"⚠️ {gameObject.name}: Sound array is empty or NULL");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: AudioSource is NULL!");
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            Debug.Log($"🔊 {gameObject.name} playing random sound: {clip.name}");
            audioSource.PlayOneShot(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: Selected clip from array is NULL!");
        }
    }
}

/// <summary>
/// Helper component to destroy an enemy after a delay
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
            Destroy(gameObject);
        }
    }
}