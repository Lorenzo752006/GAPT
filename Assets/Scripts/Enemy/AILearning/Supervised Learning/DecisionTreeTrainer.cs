using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Simple decision-tree trainer using Gini impurity to find splits.
public class DecisionTreeTrainer : MonoBehaviour
{
    [SerializeField] private int maxDepth = 3;
    [SerializeField] private int minSamplesToSplit = 3;

    public DecisionTreeNode Train(List<TrainingSample> samples)
    {
        if (samples == null || samples.Count == 0)
            return null;

        return BuildNode(samples, 0);
    }

    DecisionTreeNode BuildNode(List<TrainingSample> samples, int depth)
    {
        DecisionTreeNode node = new DecisionTreeNode();
        node.predictedLabel = MajorityLabel(samples);

        // Stop if the node is already pure, too small, or too deep.
        if (depth >= maxDepth || samples.Count < minSamplesToSplit || IsPure(samples))
        {
            node.isLeaf = true;
            return node;
        }

        if (!TryFindBestSplit(samples, out int feature, out float threshold, out var left, out var right))
        {
            node.isLeaf = true;
            return node;
        }

        node.isLeaf = false;
        node.featureIndex = feature;
        node.threshold = threshold;

        node.left = BuildNode(left, depth + 1);
        node.right = BuildNode(right, depth + 1);

        return node;
    }

    bool TryFindBestSplit(List<TrainingSample> samples, out int bestFeature, out float bestThreshold,
        out List<TrainingSample> bestLeft, out List<TrainingSample> bestRight)
    {
        bestFeature = -1;
        bestThreshold = 0;
        bestLeft = null;
        bestRight = null;

        float bestGini = float.MaxValue;

        // Test each feature and each candidate threshold, then keep the split
        // that produces the lowest weighted impurity.
        for (int feature = 0; feature < 3; feature++)
        {
            var thresholds = GetThresholds(samples, feature);

            foreach (var t in thresholds)
            {
                Split(samples, feature, t, out var left, out var right);

                if (left.Count == 0 || right.Count == 0)
                    continue;

                float gini = WeightedGini(left, right);

                if (gini < bestGini)
                {
                    bestGini = gini;
                    bestFeature = feature;
                    bestThreshold = t;
                    bestLeft = left;
                    bestRight = right;
                }
            }
        }

        return bestFeature != -1;
    }

    List<float> GetThresholds(List<TrainingSample> samples, int feature)
    {
        var values = samples.Select(s => GetFeature(s, feature)).Distinct().OrderBy(v => v).ToList();
        List<float> thresholds = new List<float>();

        // Midpoints between sorted values are candidate split points.
        for (int i = 0; i < values.Count - 1; i++)
            thresholds.Add((values[i] + values[i + 1]) * 0.5f);

        return thresholds;
    }

    void Split(List<TrainingSample> samples, int feature, float threshold,
        out List<TrainingSample> left, out List<TrainingSample> right)
    {
        left = new List<TrainingSample>();
        right = new List<TrainingSample>();

        foreach (var s in samples)
        {
            if (GetFeature(s, feature) <= threshold)
                left.Add(s);
            else
                right.Add(s);
        }
    }

    float GetFeature(TrainingSample s, int index)
    {
        switch (index)
        {
            case 0: return s.enemyHealthPercent;
            case 1: return s.playerDistance;
            case 2: return s.canAttack;
        }

        return 0;
    }

    bool IsPure(List<TrainingSample> samples)
    {
        EnemyActionLabel label = samples[0].label;

        foreach (var s in samples)
            if (s.label != label)
                return false;

        return true;
    }

    EnemyActionLabel MajorityLabel(List<TrainingSample> samples)
    {
        return samples
            .GroupBy(s => s.label)
            .OrderByDescending(g => g.Count())
            .First().Key;
    }

    float WeightedGini(List<TrainingSample> left, List<TrainingSample> right)
    {
        int total = left.Count + right.Count;

        float wl = (float)left.Count / total;
        float wr = (float)right.Count / total;

        return wl * Gini(left) + wr * Gini(right);
    }

    float Gini(List<TrainingSample> samples)
    {
        float impurity = 1f;

        var groups = samples.GroupBy(s => s.label);

        foreach (var g in groups)
        {
            float p = (float)g.Count() / samples.Count;
            impurity -= p * p;
        }

        return impurity;
    }
}
