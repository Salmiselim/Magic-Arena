using UnityEngine;

[CreateAssetMenu(fileName = "New Offensive Spell", menuName = "Spells/Offensive")]
public class OffensiveSpell : Spell
{
    public float damage;
    public float range;
    public GameObject projectilePrefab;

    public override void Cast(Vector3 origin, Vector3 direction)
    {
        if (projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab,
                                          origin + direction * 0.5f,
                                          Quaternion.LookRotation(direction));

            Projectile p = proj.GetComponent<Projectile>();
            if (p != null)
            {
                p.Initialize(damage, range);
            }
        }
        else
        {
            // Raycast if no projectile
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, range))
            {
                Debug.Log(spellName + " hit " + hit.collider.name);
                // TODO: apply damage to enemy
            }
        }
    }
}
