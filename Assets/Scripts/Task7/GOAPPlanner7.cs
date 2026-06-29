using System.Collections.Generic;

public class GOAPPlanner7
{
    public Queue<GOAPAction7> CreatePlan(
        Dictionary<string, bool> worldState,
        Dictionary<string, bool> goal,
        List<GOAPAction7> actions)
    {
        Queue<GOAPAction7> plan = new Queue<GOAPAction7>();

        Dictionary<string, bool> simulatedState = new Dictionary<string, bool>(worldState);

        foreach (GOAPAction7 action in actions)
        {
            if (PreconditionsMet(action, simulatedState))
            {
                plan.Enqueue(action);

                foreach (KeyValuePair<string, bool> effect in action.effects)
                {
                    simulatedState[effect.Key] = effect.Value;
                }

                if (GoalReached(goal, simulatedState))
                {
                    return plan;
                }
            }
        }

        return null;
    }

    private bool PreconditionsMet(GOAPAction7 action, Dictionary<string, bool> state)
    {
        foreach (KeyValuePair<string, bool> precondition in action.preconditions)
        {
            if (!state.ContainsKey(precondition.Key))
            {
                return false;
            }

            if (state[precondition.Key] != precondition.Value)
            {
                return false;
            }
        }

        return true;
    }

    private bool GoalReached(Dictionary<string, bool> goal, Dictionary<string, bool> state)
    {
        foreach (KeyValuePair<string, bool> goalState in goal)
        {
            if (!state.ContainsKey(goalState.Key))
            {
                return false;
            }

            if (state[goalState.Key] != goalState.Value)
            {
                return false;
            }
        }

        return true;
    }
}