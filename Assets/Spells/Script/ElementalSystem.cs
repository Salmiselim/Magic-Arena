using UnityEngine;

public enum ElementType { Fire, Ice, Lightning, Earth, Wind }

[CreateAssetMenu(fileName = "New Elemental Spell", menuName = "Spells/Elemental")]
public class ElementalMagic : Spell
{
    public ElementType type;
    public GameObject effectPrefab; // particle effect for wand

    public override void Cast(Vector3 origin, Vector3 direction)
    {
        // Spawn effect at wand
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, origin, Quaternion.LookRotation(direction));
            Destroy(effect, 2f); // auto-destroy
        }

        // Apply elemental effect (example)
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, 10f))
        {
            Debug.Log(spellName + " (" + type + ") hit " + hit.collider.name);
            // TODO: apply effect based on type (burn, slow, stun, etc.)
        }
    }
}
