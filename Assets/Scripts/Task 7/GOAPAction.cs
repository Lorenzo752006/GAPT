using System;
using System.Collections.Generic;

// Task 7 - GOAP.
// A single action described purely by its preconditions and effects (the world
// represented as simple facts). The planner only ever reasons about these
// symbolic facts; HOW the action is actually carried out on the dungeon grid is
// supplied by the agent through the runtime delegates below, keeping the planner
// completely independent of movement/combat code.
public class GOAPAction
{
    public readonly string name;
    public readonly float cost;

    // World facts this action needs to be true before it can run.
    public readonly Dictionary<string, bool> preconditions = new Dictionary<string, bool>();
    // World facts this action makes true (or false) once completed.
    public readonly Dictionary<string, bool> effects = new Dictionary<string, bool>();

    // ---- Runtime hooks (assigned by GOAPAgent; ignored by the planner) ----
    public Action OnEnter;       // called once when the action becomes active
    public Action OnTick;        // called every frame while the action runs
    public Func<bool> IsComplete; // returns true once the action has finished

    public GOAPAction(string name, float cost = 1f)
    {
        this.name = name;
        this.cost = cost;
    }

    // Fluent helpers so action definitions read cleanly.
    public GOAPAction Pre(string fact, bool value) { preconditions[fact] = value; return this; }
    public GOAPAction Eff(string fact, bool value) { effects[fact] = value; return this; }
}
