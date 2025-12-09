using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class WandControllerV3 : MonoBehaviour
{
    [Header("References")]
    public TrailRenderer spellTrail;
    public PlayerStats playerStats;
    public SpellManager spellManager;
    public Canvas spellMenuCanvas;
    public Transform wandTip;
    
    [Header("Input Actions")]
    public InputActionReference toggleMenuAction;
    public InputActionReference triggerAction;
    
    [Header("Settings")]
    public float castCooldown = 0.5f;
    public float spellSpawnOffset = 0.1f;
    public float uiInteractionRange = 0.5f;
    
    // Components
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    
    // State - MAKE THESE PUBLIC OR ADD GETTERS
    public bool isMenuOpen = false;
    public bool canCast = true;
    public Spell currentSpell;
    private Transform originalParent;
    public bool isHeldByRightHand = false;
    private SpellSelectButton hoveredButton;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        
        grabInteractable.selectEntered.AddListener(OnGrabAttempt);
        grabInteractable.selectExited.AddListener(OnWandReleased);
        
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;
    }

    void Start()
    {
        if (spellManager != null && spellManager.learnedSpells.Count > 0)
        {
            currentSpell = spellManager.GetActiveSpell() ?? spellManager.learnedSpells[0];
        }
        
        if (spellMenuCanvas != null)
            spellMenuCanvas.enabled = false;
        
        if (spellTrail != null)
            spellTrail.enabled = false;
        
        SetupInputActions();
        
        Debug.Log("Wand initialized. Press Left Primary Button for menu.");
    }
    
    void Update()
    {
        // Check for UI interaction when menu is open
        if (isMenuOpen && isHeldByRightHand)
        {
            CheckUIInteraction();
            
            // Also check for trigger to select button
            if (hoveredButton != null && triggerAction != null && 
                triggerAction.action.ReadValue<float>() > 0.1f)
            {
                SelectCurrentButton();
            }
        }
        
    }
    
    void CheckUIInteraction()
    {
        Vector3 tipPos = GetWandTipPosition();
        Vector3 tipDir = GetWandTipDirection();
        
        Debug.DrawRay(tipPos, tipDir * uiInteractionRange, Color.green);
        
        RaycastHit hit;
        if (Physics.Raycast(tipPos, tipDir, out hit, uiInteractionRange))
        {
            SpellSelectButton button = hit.collider.GetComponent<SpellSelectButton>();
            if (button != null && button != hoveredButton)
            {
                // New button hovered
                if (hoveredButton != null)
                    hoveredButton.OnWandHoverExit();
                
                hoveredButton = button;
                hoveredButton.OnWandHoverEnter();
                Debug.Log($"Hovering over: {button.GetSpellName()}");
            }
        }
        else if (hoveredButton != null)
        {
            // No button under cursor
            hoveredButton.OnWandHoverExit();
            hoveredButton = null;
        }
    }
    
    void SelectCurrentButton()
    {
        if (hoveredButton == null) return;
        
        Spell selectedSpell = hoveredButton.GetSpell();
        if (selectedSpell != null)
        {
            SetCurrentSpell(selectedSpell);
            
            if (SpellManager.Instance != null)
                SpellManager.Instance.SetActiveSpell(selectedSpell);
            
            hoveredButton.OnSelected();
            CloseSpellMenu();
            
            Debug.Log($"Selected: {selectedSpell.spellName}");
            
            hoveredButton = null;
        }
    }
    
    void OnGrabAttempt(SelectEnterEventArgs args)
    {
        string handName = args.interactorObject.transform.name.ToLower();
        bool isLeftHand = handName.Contains("left") || handName.Contains("_l");
        
        if (isLeftHand)
        {
            Debug.Log("Left hand blocked");
            StartCoroutine(CancelLeftHandGrab(args.interactorObject));
            return;
        }
        
        Debug.Log($"Right hand grabbed");
        isHeldByRightHand = true;
        
        if (rb != null) rb.isKinematic = true;
        
        if (grabInteractable.attachTransform != null)
        {
            transform.position = grabInteractable.attachTransform.position;
            transform.rotation = grabInteractable.attachTransform.rotation;
            transform.SetParent(grabInteractable.attachTransform);
        }
    }
    
    IEnumerator CancelLeftHandGrab(IXRSelectInteractor leftHand)
    {
        yield return null;
        if (leftHand != null && leftHand.isSelectActive)
            grabInteractable.interactionManager.SelectExit(leftHand, grabInteractable);
    }
    
    void OnWandReleased(SelectExitEventArgs args)
    {
        Debug.Log("Wand released");
        isHeldByRightHand = false;
        transform.SetParent(originalParent);
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    void SetupInputActions()
    {
        if (toggleMenuAction != null && toggleMenuAction.action != null)
        {
            toggleMenuAction.action.Enable();
            toggleMenuAction.action.performed += OnMenuToggle;
        }
        
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPressed;
            triggerAction.action.canceled += OnTriggerReleased;
        }
    }
    
    void OnMenuToggle(InputAction.CallbackContext context)
    {
        ToggleSpellMenu();
    }
    
  private void OnTriggerPressed(InputAction.CallbackContext context)
{
    if (!canCast || !isHeldByRightHand) return;
    
    // If menu is open, trigger is for UI selection, not casting
    if (isMenuOpen)
    {
        // UI selection is handled in CheckUIInteraction()
        return;
    }
    
    // Menu is closed - cast spell!
    CastSpell();
}
    
    void OnTriggerReleased(InputAction.CallbackContext context)
    {
        if (spellTrail != null)
            spellTrail.enabled = false;
    }
    
    // ===== PUBLIC METHODS =====
    
    public void ToggleSpellMenu()
    {
        if (spellMenuCanvas == null) return;
        
        isMenuOpen = !isMenuOpen;
        spellMenuCanvas.enabled = isMenuOpen;
        
        if (!isMenuOpen && hoveredButton != null)
        {
            hoveredButton.OnWandHoverExit();
            hoveredButton = null;
        }
        
        Debug.Log($"Menu: {(isMenuOpen ? "OPEN" : "CLOSED")}");
    }
    
    public bool IsMenuOpen() => isMenuOpen;
    
    public void CloseSpellMenu()
    {
        isMenuOpen = false;
        if (spellMenuCanvas != null)
            spellMenuCanvas.enabled = false;
    }
    
    public void SetCurrentSpell(Spell spell)
    {
        currentSpell = spell;
        Debug.Log($"Equipped: {spell.spellName}");
    }
    
    // ===== MISSING METHODS (ADD THESE!) =====
    
    public bool CanCast() 
    { 
        return canCast && isHeldByRightHand && !isMenuOpen && currentSpell != null;
    }
    
    public Spell GetCurrentSpell() 
    { 
        return currentSpell; 
    }
    
    public bool IsHeldByRightHand() 
    { 
        return isHeldByRightHand; 
    }
    
    public bool IsCasting() 
    { 
        return !canCast; // When on cooldown
    }
    
    // ===== CASTING =====
    
    void CastSpell()
    {
        if (currentSpell == null || playerStats == null) return;
        
        if (!playerStats.UseMana(currentSpell.manaCost))
        {
            Debug.Log("No mana!");
            return;
        }
        
        Vector3 spawnPos = GetWandTipPosition();
        Vector3 spawnDir = GetWandTipDirection();
        
        currentSpell.Cast(spawnPos, spawnDir);
        
        StartCoroutine(SpellCastFeedback());
        StartCoroutine(CastingCooldown());
        
        Debug.Log($"Cast: {currentSpell.spellName}");
    }
    
    public Vector3 GetWandTipPosition()
    {
        if (wandTip != null)
            return wandTip.position + wandTip.forward * spellSpawnOffset;
        return transform.position;
    }
    
    public Vector3 GetWandTipDirection()
    {
        if (wandTip != null)
            return wandTip.forward;
        return transform.forward;
    }
    
    IEnumerator SpellCastFeedback()
    {
        if (spellTrail != null)
        {
            spellTrail.enabled = true;
            yield return new WaitForSeconds(0.3f);
            spellTrail.enabled = false;
        }
    }
    
    IEnumerator CastingCooldown()
    {
        canCast = false;
        yield return new WaitForSeconds(castCooldown);
        canCast = true;
    }
    
    void OnDestroy()
    {
        if (toggleMenuAction != null && toggleMenuAction.action != null)
            toggleMenuAction.action.performed -= OnMenuToggle;
            
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerPressed;
            triggerAction.action.canceled -= OnTriggerReleased;
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabAttempt);
            grabInteractable.selectExited.RemoveListener(OnWandReleased);
        }
    }
}