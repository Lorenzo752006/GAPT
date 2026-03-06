using System;
using System.Collections.Generic;

// The action labels the decision tree can predict for the enemy.
public enum EnemyActionLabel
{
    Idle = 0,
    Chase = 1,
    Attack = 2,
    Flee = 3
}

[Serializable]
public class TrainingSample
{
    // Health stored as a 0..1 percentage so samples are scale-independent.
    public float enemyHealthPercent;

    // Distance from enemy to player at the time of recording.
    public float playerDistance;

    // Stored as 0/1 so it can be treated like a numeric feature by the tree.
    public int canAttack;

    // The teacher-provided label for this recorded state.
    public EnemyActionLabel label;
}

[Serializable]
public class TrainingSampleCollection
{
    // JsonUtility serializes wrapper classes more reliably than raw lists.
    public List<TrainingSample> samples = new List<TrainingSample>();
}

[Serializable]
public class DecisionTreeNode
{
    // Leaf nodes return a label directly. Non-leaf nodes split on a feature.
    public bool isLeaf;
    public int featureIndex;
    public float threshold;
    public EnemyActionLabel predictedLabel;

    public DecisionTreeNode left;
    public DecisionTreeNode right;
}
