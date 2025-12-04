using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WandController : MonoBehaviour
{
    [Header("References")]
    public XRGrabInteractable grabInteractable;
    public TrailRenderer spellTrail;
    public PlayerStats playerStats;
    public SpellManager spellManager;

    [Header("Input")]
    public InputActionProperty triggerAction; // assign via inspector (Controller -> Trigger)

    private IXRSelectInteractor currentInteractor;
    private bool spellSelected = false;

    private void Awake()
    {
        if (!grabInteractable)
            grabInteractable = GetComponent<XRGrabInteractable>();
          //  SpellManager.Instance.OnSpellChanged += OnSpellChanged;
        Debug.Log("WandController connected to SpellManager!");
        if (spellTrail)
            spellTrail.enabled = false;
    }

    private void Update()
    {
        if (!spellSelected || currentInteractor == null)
        {
            if (spellTrail) spellTrail.enabled = false;
            return;
        }

        // Use InputAction directly (new system)
        if (triggerAction.action != null && triggerAction.action.ReadValue<float>() > 0.1f)
        {
            if (spellTrail) spellTrail.enabled = true;
            CastSpell();
        }
        else
        {
            if (spellTrail) spellTrail.enabled = false;
        }
    }

    public void SetSpellSelected(bool active)
    {
        spellSelected = active;
        if (!active && spellTrail) spellTrail.enabled = false;
    }

    private void CastSpell()
    {
        Spell spell = spellManager.GetActiveSpell();
        if (spell == null) return;

        if (!playerStats.UseMana(spell.manaCost)) return;

        spell.Cast(transform.position, transform.forward);
    }
}
