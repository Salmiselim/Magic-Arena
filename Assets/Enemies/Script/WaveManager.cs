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

            // Spawn enemy
            StartCoroutine(SpawnSingleEnemy(spawnPos, data));

            // Wait before next spawn
            yield return new WaitForSeconds(data.spawnDelay);
        }

        Debug.Log($"All {data.spawnCount} enemies spawned for this wave!");
    }

    IEnumerator SpawnSingleEnemy(Vector3 position, EnemyData data)
    {
        GameObject spawnedEnemy = null;

        // Spawn with portal animation
        yield return StartCoroutine(spawner.SpawnSequenceWithData(
            position,
            data,
            (enemyObj, enemyData) => {
                spawnedEnemy = enemyObj;
            }
        ));

        if (spawnedEnemy != null)
        {
            // Make sure it has Enemy tag
            if (spawnedEnemy.tag != "Enemy")
            {
                Debug.LogWarning($"Enemy spawned without 'Enemy' tag! Setting it now.");
                spawnedEnemy.tag = "Enemy";
            }

            // Initialize enemy
            EnemyFollow enemy = spawnedEnemy.GetComponent<EnemyFollow>();
            if (enemy != null)
            {
                enemy.Initialize(data);
            }
            else
            {
                Debug.LogError("Spawned enemy has no EnemyFollow script!");
            }

            // Track it
            spawnedEnemies.Add(spawnedEnemy);
            aliveEnemies++;

            Debug.Log($"✅ Enemy spawned and tracked! Name: {spawnedEnemy.name}, Tag: {spawnedEnemy.tag}, Alive: {aliveEnemies}/{totalEnemiesInWave}");
        }
        else
        {
            Debug.LogError("❌ Enemy failed to spawn!");
        }
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

    public void OnEnemyDied(EnemyFollow enemy)
    {
        Debug.Log($"📢 WaveManager.OnEnemyDied called for {(enemy != null ? enemy.gameObject.name : "NULL")}");

        // Remove from tracking (but DON'T destroy - enemy destroys itself!)
        if (enemy != null && enemy.gameObject != null)
        {
            spawnedEnemies.Remove(enemy.gameObject);
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
    /// NUCLEAR OPTION: Kill all enemies with death animations
    /// </summary>
    void NuclearKillAll()
    {
        Debug.Log("🔥🔥🔥 NUCLEAR KILL ACTIVATED 🔥🔥🔥");

        StopAllCoroutines();

        EnemyFollow[] allEnemies = FindObjectsOfType<EnemyFollow>();
        Debug.Log($"Found {allEnemies.Length} enemies to kill with death animation");

        int killedCount = 0;
        foreach (EnemyFollow enemy in allEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                Debug.Log($"  💀 Triggering death for: {enemy.gameObject.name}");
                enemy.TakeDamage(9999);
                killedCount++;
            }
        }

        Debug.Log($"💀 Triggered death animation for {killedCount} enemies");

        spawnedEnemies.Clear();
        aliveEnemies = 0;

        // ******** FIX HERE ********
        if (waveActive)
        {
            Debug.Log("🔥 Force completing wave (death animations will continue)");
            CancelInvoke();
            WaveComplete();
        }

        Debug.Log("🔥 Nuclear kill complete!");
    }


    void KillAllEnemies()
    {
        Debug.Log($"=== KILLING ALL ENEMIES ===");

        // Find all enemies with the script
        EnemyFollow[] allEnemies = FindObjectsOfType<EnemyFollow>();
        Debug.Log($"Found {allEnemies.Length} enemies to kill");

        // Kill each enemy properly (triggers death animation)
        foreach (EnemyFollow enemy in allEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                Debug.Log($"Killing enemy: {enemy.gameObject.name}");
                enemy.TakeDamage(9999); // Calls Die() method which plays animation
            }
        }

        // Clear our tracking lists
        spawnedEnemies.Clear();
        aliveEnemies = 0;

        Debug.Log($"After kill - Alive: {aliveEnemies}");

        // Force complete the wave immediately
        if (waveActive)
        {
            Debug.Log("Force completing wave NOW");
            CancelInvoke(); // Cancel any pending wave starts
            WaveComplete();
        }
    }
}