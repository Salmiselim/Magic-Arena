using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;          // Your skeleton prefab
    public GameObject spawnPortalPrefab;    // The portal quad with shader

    [Header("Difficulty Colors")]
    public Color easyColor = Color.green;
    public Color mediumColor = Color.yellow;
    public Color hardColor = Color.red;

    [Header("Portal Animation")]
    public float portalOpenTime = 1f;       // Time for portal to fully open
    public float portalHoldTime = 0.5f;     // Time portal stays open
    public float portalCloseTime = 0.8f;    // Time for portal to close
    public float maxPortalScale = 3f;       // Maximum portal size
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Enemy Spawn Animation")]
    public float enemyRiseHeight = 2f;      // How far below ground enemy starts
    public float enemyRiseTime = 1.2f;      // Time for enemy to rise

    [Header("Auto Spawn Testing (optional)")]
    public bool autoSpawnForTesting = true;
    public float autoSpawnInterval = 5f;
    private float nextAutoSpawnTime;

    // Difficulty levels
    public enum Difficulty { Easy, Medium, Hard }

    /// <summary>
    /// Call this to spawn an enemy at a specific position with a difficulty
    /// </summary>
    public void SpawnEnemy(Vector3 position, Difficulty difficulty)
    {
        StartCoroutine(SpawnSequence(position, difficulty));
    }

    /// <summary>
    /// Spawn enemy using EnemyData (used by WaveManager)
    /// </summary>
    public IEnumerator SpawnSequenceWithData(Vector3 position, EnemyData data, System.Action<GameObject, EnemyData> onComplete)
    {
        yield return StartCoroutine(SpawnSequenceInternal(position, data.difficulty, data.prefab, data.portalColor));

        // Find the spawned enemy and callback
        GameObject spawnedEnemy = GameObject.FindGameObjectWithTag("Enemy"); // You'll need to tag enemies
        if (onComplete != null && spawnedEnemy != null)
        {
            onComplete(spawnedEnemy, data);
        }
    }

    /// <summary>
    /// The complete spawn animation sequence with portal opening/closing
    /// </summary>
    private IEnumerator SpawnSequence(Vector3 spawnPos, Difficulty difficulty)
    {
        yield return StartCoroutine(SpawnSequenceInternal(spawnPos, difficulty, enemyPrefab, GetDifficultyColor(difficulty)));
    }

    /// <summary>
    /// Internal spawn sequence that accepts custom prefab and color
    /// </summary>
    private IEnumerator SpawnSequenceInternal(Vector3 spawnPos, Difficulty difficulty, GameObject prefabToSpawn, Color portalColor)
    {
        // Ensure spawn position is on ground
        spawnPos.y = 0.01f; // Slightly above ground to avoid z-fighting

        // ========== PHASE 1: CREATE AND OPEN PORTAL ==========
        GameObject portal = Instantiate(spawnPortalPrefab, spawnPos, Quaternion.Euler(90, 0, 0));

        // Get components
        Renderer portalRenderer = portal.GetComponent<Renderer>();
        ParticleSystem portalParticles = portal.GetComponentInChildren<ParticleSystem>();

        // Set portal color
        portalRenderer.material.SetColor("_Color", portalColor);

        // Set particle color if exists
        if (portalParticles != null)
        {
            var mainModule = portalParticles.main;
            mainModule.startColor = portalColor;
            portalParticles.Play();
        }

        // Animate portal opening (scale from 0 to max)
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * maxPortalScale;

        while (elapsed < portalOpenTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / portalOpenTime;
            float curveValue = openCurve.Evaluate(progress);

            portal.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, curveValue);

            // Adjust shader properties during opening
            portalRenderer.material.SetFloat("_OuterRadius", 0.4f + curveValue * 0.1f);

            yield return null;
        }

        portal.transform.localScale = targetScale;

        // ========== PHASE 2: SPAWN ENEMY UNDERGROUND ==========
        Vector3 undergroundPos = spawnPos - new Vector3(0, enemyRiseHeight, 0);
        GameObject enemy = Instantiate(prefabToSpawn, undergroundPos, Quaternion.identity);

        // Tag enemy so WaveManager can find it
        enemy.tag = "Enemy";

        // Get all renderers and make enemy invisible
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
        Material[] originalMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            // Store original material
            originalMaterials[i] = new Material(renderers[i].material);

            // Make transparent
            SetMaterialTransparent(renderers[i].material);
            Color col = renderers[i].material.color;
            col.a = 0;
            renderers[i].material.color = col;
        }

        // Hold portal open briefly
        yield return new WaitForSeconds(portalHoldTime);

        // ========== PHASE 3: ENEMY RISES THROUGH PORTAL ==========
        elapsed = 0f;

        while (elapsed < enemyRiseTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / enemyRiseTime;

            // Smooth ease-out curve
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            // Move enemy up
            enemy.transform.position = Vector3.Lerp(undergroundPos, spawnPos, smoothProgress);

            // Fade enemy in (faster fade than movement)
            float fadeProgress = Mathf.Min(progress * 1.5f, 1f);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color col = renderers[i].material.color;
                col.a = fadeProgress;
                renderers[i].material.color = col;
            }

            yield return null;
        }

        // Ensure final position and visibility
        enemy.transform.position = spawnPos;

        // ========== PHASE 4: CLOSE PORTAL ==========
        elapsed = 0f;

        while (elapsed < portalCloseTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / portalCloseTime;
            float curveValue = openCurve.Evaluate(1f - progress); // Reverse the curve

            portal.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, curveValue);

            // Make portal more intense as it closes
            portalRenderer.material.SetFloat("_EmissionStrength", 2f + (1f - curveValue) * 3f);

            yield return null;
        }

        // ========== PHASE 5: CLEANUP ==========
        // Restore enemy materials to non-transparent
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }

        // Activate enemy AI
        EnemyFollow enemyScript = enemy.GetComponent<EnemyFollow>();
        if (enemyScript != null)
        {
            enemyScript.Activate();
        }

        // Final portal flash effect
        if (portalParticles != null)
        {
            var emission = portalParticles.emission;
            emission.rateOverTime = 100; // Burst of particles
            yield return new WaitForSeconds(0.2f);
        }

        // Destroy portal
        Destroy(portal);
    }

    /// <summary>
    /// Make a material support transparency
    /// </summary>
    private void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 2); // Fade mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    /// <summary>
    /// Get the color for each difficulty level
    /// </summary>
    private Color GetDifficultyColor(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return easyColor;
            case Difficulty.Medium: return mediumColor;
            case Difficulty.Hard: return hardColor;
            default: return Color.white;
        }
    }

    // ✨ TESTING CODE
    void Update()
    {
        // Auto-spawn for testing
        if (autoSpawnForTesting && Time.time >= nextAutoSpawnTime)
        {
            Difficulty testDifficulty = (Difficulty)(((int)(Time.time / autoSpawnInterval)) % 3);
            Vector3 spawnPos = transform.position + transform.forward * 5f;
            spawnPos.y = 0;
            SpawnEnemy(spawnPos, testDifficulty);
            nextAutoSpawnTime = Time.time + autoSpawnInterval;
        }

        // Manual keyboard testing
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Vector3 spawnPos = transform.position + transform.forward * 5f;
                spawnPos.y = 0;
                SpawnEnemy(spawnPos, Difficulty.Easy);
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Vector3 spawnPos = transform.position + transform.forward * 5f;
                spawnPos.y = 0;
                SpawnEnemy(spawnPos, Difficulty.Medium);
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                Vector3 spawnPos = transform.position + transform.forward * 5f;
                spawnPos.y = 0;
                SpawnEnemy(spawnPos, Difficulty.Hard);
            }
        }
    }
}