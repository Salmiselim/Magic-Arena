using UnityEngine;
using UnityEngine.UI;

public class SpellSelectButton : MonoBehaviour
{
    [Header("Spell Settings")]
    public int spellIndex = 0;
    public Spell assignedSpell;
    
    [Header("UI References")]
    public Image buttonImage;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    
    private bool isHovered = false;
    private bool isSelected = false;
    
    void Start()
    {
        // Auto-find Image if not assigned
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        
        UpdateButtonAppearance();
    }
    
    void UpdateButtonAppearance()
    {
        if (buttonImage == null) return;
        
        if (isSelected)
        {
            buttonImage.color = selectedColor;
        }
        else if (isHovered)
        {
            buttonImage.color = hoverColor;
        }
        else
        {
            // Default color based on spell type
            Spell spell = GetSpell();
            if (spell != null)
            {
                if (spell is ElementalMagic elemental)
                    buttonImage.color = GetElementColor(elemental.type) * 0.8f;
                else if (spell is OffensiveSpell)
                    buttonImage.color = Color.red * 0.8f;
                else if (spell is DefensiveSpell)
                    buttonImage.color = Color.green * 0.8f;
            }
            else
            {
                buttonImage.color = normalColor;
            }
        }
    }
    
    // ===== WAND INTERACTION =====
    
    public void OnWandHoverEnter()
    {
        if (isHovered || isSelected) return;
        
        isHovered = true;
        UpdateButtonAppearance();
        
        Debug.Log($"Hovering: {GetSpellName()}");
    }
    
    public void OnWandHoverExit()
    {
        if (!isHovered) return;
        
        isHovered = false;
        UpdateButtonAppearance();
    }
    
    public void OnSelected()
    {
        isSelected = true;
        UpdateButtonAppearance();
        Debug.Log($"Selected: {GetSpellName()}");
    }
    
    public void Deselect()
    {
        isSelected = false;
        UpdateButtonAppearance();
    }
    
    // ===== UTILITY METHODS =====
    
    public Spell GetSpell()
    {
        if (assignedSpell != null) return assignedSpell;
        
        if (SpellManager.Instance != null && 
            spellIndex >= 0 && 
            spellIndex < SpellManager.Instance.learnedSpells.Count)
        {
            return SpellManager.Instance.learnedSpells[spellIndex];
        }
        
        return null;
    }
    
    public string GetSpellName()
    {
        Spell spell = GetSpell();
        return spell != null ? spell.spellName : "Unknown";
    }
    
    Color GetElementColor(ElementType element)
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
}