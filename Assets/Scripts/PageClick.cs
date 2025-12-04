using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PageClick : MonoBehaviour
{
    [Header("Assign your Spell Sheet prefab here")]
    public GameObject spellSheetPrefab;  // unified name

    private GameObject activeSpellSheet; // track the spawned sheet

    // This function is called when the page is clicked (Select Entered)
    public void OnPageClicked(XRBaseInteractor interactor)
    {
        if (spellSheetPrefab == null)
        {
            Debug.LogWarning("No spell sheet prefab assigned!");
            return;
        }

        // If a sheet is already open, don’t spawn another
        if (activeSpellSheet != null) return;

        // Spawn the spell sheet in front of the player’s camera
        Transform cam = Camera.main.transform;
        Vector3 spawnPos = cam.position + cam.forward * 0.8f; // 0.8m in front
        Quaternion spawnRot = Quaternion.LookRotation(cam.forward, Vector3.up);

        activeSpellSheet = Instantiate(spellSheetPrefab, spawnPos, spawnRot);
    }

    // Called by SpellButton after casting a spell
    public void CloseSpellSheet()
    {
        if (activeSpellSheet != null)
        {
            Destroy(activeSpellSheet);
            activeSpellSheet = null;
        }
    }
}