using UnityEngine;
using UnityEngine.Windows.Speech;

public class AutoVoiceSpellManager : MonoBehaviour
{
    public static AutoVoiceSpellManager Instance;

    [Header("Settings")]
    public float commandCooldown = 0.5f;

    private DictationRecognizer dictationRecognizer;
    private float lastCastTime;
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeVoiceRecognition();
    }

    void InitializeVoiceRecognition()
    {
        if (isInitialized) return;

        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationResult += OnDictationResult;
        dictationRecognizer.DictationError += OnDictationError;

        dictationRecognizer.Start();
        isInitialized = true;

        Debug.Log("🎤 AUTO-VOICE READY! Just speak naturally.");
        Debug.Log("💬 Try: 'fireball', 'ice', 'shield', 'lightning'");
    }

    void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        // Clean and process
        text = text.ToLower().Trim();

        // Only accept high/medium confidence
        if (confidence == ConfidenceLevel.Low || confidence == ConfidenceLevel.Rejected)
        {
            Debug.Log($"🎤 (Low confidence): '{text}'");
            return;
        }

        Debug.Log($"<color=green>🎤 Heard: '{text}'</color>");

        ProcessVoiceCommand(text);
    }

    void ProcessVoiceCommand(string command)
    {
        // Cooldown check
        if (Time.time - lastCastTime < commandCooldown)
            return;

        // Find wand for casting
        WandControllerV3 wand = FindObjectOfType<WandControllerV3>();
        bool hasWand = wand != null && wand.IsHeldByRightHand();

        // Check for spell commands
        if (command.Contains("fire") || command.Contains("fireball") || command.Contains("flame"))
        {
            TryCastSpell("Fireball", hasWand, wand);
        }
        else if (command.Contains("ice") || command.Contains("freeze") || command.Contains("frost"))
        {
            TryCastSpell("Ice", hasWand, wand);
        }
        else if (command.Contains("shield") || command.Contains("protect") || command.Contains("defend"))
        {
            TryCastSpell("Shield", hasWand, wand);
        }
        else if (command.Contains("lightning") || command.Contains("thunder") || command.Contains("shock"))
        {
            TryCastSpell("Lightning", hasWand, wand);
        }
        else if (command.Contains("heal") || command.Contains("health"))
        {
            TryCastSpell("Heal", hasWand, wand);
        }
        else
        {
            Debug.Log($"❌ Unknown command: '{command}'");
        }
    }

    void TryCastSpell(string spellName, bool useWand, WandControllerV3 wand)
    {
        // Find spell in SpellManager
        if (SpellManager.Instance == null)
        {
            Debug.LogError("No SpellManager found!");
            return;
        }

        Spell spell = SpellManager.Instance.learnedSpells.Find(s =>
            s.spellName.ToLower().Contains(spellName.ToLower()));

        if (spell == null)
        {
            Debug.Log($"❌ No spell named '{spellName}' found");
            return;
        }

        Debug.Log($"🎤 Casting: {spell.spellName}");
        lastCastTime = Time.time;

        // Get casting position/direction
        Vector3 origin, direction;

        if (useWand && wand != null)
        {
            // Cast from wand
            origin = wand.transform.position + wand.transform.forward * 0.5f;
            direction = wand.transform.forward;
        }
        else
        {
            // Cast from player view
            Transform player = Camera.main?.transform;
            origin = player.position + player.forward * 0.5f;
            direction = player.forward;
        }

        // Cast the spell
        spell.Cast(origin, direction);

        // Visual feedback
        if (useWand && wand.spellTrail != null)
        {
            StartCoroutine(ShowVoiceFeedback(wand));
        }
    }

    System.Collections.IEnumerator ShowVoiceFeedback(WandControllerV3 wand)
    {
        TrailRenderer trail = wand.spellTrail;
        if (trail == null) yield break;

        Color originalColor = trail.startColor;
        trail.startColor = Color.cyan;
        trail.enabled = true;

        yield return new WaitForSeconds(0.3f);

        trail.enabled = false;
        trail.startColor = originalColor;
    }

    void OnDictationError(string error, int hresult)
    {
        Debug.LogError($"🎤 Voice error: {error}");

        // Auto-restart
        if (dictationRecognizer != null)
        {
            dictationRecognizer.Stop();
            Invoke(nameof(RestartListening), 1f);
        }
    }

    void RestartListening()
    {
        if (dictationRecognizer != null && dictationRecognizer.Status != SpeechSystemStatus.Running)
        {
            dictationRecognizer.Start();
            Debug.Log("🎤 Restarted voice listening");
        }
    }

    void OnDestroy()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult -= OnDictationResult;
            dictationRecognizer.DictationError -= OnDictationError;
            dictationRecognizer.Dispose();
        }
    }
}