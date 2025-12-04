using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpellSelectButton : MonoBehaviour

{
    public Canvas spellMenu;
    public InputActionProperty openMenuButton;

    private bool isOpen = false;

    void Update()
    {
        if (openMenuButton.action.WasPerformedThisFrame())
        {
            isOpen = !isOpen;
            spellMenu.enabled = isOpen;
        }
    }

public void SelectSpellByIndex(int index)
{
    if (index >= 0 && index < SpellManager.Instance.learnedSpells.Count)
    {
        Spell spell = SpellManager.Instance.learnedSpells[index];
        SpellManager.Instance.SetActiveSpell(spell);
    }
}

}
