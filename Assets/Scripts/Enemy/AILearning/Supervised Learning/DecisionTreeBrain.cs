using UnityEngine;
using UnityEngine.InputSystem;

// Decision-tree controller for Task 10.
// This script loads a recorded dataset, trains a decision tree,
// then uses the tree to control the enemy.
public class DecisionTreeBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyLocomotionTask6 locomotion;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private DecisionTreeTrainer trainer;
    [SerializeField] private TeacherAndRecorder recorder;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.2f;

    [Header("Flee Safety")]
    [SerializeField] private float lowHealthFleeThreshold = 0.3f;

    [Header("Reset Settings")]
    [SerializeField] private Transform resetPoint;
    [SerializeField] private bool resetPositionOnLoad = true;
    [SerializeField] private bool resetHealthOnLoad = true;

    [Header("Debug")]
    [SerializeField] private bool printPredictions = true;

    // Root node of the trained decision tree.
    private DecisionTreeNode root;

    // Starting position and rotation used if no reset point is assigned.
    private Vector3 startingPosition;
    private Quaternion startingRotation;

    void Start()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;
    }

    void Update()
    {
        // Press L to load the saved dataset, reset the enemy, and train the tree.
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            TrainTree();
    }

    void FixedUpdate()
    {
        // Do nothing before the decision tree is trained.
        // This allows TeacherAndRecorder to control the enemy during recording.
        if (root == null)
            return;

        if (locomotion == null)
            return;

        // Build the current state that the tree will classify.
        TrainingSample sample = BuildSample();

        // Predict an action from the decision tree.
        EnemyActionLabel action = Predict(root, sample);

        // Safety correction:
        // The enemy is not allowed to flee unless health is actually low.
        if (action == EnemyActionLabel.Flee && sample.enemyHealthPercent > lowHealthFleeThreshold)
        {
            action = EnemyActionLabel.Chase;
        }

        if (printPredictions)
        {
            Debug.Log(
                $"DecisionTree Prediction: {action} | " +
                $"HP:{sample.enemyHealthPercent:F2} | " +
                $"Distance:{sample.playerDistance:F2} | " +
                $"CanAttack:{sample.canAttack}"
            );
        }

        // Execute the chosen action through the locomotion system.
        switch (action)
        {
            case EnemyActionLabel.Idle:
                SetIdleState();
                break;

            case EnemyActionLabel.Chase:
                if (player == null) return;

                locomotion.SetTarget(player);
                locomotion.SetSpeedMultiplier(1f);
                locomotion.SetFlee(false);
                break;

            case EnemyActionLabel.Attack:
                if (player == null) return;

                locomotion.SetTarget(player);
                locomotion.SetSpeedMultiplier(1f);
                locomotion.SetFlee(false);
                break;

            case EnemyActionLabel.Flee:
                if (player == null) return;

                locomotion.SetTarget(player);
                locomotion.SetSpeedMultiplier(1f);
                locomotion.SetFlee(true);
                break;
        }
    }

    void SetIdleState()
    {
        if (locomotion == null)
            return;

        locomotion.SetFlee(false);
        locomotion.SetSpeedMultiplier(0f);
        locomotion.ClearTarget();
        locomotion.StopMovement();
    }

    void TrainTree()
    {
        if (recorder == null || trainer == null)
        {
            Debug.LogError("Task10: missing recorder or trainer reference.");
            return;
        }

        TrainingSampleCollection data = recorder.LoadFromFile();

        if (data == null || data.samples == null || data.samples.Count == 0)
        {
            Debug.LogError("Task10: no training samples.");
            return;
        }

        ResetEnemyForDecisionTree();

        PrintLabelCounts(data);

        root = trainer.Train(data.samples);

        if (root == null)
        {
            Debug.LogError("Task10: tree training failed.");
            return;
        }

        recorder.SetManualControlEnabled(false);
        recorder.SetRecordingEnabled(false);

        Debug.Log("Task10: Decision tree trained. Enemy reset. Teacher control disabled. Decision tree now controls enemy.");
    }

    void ResetEnemyForDecisionTree()
    {
        SetIdleState();

        if (resetPositionOnLoad)
        {
            if (resetPoint != null)
            {
                transform.position = resetPoint.position;
                transform.rotation = resetPoint.rotation;
            }
            else
            {
                transform.position = startingPosition;
                transform.rotation = startingRotation;
            }
        }

        if (resetHealthOnLoad && enemyHealth != null)
        {
            enemyHealth.ResetHealthToFull();
        }

        Debug.Log("Task10: Enemy position and health reset.");
    }

    TrainingSample BuildSample()
    {
        float hp = 0f;

        if (enemyHealth != null && enemyHealth.maxHealth > 0)
            hp = enemyHealth.currentHealth / enemyHealth.maxHealth;

        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : 999f;

        int canAttack = dist <= attackRange ? 1 : 0;

        return new TrainingSample
        {
            enemyHealthPercent = hp,
            playerDistance = dist,
            canAttack = canAttack
        };
    }

    EnemyActionLabel Predict(DecisionTreeNode node, TrainingSample sample)
    {
        if (node == null)
            return EnemyActionLabel.Idle;

        if (node.isLeaf)
            return node.predictedLabel;

        float value;

        if (node.featureIndex == 0)
            value = sample.enemyHealthPercent;
        else if (node.featureIndex == 1)
            value = sample.playerDistance;
        else
            value = sample.canAttack;

        if (value <= node.threshold)
            return Predict(node.left, sample);

        return Predict(node.right, sample);
    }

    void PrintLabelCounts(TrainingSampleCollection data)
    {
        int idle = 0;
        int chase = 0;
        int attack = 0;
        int flee = 0;

        foreach (TrainingSample sample in data.samples)
        {
            switch (sample.label)
            {
                case EnemyActionLabel.Idle:
                    idle++;
                    break;

                case EnemyActionLabel.Chase:
                    chase++;
                    break;

                case EnemyActionLabel.Attack:
                    attack++;
                    break;

                case EnemyActionLabel.Flee:
                    flee++;
                    break;
            }
        }

        Debug.Log($"Task10 Label Counts - Idle:{idle}, Chase:{chase}, Attack:{attack}, Flee:{flee}");
    }
}