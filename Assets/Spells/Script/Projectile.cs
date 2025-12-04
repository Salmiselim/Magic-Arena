using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 5f;

    private float damage;
    private float range;
    private Vector3 startPosition;

    public void Initialize(float damageAmount, float rangeAmount)
    {
        damage = damageAmount;
        range = rangeAmount;
        startPosition = transform.position;
        Destroy(gameObject, lifetime); // auto destroy
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        if (Vector3.Distance(startPosition, transform.position) > range)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // TODO: Check if "other" is an enemy
        Debug.Log("Projectile hit " + other.name);

        // Apply damage if enemy has Health component
       /* var enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
*/
        Destroy(gameObject);
    }
}
