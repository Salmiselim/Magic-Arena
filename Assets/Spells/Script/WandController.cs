using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WandController : MonoBehaviour
{
    [Header("Wand References")]
    public XRGrabInteractable grabInteractable;
    public TrailRenderer spellTrail;
    public PlayerStats playerStats;
    public SpellManager spellManager;

    [Header("UI References")]
    public Canvas spellMenuCanvas; // Only this one - your "Select your spell" Canvas

    [Header("Input Actions")]
    public InputActionReference toggleMenuAction;  // Left Primary Button
    public InputActionReference triggerAction;     // Right Trigger

    [Header("Settings")]
    public float castCooldown = 0.5f;

    // State
    private bool isMenuOpen = false;
    private bool canCast = true;
    private Spell currentSpell;

    private void Start()
    {
        // Initialize spells with this controller as caster
        if (spellManager != null)
        {
            foreach (Spell spell in spellManager.learnedSpells)
            {
                spell.Initialize(this);
            }

            // Set initial spell
            currentSpell = spellManager.GetActiveSpell();
            if (currentSpell == null && spellManager.learnedSpells.Count > 0)
            {
                spellManager.SetActiveSpell(spellManager.learnedSpells[0]);
                currentSpell = spellManager.learnedSpells[0];
            }
        }

        // Hide menu by default
        if (spellMenuCanvas != null)
            spellMenuCanvas.enabled = false;

        // Setup trail
        if (spellTrail != null)
            spellTrail.enabled = false;

        // Setup input
        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (toggleMenuAction != null)
        {
            toggleMenuAction.action.Enable();
            toggleMenuAction.action.performed += OnMenuToggle;
        }

        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPressed;
            triggerAction.action.canceled += OnTriggerReleased;
        }
    }

    #region Input Handlers

    private void OnMenuToggle(InputAction.CallbackContext context)
    {
        ToggleSpellMenu();
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        if (!canCast || !grabInteractable.isSelected || isMenuOpen) return;

        CastSpell();
    }

    private void OnTriggerReleased(InputAction.CallbackContext context)
    {
        if (spellTrail != null)
            spellTrail.enabled = false;
    }

    #endregion

    #region Spell Functions

    private void ToggleSpellMenu()
    {
        if (spellMenuCanvas == null) return;

        isMenuOpen = !isMenuOpen;
        spellMenuCanvas.enabled = isMenuOpen;
        Debug.Log($"Spell Menu: {(isMenuOpen ? "OPEN" : "CLOSED")}");
    }

    private void CastSpell()
    {
        if (spellManager == null || playerStats == null || currentSpell == null) return;

        // Check mana
        if (!playerStats.UseMana(currentSpell.manaCost))
        {
            Debug.Log("Not enough mana!");
            return;
        }

        // Get cast position from wand tip
        Vector3 castPosition = transform.position;
        if (grabInteractable.attachTransform != null)
        {
            castPosition = grabInteractable.attachTransform.position;
        }

        // Cast the spell
        currentSpell.Cast(castPosition, transform.forward);

        // Visual feedback
        StartCoroutine(SpellCastFeedback());

        // Cooldown
        StartCoroutine(CastingCooldown());

        Debug.Log($"Cast: {currentSpell.spellName}");
    }

    // Public method for UI buttons to call
    public void SelectSpellByName(string spellName)
    {
        if (spellManager == null) return;

        foreach (Spell spell in spellManager.learnedSpells)
        {
            if (spell.spellName == spellName)
            {
                spellManager.SetActiveSpell(spell);
                currentSpell = spell;
                Debug.Log($"Selected: {spell.spellName}");

                // Close menu after selection
                if (spellMenuCanvas != null)
                {
                    spellMenuCanvas.enabled = false;
                    isMenuOpen = false;
                }
                return;
            }
        }

        Debug.LogWarning($"Spell '{spellName}' not found!");
    }

    // Alternative: Select by index
    public void SelectSpellByIndex(int index)
    {
        if (spellManager == null || index < 0 || index >= spellManager.learnedSpells.Count)
            return;

        Spell selectedSpell = spellManager.learnedSpells[index];
        spellManager.SetActiveSpell(selectedSpell);
        currentSpell = selectedSpell;
        Debug.Log($"Selected: {selectedSpell.spellName}");

        // Close menu
        if (spellMenuCanvas != null)
        {
            spellMenuCanvas.enabled = false;
            isMenuOpen = false;
        }
    }

    #endregion

    #region Visual Feedback

    private IEnumerator SpellCastFeedback()
    {
        // Enable trail
        if (spellTrail != null)
        {
            spellTrail.enabled = true;

            // Color based on spell type
            if (currentSpell is ElementalMagic elemental)
            {
                spellTrail.startColor = GetElementColor(elemental.type);
                spellTrail.endColor = new Color(spellTrail.startColor.r, spellTrail.startColor.g, spellTrail.startColor.b, 0);
            }
        }

        // Simple wand recoil
        Vector3 originalPos = transform.localPosition;
        transform.localPosition = originalPos + Vector3.back * 0.05f;
        yield return new WaitForSeconds(0.05f);
        transform.localPosition = originalPos;

        // Keep trail for a bit
        yield return new WaitForSeconds(0.3f);

        if (spellTrail != null)
            spellTrail.enabled = false;
    }

    private IEnumerator CastingCooldown()
    {
        canCast = false;
        yield return new WaitForSeconds(castCooldown);
        canCast = true;
    }

    private Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return new Color(1f, 0.5f, 0f);
            case ElementType.Ice: return new Color(0f, 0.8f, 1f);
            case ElementType.Lightning: return new Color(1f, 1f, 0f);
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case ElementType.Wind: return new Color(0.8f, 1f, 1f);
            default: return Color.white;
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (toggleMenuAction != null)
            toggleMenuAction.action.performed -= OnMenuToggle;

        if (triggerAction != null)
        {
            triggerAction.action.performed -= OnTriggerPressed;
            triggerAction.action.canceled -= OnTriggerReleased;
        }
    }

    #endregion
}