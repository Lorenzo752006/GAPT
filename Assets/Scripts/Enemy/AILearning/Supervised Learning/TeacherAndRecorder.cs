using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Manual teacher and data recorder for Task 10.
// This script lets the user manually label enemy behaviour and record training samples.
public class TeacherAndRecorder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyLocomotionTask6 locomotion;
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Recording")]
    [SerializeField] private float recordInterval = 1f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private string fileName = "task10_trainingdata.json";

    [Header("Mode")]
    [SerializeField] private bool manualControlEnabled = true;
    [SerializeField] private bool recordingEnabled = true;

    private float timer;
    private TrainingSampleCollection collection = new TrainingSampleCollection();

    public EnemyActionLabel CurrentLabel { get; private set; } = EnemyActionLabel.Idle;

    void Update()
    {
        // Only allow manual label input while teacher mode is active.
        if (manualControlEnabled)
            HandleTeacherInput();

        // Record samples only while recording is enabled.
        if (recordingEnabled)
        {
            timer += Time.deltaTime;

            if (timer >= recordInterval)
            {
                timer = 0f;
                RecordSample();
            }
        }

        // O saves the dataset.
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
            SaveToFile();

        // P clears the currently recorded dataset.
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            ClearSamples();
    }

    void FixedUpdate()
    {
        // This is the important fix.
        // If manual control is disabled, this script stops controlling movement.
        // This allows DecisionTreeBrain to control the enemy without conflict.
        if (!manualControlEnabled)
            return;

        if (locomotion == null)
            return;

        switch (CurrentLabel)
        {
            case EnemyActionLabel.Idle:
                locomotion.SetFlee(false);
                locomotion.SetSpeedMultiplier(0f);
                locomotion.ClearTarget();
                locomotion.StopMovement();
                break;

            case EnemyActionLabel.Chase:
                if (player == null) return;

                locomotion.SetTarget(player);
                locomotion.SetFlee(false);
                locomotion.SetSpeedMultiplier(1f);
                break;

            case EnemyActionLabel.Attack:
                if (player == null) return;

                // Attack currently uses chase movement.
                // The important part for supervised learning is the recorded label.
                locomotion.SetTarget(player);
                locomotion.SetFlee(false);
                locomotion.SetSpeedMultiplier(1f);
                break;

            case EnemyActionLabel.Flee:
                if (player == null) return;

                locomotion.SetTarget(player);
                locomotion.SetFlee(true);
                locomotion.SetSpeedMultiplier(1f);
                break;
        }
    }

    void HandleTeacherInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            CurrentLabel = EnemyActionLabel.Idle;
            Debug.Log("Teacher label = Idle");
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            CurrentLabel = EnemyActionLabel.Chase;
            Debug.Log("Teacher label = Chase");
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            CurrentLabel = EnemyActionLabel.Attack;
            Debug.Log("Teacher label = Attack");
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
        {
            CurrentLabel = EnemyActionLabel.Flee;
            Debug.Log("Teacher label = Flee");
        }
    }

    void RecordSample()
    {
        if (player == null || enemyHealth == null)
            return;

        float hpPercent = 0f;

        if (enemyHealth.maxHealth > 0)
            hpPercent = enemyHealth.currentHealth / enemyHealth.maxHealth;

        float dist = Vector2.Distance(transform.position, player.position);
        int canAttack = dist <= attackRange ? 1 : 0;

        TrainingSample sample = new TrainingSample
        {
            enemyHealthPercent = hpPercent,
            playerDistance = dist,
            canAttack = canAttack,
            label = CurrentLabel
        };

        collection.samples.Add(sample);

        Debug.Log($"Recorded sample - HP:{hpPercent:F2} Dist:{dist:F2} Attack:{canAttack} Label:{CurrentLabel}");
    }

    void SaveToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        string json = JsonUtility.ToJson(collection, true);

        File.WriteAllText(path, json);

        Debug.Log($"Task10: Saved {collection.samples.Count} samples to {path}");
    }

    void ClearSamples()
    {
        collection.samples.Clear();
        Debug.Log("Task10: samples cleared.");
    }

    public TrainingSampleCollection LoadFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError("Task10: training file not found.");
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<TrainingSampleCollection>(json);
    }

    // Called by DecisionTreeBrain after training.
    // This prevents the teacher from fighting the trained AI.
    public void SetManualControlEnabled(bool enabled)
    {
        manualControlEnabled = enabled;
        Debug.Log("Task10: Manual teacher control = " + enabled);
    }

    // Called by DecisionTreeBrain after training.
    // Usually recording should stop once the trained AI takes over.
    public void SetRecordingEnabled(bool enabled)
    {
        recordingEnabled = enabled;
        Debug.Log("Task10: Recording = " + enabled);
    }
}