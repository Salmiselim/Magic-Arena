using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    public Image healthFill;              // Drag the HealthFill image here
    private Transform target;              // The enemy this bar follows
    private Camera mainCamera;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0);  // Position above enemy (adjusted for 0.9 scale)
    public bool rotateToCamera = true;     // Always face camera

    [Header("Color Settings")]
    public Color fullHealthColor = Color.green;
    public Color halfHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public float colorTransitionThreshold = 0.5f;  // When to change from green to yellow
    public float lowHealthThreshold = 0.3f;        // When to change to red

    void Start()
    {
        mainCamera = Camera.main;

        if (healthFill == null)
        {
            Debug.LogError("❌ HealthFill not assigned! Drag the HealthFill image into the script.");
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            // Target destroyed, destroy health bar
            Destroy(gameObject);
            return;
        }

        // Follow the enemy with offset (scaled for 0.9 enemy)
        transform.position = target.position + offset;

        // Always face camera
        if (rotateToCamera && mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }

    /// <summary>
    /// Set which enemy this health bar follows
    /// </summary>
    public void SetTarget(Transform enemyTransform)
    {
        target = enemyTransform;
        Debug.Log($"✅ Health bar set to follow: {enemyTransform.name}");
    }

    /// <summary>
    /// Update the health bar fill amount and color
    /// </summary>
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthFill == null)
        {
            Debug.LogWarning("⚠️ HealthFill is null, cannot update health bar!");
            return;
        }

        // Calculate fill percentage (0 to 1)
        float healthPercent = (float)currentHealth / maxHealth;
        healthPercent = Mathf.Clamp01(healthPercent); // Ensure between 0 and 1

        // Update fill amount
        healthFill.fillAmount = healthPercent;

        // Change color based on health
        if (healthPercent <= lowHealthThreshold)
        {
            healthFill.color = lowHealthColor; // Red when low
        }
        else if (healthPercent <= colorTransitionThreshold)
        {
            healthFill.color = halfHealthColor; // Yellow when medium
        }
        else
        {
            healthFill.color = fullHealthColor; // Green when high
        }

        Debug.Log($"Health bar updated: {currentHealth}/{maxHealth} ({healthPercent * 100:F0}%)");
    }
}