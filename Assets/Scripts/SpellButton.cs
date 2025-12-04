using UnityEngine;

public class SpellButton : MonoBehaviour
{
    [Header("Name of the spell this button casts")]
    public string spellName;

    public void CastSpell()
    {
        Debug.Log("Casting spell: " + spellName);

        if (spellName == "Expelliarmus")
        {
            GameObject expelliarmus = Resources.Load<GameObject>("ExpelliarmusProjectile");
            if (expelliarmus != null)
            {
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
                GameObject proj = Instantiate(expelliarmus, spawnPos, Camera.main.transform.rotation);
            }
        }
        if (spellName == "Wingardium Leviosa")
        {
            GameObject leviosa = Resources.Load<GameObject>("WingardiumLeviosaSpell");
            if (leviosa != null)
            {
                Vector3 spawnPos = Camera.main.transform.position;
                Instantiate(leviosa, spawnPos, Camera.main.transform.rotation);
            }
        }

        if (spellName == "Protego")
        {
            GameObject shield = Resources.Load<GameObject>("ProtegoShield");
            if (shield != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 spawnPos = cam.position + cam.forward * 0.8f; // in front of player
                Quaternion spawnRot = Quaternion.LookRotation(cam.forward, Vector3.up);

                Instantiate(shield, spawnPos, spawnRot);
            }
        }
        PageClick pageClick = FindObjectOfType<PageClick>();
        if (pageClick != null)
        {
            pageClick.CloseSpellSheet();
        }
        

    }
}