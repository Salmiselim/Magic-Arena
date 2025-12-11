// DestructibleWall_FixedForReal.cs  ←  Replace your current script with this
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DestructibleWall_FixedForReal : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 1800f;
    public float explosionRadius = 6f;
    public float chunkLifetime = 12f;

    [Header("Optional Polish")]
    public ParticleSystem explosionVFX;
    public AudioClip boomSound;

    [Header("XR Interactable (drag WallMesh interactable here)")]
    public XRSimpleInteractable wallInteractable;

    private AudioSource audioSource;
    private bool isDestroyed = false;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        if (wallInteractable == null)
            wallInteractable = GetComponentInChildren<XRSimpleInteractable>();
    }

    void OnEnable() => wallInteractable?.activated.AddListener(_ => Explode());
    void OnDisable() => wallInteractable?.activated.RemoveListener(_ => Explode());

    // Still works with real spells too
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.GetComponent<SpellProjectile>())
            Explode();
    }

    [Header("Puzzle (Optional – drag PuzzleManager here if part of puzzle)")]
    public RunePuzzleManager puzzleManager;
    public int runeIndex = -1;
    public void Explode()
    {

        if (puzzleManager && runeIndex >= 0)
            puzzleManager.OnRuneDestroyed(runeIndex);
        if (isDestroyed) return;
        isDestroyed = true;

        // 1. Hide the intact wall instantly
        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.gameObject.name.ToLower().Contains("wall") ||
                mr.gameObject.name.ToLower().Contains("intact"))
                mr.enabled = false;
        }

        // 2. Activate + force-show + blast every chunk
        foreach (Transform child in transform)
        {
            // Skip the intact wall object itself
            if (child.name.ToLower().Contains("wall") || child.name.ToLower().Contains("intact"))
                continue;

            // Make sure the chunk is fully awake and visible
            child.gameObject.SetActive(true);

            MeshRenderer chunkMR = child.GetComponent<MeshRenderer>();
            if (chunkMR) chunkMR.enabled = true;                 // ← THIS WAS MISSING!

            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.WakeUp();

                Vector3 dir = (child.position - transform.position).normalized + Vector3.up * 0.3f;
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1.5f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 300f, ForceMode.Impulse); // spin!
            }
        }

        explosionVFX?.Play();
        if (boomSound) audioSource.PlayOneShot(boomSound);


        IEnumerator Cleanup()
        {
            yield return new WaitForSeconds(chunkLifetime);
            Destroy(gameObject);
        }
    }
}