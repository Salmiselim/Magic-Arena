using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    public EnemySpawner spawner;
    public Transform player;

    [Header("Level Enemies - MUST BE SIZE 3")]
    public EnemyData easyEnemy;     // Assign Level1_Easy here
    public EnemyData mediumEnemy;   // Assign Level1_Medium here
    public EnemyData hardEnemy;     // Assign Level1_Hard here

    [Header("Wave Settings")]
    public float spawnRadius = 15f;
    public float minSpawnDistance = 8f;
    public float timeBetweenWaves = 5f;

    [Header("Debug Info - READ ONLY")]
    public int currentWave = 0;         // 0=Easy, 1=Medium, 2=Hard
    public int totalEnemiesInWave = 0;
    public int aliveEnemies = 0;
    public bool waveActive = false;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    void Start()
    {
        Debug.Log("=== WAVE MANAGER START ===");

        // Auto-find references
        if (spawner == null)
        {
            spawner = FindObjectOfType<EnemySpawner>();
            Debug.Log("Auto-found EnemySpawner: " + (spawner != null));
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("Auto-found Player: " + player.name);
            }
        }

        // Validate
        if (easyEnemy == null || mediumEnemy == null || hardEnemy == null)
        {
            Debug.LogError("❌ WaveManager: You must assign all 3 enemy types in Inspector!");
            return;
        }

        if (spawner == null)
        {
            Debug.LogError("❌ WaveManager: No EnemySpawner found!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("❌ WaveManager: No Player found! Make sure player has 'Player' tag.");
            return;
        }

        Debug.Log("✅ All references valid. Starting first wave in 3 seconds...");

        // Start first wave
        Invoke("StartNextWave", 3f);
    }

    void StartNextWave()
    {
        if (currentWave >= 3)
        {
            Debug.Log("🎉 ALL WAVES COMPLETE!");
            return;
        }

        waveActive = true;

        // Get current wave data
        EnemyData data = GetCurrentWaveData();

        Debug.Log($"🌊 WAVE {currentWave + 1} START: {data.enemyName}");
        Debug.Log($"   Spawning {data.spawnCount} enemies");

        totalEnemiesInWave = data.spawnCount;
        aliveEnemies = 0;

        // Spawn the wave
        StartCoroutine(SpawnWaveCoroutine(data));
    }

    EnemyData GetCurrentWaveData()
    {
        switch (currentWave)
        {
            case 0: return easyEnemy;
            case 1: return mediumEnemy;
            case 2: return hardEnemy;
            default: return easyEnemy;
        }
    }

    IEnumerator SpawnWaveCoroutine(EnemyData data)
    {
        for (int i = 0; i < data.spawnCount; i++)
        {
            // Get random spawn position
            Vector3 spawnPos = GetRandomSpawnPosition();

            Debug.Log($"Spawning enemy {i + 1}/{data.spawnCount} at {spawnPos}");

            // FIXED: Wait for this enemy to fully spawn before continuing
            yield return StartCoroutine(SpawnSingleEnemy(spawnPos, data));

            // Wait before next spawn
            yield return new WaitForSeconds(data.spawnDelay);
        }

        Debug.Log($"All {data.spawnCount} enemies spawned for this wave!");
    }

    IEnumerator SpawnSingleEnemy(Vector3 position, EnemyData data)
    {
        Debug.Log($"🔵 SpawnSingleEnemy START for {data.enemyName} at {position}");

        GameObject spawnedEnemy = null;
        bool callbackCalled = false;

        // Spawn with portal animation
        yield return StartCoroutine(spawner.SpawnSequenceWithData(
            position,
            data,
            (enemyObj, enemyData) => {
                spawnedEnemy = enemyObj;
                callbackCalled = true;
                Debug.Log($"🟢 Spawn callback received! Enemy: {(enemyObj != null ? enemyObj.name : "NULL")}");
            }
        ));

        Debug.Log($"🟡 After SpawnSequenceWithData - Callback called: {callbackCalled}, Enemy: {(spawnedEnemy != null ? spawnedEnemy.name : "NULL")}");

        if (spawnedEnemy != null)
        {
            // Make sure it has Enemy tag
            if (spawnedEnemy.tag != "Enemy")
            {
                Debug.LogWarning($"Enemy spawned without 'Enemy' tag! Setting it now.");
                spawnedEnemy.tag = "Enemy";
            }

            // Try to initialize - check for BOTH EnemyFollow AND EnemyRanged
            EnemyFollow meleeEnemy = spawnedEnemy.GetComponent<EnemyFollow>();
            EnemyRanged rangedEnemy = spawnedEnemy.GetComponent<EnemyRanged>();

            Debug.Log($"🔍 Components check - Melee: {meleeEnemy != null}, Ranged: {rangedEnemy != null}");

            bool initialized = false;

            if (meleeEnemy != null)
            {
                // It's a melee enemy
                Debug.Log($"⚔️ Initializing as MELEE enemy...");
                meleeEnemy.Initialize(data);
                initialized = true;
                Debug.Log($"✅ Melee enemy initialized: {spawnedEnemy.name}");
            }
            else if (rangedEnemy != null)
            {
                // It's a ranged enemy
                Debug.Log($"🏹 Initializing as RANGED enemy...");
                rangedEnemy.Initialize(data);
                initialized = true;
                Debug.Log($"✅ Ranged enemy initialized: {spawnedEnemy.name}");
            }
            else
            {
                // Neither script found - this is an error
                Debug.LogError($"❌ Enemy has NEITHER EnemyFollow NOR EnemyRanged script! GameObject: {spawnedEnemy.name}");
            }

            if (initialized)
            {
                // Track it
                spawnedEnemies.Add(spawnedEnemy);
                aliveEnemies++;

                Debug.Log($"✅ Enemy spawned and tracked! Name: {spawnedEnemy.name}, Tag: {spawnedEnemy.tag}, Alive: {aliveEnemies}/{totalEnemiesInWave}");
            }
        }
        else
        {
            Debug.LogError("❌ Enemy failed to spawn - spawnedEnemy is NULL!");
        }

        Debug.Log($"🔵 SpawnSingleEnemy END");
    }

    Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, spawnRadius);

        Vector3 spawnPos = player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            0,
            Mathf.Sin(angle) * distance
        );

        spawnPos.y = 0;
        return spawnPos;
    }

    // Main method that accepts GameObject (works for both enemy types)
    public void OnEnemyDied(GameObject enemyObject)
    {
        Debug.Log($"📢 WaveManager.OnEnemyDied called for {(enemyObject != null ? enemyObject.name : "NULL")}");

        // Remove from tracking
        if (enemyObject != null)
        {
            spawnedEnemies.Remove(enemyObject);
            Debug.Log($"  ✅ Removed from tracking list");
        }

        aliveEnemies--;

        Debug.Log($"  📊 Remaining alive: {aliveEnemies}/{totalEnemiesInWave}");

        // Check if wave complete
        if (aliveEnemies <= 0 && waveActive)
        {
            Debug.Log($"  🎉 Wave complete condition met!");
            WaveComplete();
        }
    }

    // Overload for EnemyFollow backward compatibility
    public void OnEnemyDied(EnemyFollow enemy)
    {
        if (enemy != null)
        {
            OnEnemyDied(enemy.gameObject);
        }
    }

    void WaveComplete()
    {
        waveActive = false;
        currentWave++;

        Debug.Log($"✅✅✅ WAVE {currentWave} COMPLETE! ✅✅✅");

        if (currentWave >= 3)
        {
            Debug.Log("🎉🎉🎉 ALL WAVES COMPLETE! 🎉🎉🎉");

            // Load next level
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.CompleteLevel();
            }
            else
            {
                Debug.LogWarning("No LevelManager found! Add one to load next level.");
            }
        }
        else
        {
            Debug.Log($"Next wave starts in {timeBetweenWaves} seconds...");
            Invoke("StartNextWave", timeBetweenWaves);
        }
    }

    void Update()
    {
        // Only use New Input System
        if (Keyboard.current != null)
        {
            // Press N to kill all enemies
            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                Debug.Log("⏭️ N PRESSED - KILLING ALL ENEMIES");
                NuclearKillAll();
            }

            // Press H to damage all enemies (50 damage)
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                Debug.Log("💥 H PRESSED - DAMAGING ALL ENEMIES");
                DamageAllEnemies(50);
            }

            // Press J to damage all enemies (10 damage)
            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                Debug.Log("💥 J PRESSED - SMALL DAMAGE TO ALL ENEMIES");
                DamageAllEnemies(10);
            }

            // Press I for debug info
            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                Debug.Log("=== DEBUG INFO ===");
                Debug.Log($"Current Wave: {currentWave + 1}/3");
                Debug.Log($"Wave Active: {waveActive}");
                Debug.Log($"Alive Enemies: {aliveEnemies}/{totalEnemiesInWave}");
                Debug.Log($"Spawned List Count: {spawnedEnemies.Count}");
                Debug.Log($"Easy Enemy: {(easyEnemy != null ? easyEnemy.name : "NULL")}");
                Debug.Log($"Medium Enemy: {(mediumEnemy != null ? mediumEnemy.name : "NULL")}");
                Debug.Log($"Hard Enemy: {(hardEnemy != null ? hardEnemy.name : "NULL")}");

                // List all enemies
                Debug.Log("=== SPAWNED ENEMIES ===");
                for (int i = 0; i < spawnedEnemies.Count; i++)
                {
                    if (spawnedEnemies[i] != null)
                    {
                        Debug.Log($"  {i}: {spawnedEnemies[i].name} (Active: {spawnedEnemies[i].activeSelf})");
                    }
                    else
                    {
                        Debug.Log($"  {i}: NULL");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Damage all enemies by a specified amount (for testing)
    /// </summary>
    void DamageAllEnemies(int damageAmount)
    {
        Debug.Log($"💥💥💥 DAMAGING ALL ENEMIES ({damageAmount} damage) 💥💥💥");

        // Find BOTH types of enemies
        EnemyFollow[] meleeEnemies = FindObjectsOfType<EnemyFollow>();
        EnemyRanged[] rangedEnemies = FindObjectsOfType<EnemyRanged>();

        int totalEnemies = meleeEnemies.Length + rangedEnemies.Length;
        Debug.Log($"Found {totalEnemies} enemies ({meleeEnemies.Length} melee, {rangedEnemies.Length} ranged)");

        int damagedCount = 0;

        // Damage melee enemies
        foreach (EnemyFollow enemy in meleeEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                Debug.Log($"  💥 Damaging melee: {enemy.gameObject.name}");
                enemy.TakeDamage(damageAmount);
                damagedCount++;
            }
        }

        // Damage ranged enemies
        foreach (EnemyRanged enemy in rangedEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                Debug.Log($"  💥 Damaging ranged: {enemy.gameObject.name}");
                enemy.TakeDamage(damageAmount);
                damagedCount++;
            }
        }

        Debug.Log($"💥 Damaged {damagedCount} enemies for {damageAmount} damage each!");
    }

    /// <summary>
    /// NUCLEAR OPTION: Kill all enemies with death animations
    /// </summary>
    void NuclearKillAll()
    {
        Debug.Log("🔥🔥🔥 NUCLEAR KILL ACTIVATED 🔥🔥🔥");

        StopAllCoroutines();

        // Find BOTH types of enemies
        EnemyFollow[] meleeEnemies = FindObjectsOfType<EnemyFollow>();
        EnemyRanged[] rangedEnemies = FindObjectsOfType<EnemyRanged>();

        int totalEnemies = meleeEnemies.Length + rangedEnemies.Length;
        Debug.Log($"Found {totalEnemies} enemies ({meleeEnemies.Length} melee, {rangedEnemies.Length} ranged)");

        int killedCount = 0;

        // Kill melee enemies
        foreach (EnemyFollow enemy in meleeEnemies)
        {
            if (enemy != null)
            {
                bool wasAlive = enemy.IsAlive();
                Debug.Log($"  💀 Melee: {enemy.gameObject.name}, Alive: {wasAlive}");
                enemy.TakeDamage(9999);
                killedCount++;
            }
        }

        // Kill ranged enemies
        foreach (EnemyRanged enemy in rangedEnemies)
        {
            if (enemy != null)
            {
                bool wasAlive = enemy.IsAlive();
                Debug.Log($"  💀 Ranged: {enemy.gameObject.name}, Alive: {wasAlive}, Active: {enemy.isActiveAndEnabled}");
                enemy.TakeDamage(9999);
                killedCount++;
            }
        }

        Debug.Log($"💀 Killed {killedCount} enemies");

        spawnedEnemies.Clear();
        aliveEnemies = 0;

        if (waveActive)
        {
            Debug.Log("🔥 Force completing wave");
            CancelInvoke();
            WaveComplete();
        }

        Debug.Log("🔥 Nuclear kill complete!");
    }

    void KillAllEnemies()
    {
        Debug.Log($"=== KILLING ALL ENEMIES ===");

        // Find all enemy types
        EnemyFollow[] meleeEnemies = FindObjectsOfType<EnemyFollow>();
        EnemyRanged[] rangedEnemies = FindObjectsOfType<EnemyRanged>();

        Debug.Log($"Found {meleeEnemies.Length} melee and {rangedEnemies.Length} ranged enemies");

        foreach (EnemyFollow enemy in meleeEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                enemy.TakeDamage(9999);
            }
        }

        foreach (EnemyRanged enemy in rangedEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                enemy.TakeDamage(9999);
            }
        }

        spawnedEnemies.Clear();
        aliveEnemies = 0;

        if (waveActive)
        {
            CancelInvoke();
            WaveComplete();
        }
    }
}