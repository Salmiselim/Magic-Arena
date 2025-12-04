using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays enemy health bar above their head
/// Attach to the health bar Canvas prefab
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    public Image fillImage;                 // The green fill bar

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0);  // Height above enemy
    public bool alwaysFaceCamera = true;    // Rotate to face camera
    public bool hideWhenFull = true;        // Hide bar at full health

    private Transform enemy;
    private Camera mainCamera;
    private Canvas canvas;

    void Awake()
    {
        mainCamera = Camera.main;
        canvas = GetComponent<Canvas>();

        // Auto-find fill image if not assigned
        if (fillImage == null)
        {
            fillImage = transform.Find("Background/Fill")?.GetComponent<Image>();

            if (fillImage == null)
            {
                Debug.LogError("❌ Health bar Fill image not found! Make sure hierarchy is: Canvas/Background/Fill");
            }
        }
    }

    void LateUpdate()
    {
        // Follow enemy position
        if (enemy != null)
        {
            transform.position = enemy.position + offset;

            // Always face camera
            if (alwaysFaceCamera && mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
    }

    /// <summary>
    /// Set which enemy this health bar belongs to
    /// </summary>
    public void SetTarget(Transform enemyTransform)
    {
        enemy = enemyTransform;
    }

    /// <summary>
    /// Update health bar fill amount
    /// </summary>
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (fillImage == null)
        {
            Debug.LogError("❌ Fill Image is still null in UpdateHealth!");
            return;
        }

        float fillAmount = (float)currentHealth / maxHealth;
        fillImage.fillAmount = fillAmount;

        // Change color based on health
        if (fillAmount > 0.6f)
            fillImage.color = Color.green;
        else if (fillAmount > 0.3f)
            fillImage.color = Color.yellow;
        else
            fillImage.color = Color.red;

        // Hide when full health (optional)
        if (hideWhenFull && canvas != null)
        {
            canvas.enabled = fillAmount < 1f;
        }
    }
}