using UnityEngine;

[CreateAssetMenu(fileName = "New Defensive Spell", menuName = "Spells/Defensive")]
public class DefensiveSpell : Spell
{
    public float duration;
    public float strength; // shield HP or heal amount
    public GameObject shieldPrefab; // optional visual

    public override void Cast(Vector3 origin, Vector3 direction)
    {
        Debug.Log(spellName + " defensive spell cast.");

        if (shieldPrefab != null)
        {
            GameObject shield = Instantiate(shieldPrefab, origin, Quaternion.identity);
            shield.transform.parent = caster.transform;
            Destroy(shield, duration);
        }

        // Example: heal player if caster has PlayerStats
        PlayerStats stats = caster.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.Heal(strength);
        }
        
    }
}
