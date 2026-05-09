using System.Collections.Generic;
using UnityEngine;

public class GOAPPlanner
{
    public Queue<GOAPAction> CreatePlan(
        Dictionary<string, bool> worldState,
        Dictionary<string, bool> goal,
        List<GOAPAction> actions)
    {
        Queue<GOAPAction> plan = new Queue<GOAPAction>();
        Dictionary<string, bool> currentState = new Dictionary<string, bool>(worldState);

        int safetyCounter = 0;

        while (!GoalAchieved(currentState, goal) && safetyCounter < 10)
        {
            bool foundAction = false;

            foreach (GOAPAction action in actions)
            {
                if (plan.Contains(action))
                    continue;

                if (CanExecute(action, currentState))
                {
                    plan.Enqueue(action);

                    foreach (var effect in action.effects)
                    {
                        currentState[effect.Key] = effect.Value;
                    }

                    foundAction = true;
                    break;
                }
            }

            if (!foundAction)
            {
                Debug.LogWarning("GOAP failed: no valid action found.");
                break;
            }

            safetyCounter++;
        }

        return plan;
    }

    bool CanExecute(GOAPAction action, Dictionary<string, bool> state)
    {
        foreach (var precondition in action.preconditions)
        {
            if (!state.ContainsKey(precondition.Key))
                return false;

            if (state[precondition.Key] != precondition.Value)
                return false;
        }

        return true;
    }

    bool GoalAchieved(Dictionary<string, bool> state, Dictionary<string, bool> goal)
    {
        foreach (var g in goal)
        {
            if (!state.ContainsKey(g.Key))
                return false;

            if (state[g.Key] != g.Value)
                return false;
        }

        return true;
    }
}