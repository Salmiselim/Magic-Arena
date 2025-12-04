using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private bool isImmobilized = false;
    private float immobilizeTimer = 0f;

    void Update()
    {
        if (isImmobilized)
        {
            immobilizeTimer -= Time.deltaTime;
            if (immobilizeTimer <= 0f)
            {
                isImmobilized = false;
                // Re-enable movement/animations
            }
            return; // skip movement/attack logic while frozen
        }

        // Normal enemy AI movement/attack here
    }

    public void Immobilize(float duration)
    {
        isImmobilized = true;
        immobilizeTimer = duration;

        // Optional: play "statue" animation or freeze Animator
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
    }
    public void Disarm()
    {
        Debug.Log("Enemy weapon dropped! (placeholder)");
        // Later: detach weapon from hand, apply force, etc.
    }
}
