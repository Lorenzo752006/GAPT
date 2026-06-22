using System.Collections.Generic;

// Task 7 - GOAP planner (backward / regressive search).
//
// As the brief describes, this starts AT THE GOAL and works backward: for an
// unmet goal fact it finds an action whose effect produces that fact, then adds
// that action's preconditions as new sub-goals, chaining back until every
// remaining fact is already satisfied by the current world state. Among all
// valid chains it keeps the cheapest one (lowest summed action cost).
public class GOAPPlanner
{
    // Returns an ordered plan (first action to execute first), or null if the
    // goal cannot be reached from the current world state with these actions.
    public List<GOAPAction> Plan(
        Dictionary<string, bool> worldState,
        Dictionary<string, bool> goal,
        List<GOAPAction> actions)
    {
        List<GOAPAction> best = null;
        float bestCost = float.MaxValue;

        Regress(new Dictionary<string, bool>(goal), worldState, actions,
                new List<GOAPAction>(), 0f, ref best, ref bestCost, 0);

        return best;
    }

    private void Regress(
        Dictionary<string, bool> goals,
        Dictionary<string, bool> worldState,
        List<GOAPAction> actions,
        List<GOAPAction> chain,
        float cost,
        ref List<GOAPAction> best,
        ref float bestCost,
        int depth)
    {
        if (depth > 32) return; // safety against cyclic effect/precondition loops

        // Find the first goal fact not already satisfied by the world state.
        string unmetFact = null;
        bool unmetValue = false;
        foreach (var g in goals)
        {
            if (!worldState.TryGetValue(g.Key, out bool current) || current != g.Value)
            {
                unmetFact = g.Key;
                unmetValue = g.Value;
                break;
            }
        }

        // Nothing unmet -> the current world state already satisfies the goal.
        // The chain was built goal-first, so reverse it into execution order.
        if (unmetFact == null)
        {
            if (cost < bestCost)
            {
                bestCost = cost;
                best = new List<GOAPAction>(chain);
                best.Reverse();
            }
            return;
        }

        // Try every action that can produce the unmet fact.
        foreach (var action in actions)
        {
            if (chain.Contains(action)) continue; // don't reuse an action in one chain
            if (!action.effects.TryGetValue(unmetFact, out bool produced) || produced != unmetValue)
                continue;

            // Build the new sub-goal set: drop goals this action now satisfies,
            // then add this action's own preconditions as fresh sub-goals.
            var subGoals = new Dictionary<string, bool>(goals);
            foreach (var e in action.effects)
                if (subGoals.TryGetValue(e.Key, out bool gv) && gv == e.Value)
                    subGoals.Remove(e.Key);
            foreach (var p in action.preconditions)
                subGoals[p.Key] = p.Value;

            chain.Add(action);
            Regress(subGoals, worldState, actions, chain, cost + action.cost,
                    ref best, ref bestCost, depth + 1);
            chain.RemoveAt(chain.Count - 1);
        }
    }
}
