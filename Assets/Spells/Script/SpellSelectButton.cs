using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SpellSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Spell Settings")]
    public int spellIndex = 0;
    public Spell assignedSpell; // Optional: assign specific spell

    [Header("UI Elements")]
    public Image iconImage;
    public Text spellNameText;
    public Text manaCostText;
    public Image background;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.green;

    private Button button;
    private bool isSelected = false;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // If assignedSpell is set, use it
        if (assignedSpell != null)
        {
            UpdateButtonUI(assignedSpell);
        }
        // Otherwise, get from SpellManager
        else if (SpellManager.Instance != null && spellIndex < SpellManager.Instance.learnedSpells.Count)
        {
            Spell spell = SpellManager.Instance.learnedSpells[spellIndex];
            UpdateButtonUI(spell);
        }
    }

    private void UpdateButtonUI(Spell spell)
    {
        if (spellNameText != null)
        {
            spellNameText.text = spell.spellName;

            // Color based on spell type
            if (spell is ElementalMagic elemental)
                spellNameText.color = GetElementColor(elemental.type);
            else if (spell is OffensiveSpell)
                spellNameText.color = Color.red;
            else if (spell is DefensiveSpell)
                spellNameText.color = Color.green;
        }

        if (manaCostText != null)
            manaCostText.text = $"Mana: {spell.manaCost}";

        if (iconImage != null)
        {
            // Set icon color based on spell type
            if (spell is ElementalMagic elemental)
                iconImage.color = GetElementColor(elemental.type);
        }
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

    private void OnButtonClicked()
    {
        if (SpellManager.Instance == null) return;

        // Use assigned spell or index
        if (assignedSpell != null)
        {
            SpellManager.Instance.SetActiveSpell(assignedSpell);
        }
        else if (spellIndex < SpellManager.Instance.learnedSpells.Count)
        {
            SpellManager.Instance.SetActiveSpell(SpellManager.Instance.learnedSpells[spellIndex]);
        }

        SetSelected(true);
        Debug.Log($"Selected spell via button: {SpellManager.Instance.GetActiveSpell()?.spellName}");
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (background != null && !isSelected)
        {
            background.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (background != null && !isSelected)
        {
            background.color = normalColor;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}