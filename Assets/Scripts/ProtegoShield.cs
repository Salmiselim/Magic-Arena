using UnityEngine;

public class ProtegoShield : MonoBehaviour
{
    public float duration = 5f;

    void Start()
    {
        // Destroy shield after duration
        Destroy(gameObject, duration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Block projectiles
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Destroy(collision.gameObject); // absorb the hit
            Debug.Log("Protego blocked a projectile!");
        }
    }
}