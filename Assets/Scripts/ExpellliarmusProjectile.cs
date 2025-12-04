using UnityEngine;

public class ExpelliarmusProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float lifeTime = 5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Placeholder: tell enemy to drop weapon
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Disarm(); // Your friend will implement this
            }

            Debug.Log("Expelliarmus hit! Enemy disarmed.");
            Destroy(gameObject);
        }
    }
}