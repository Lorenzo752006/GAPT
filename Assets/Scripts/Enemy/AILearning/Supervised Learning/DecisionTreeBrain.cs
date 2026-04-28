using UnityEngine;
using UnityEngine.InputSystem;

/*
    DecisionTreeBrain

    This script controls the enemy behaviour using a trained Decision Tree.
    The tree is trained from previously recorded gameplay data (Task 10 dataset).

    Workflow:
    1. At the start of the game the enemy remains idle.
    2. Pressing the "L" key loads the dataset and trains the decision tree.
    3. Once trained, the tree predicts which action the enemy should take
       based on the current game state (health, distance, attack possibility).
    4. The predicted action is then executed through the locomotion system.
*/

public class DecisionTreeBrain : MonoBehaviour
{
    [Header("References")]

    // Reference to the player so distance can be calculated
    [SerializeField] private Transform player;

    // Locomotion system that actually moves the enemy
    [SerializeField] private EnemyLocomotionTask6 locomotion;

    // Enemy health script used to read current health
    [SerializeField] private EnemyHealth enemyHealth;

    // Trainer used to build the decision tree from data
    [SerializeField] private DecisionTreeTrainer trainer;

    // Recorder used to load the previously recorded training dataset
    [SerializeField] private TeacherAndRecorder recorder;

    [Header("Combat Settings")]

    // Distance at which the enemy is considered able to attack
    [SerializeField] private float attackRange = 1.2f;

    // Root node of the trained decision tree
    private DecisionTreeNode root;

    /*
        Start()

        Runs once when the enemy is created.
        The enemy begins in an idle state until the tree is trained.
    */
    void Start()
    {
        SetIdleState();
    }

    /*
        Update()

        Checks for keyboard input using the new Input System.
        Pressing "L" trains the decision tree using the saved dataset.
    */
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            TrainTree();
    }

    /*
        FixedUpdate()

        Runs every physics frame.
        If the tree is not trained yet, the enemy remains idle.
        Once trained, the tree predicts an action based on the current state.
    */
    void FixedUpdate()
    {
        // If the tree hasn't been trained yet, keep enemy idle
        if (root == null)
        {
            SetIdleState();
            return;
        }

        if (locomotion == null)
            return;

        // Build a snapshot of the current world state
        TrainingSample sample = BuildSample();

        // Ask the decision tree what action should be taken
        EnemyActionLabel action = Predict(root, sample);

        // Execute the predicted action
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

    /*
        SetIdleState()

        Forces the enemy into a complete idle state.
        This prevents drift or movement before the AI is trained.
    */
    void SetIdleState()
    {
        if (locomotion == null)
            return;

        locomotion.SetFlee(false);
        locomotion.SetSpeedMultiplier(0f);
        locomotion.ClearTarget();
        locomotion.StopMovement();
    }

    /*
        TrainTree()

        Loads the recorded dataset and trains the decision tree.
        The tree is stored in the 'root' node for later prediction.
    */
    void TrainTree()
    {
        var data = recorder.LoadFromFile();

        if (data == null || data.samples.Count == 0)
        {
            Debug.LogError("Task10: no training samples.");
            return;
        }

        // Train the decision tree
        root = trainer.Train(data.samples);

        Debug.Log("Task10: Decision tree trained.");
    }

    /*
        BuildSample()

        Builds the current world-state snapshot that will be fed
        into the decision tree for prediction.

        Features used by the classifier:
        - enemy health percentage
        - distance to player
        - whether the enemy can attack
    */
    TrainingSample BuildSample()
    {
        float hp = 0f;

        // Convert health to percentage
        if (enemyHealth.maxHealth > 0)
            hp = enemyHealth.currentHealth / enemyHealth.maxHealth;

        // Distance between enemy and player
        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : 999f;

        // Determine whether enemy is close enough to attack
        int canAttack = dist <= attackRange ? 1 : 0;

        // Create and return a sample describing this state
        return new TrainingSample
        {
            enemyHealthPercent = hp,
            playerDistance = dist,
            canAttack = canAttack
        };
    }

    /*
        Predict()

        Traverses the decision tree recursively until a leaf node is reached.
        The leaf node contains the predicted action label.
    */
    EnemyActionLabel Predict(DecisionTreeNode node, TrainingSample sample)
    {
        // If the node is a leaf, return its prediction
        if (node.isLeaf)
            return node.predictedLabel;

        float value = 0f;

        // Select which feature to evaluate
        if (node.featureIndex == 0)
            value = sample.enemyHealthPercent;
        else if (node.featureIndex == 1)
            value = sample.playerDistance;
        else
            value = sample.canAttack;

        // Traverse the appropriate branch of the tree
        if (value <= node.threshold)
            return Predict(node.left, sample);

        return Predict(node.right, sample);
    }
}