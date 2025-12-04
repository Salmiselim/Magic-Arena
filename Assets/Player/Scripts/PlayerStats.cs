using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Mana")]
    public float maxMana = 100f;
    public float currentMana;

    void Awake()
    {
        currentHP = maxHP;
        currentMana = maxMana;
    }

    // -----------------------
    // Health Methods
    // -----------------------

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log("Player took " + amount + " damage. HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log("Player healed " + amount + " HP. Current HP: " + currentHP);
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        // TODO: add death logic (respawn, game over screen, etc.)
    }

    // -----------------------
    // Mana Methods
    // -----------------------

    public bool UseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            Debug.Log("Used " + amount + " mana. Remaining: " + currentMana);
            return true;
        }
        else
        {
            Debug.Log("Not enough mana!");
            return false;
        }
    }

    public void ReplenishMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        Debug.Log("Mana replenished by " + amount + ". Current Mana: " + currentMana);
    }
}
