using UnityEngine;
using System;
using System.Collections.Generic;

public enum SimpleState { Wander, Chase, Attack, Flee }
public enum ComplexityMode { Basic, Complex }

public class EnemyFSM : MonoBehaviour
{
    [Header("AI Settings")]
    public ComplexityMode aiMode = ComplexityMode.Complex;
    public SimpleState currentStateType = SimpleState.Wander;
    
    [Header("Stats")]
    public float health = 100f;
    public float fleeThreshold = 25f; 
    public float chaseRange = 5f;
    public float attackRange = 1.2f;

    [HideInInspector] public GridPlayerController player;
    [HideInInspector] public Vector3 targetWorldPos;
    [HideInInspector] public bool isMoving = false;

    private IEnemyState currentState;
    private Dictionary<SimpleState, IEnemyState> stateMap;

    private Dictionary<ComplexityMode, IMovementStrategy> movementStrategies;

    void Start() {
        player = FindFirstObjectByType<GridPlayerController>();
        
        int spawnX = GridManager.Instance.Width - 2;
        int spawnY = GridManager.Instance.Height - 2;
        transform.position = GridManager.Instance.GridToWorld(spawnX, spawnY);

        stateMap = new Dictionary<SimpleState, IEnemyState>
        {
            { SimpleState.Wander, new WanderState(this) },
            { SimpleState.Chase,  new ChaseState(this) },
            { SimpleState.Attack, new AttackState(this) },
            { SimpleState.Flee,   new FleeState(this) }
        };

        movementStrategies = new Dictionary<ComplexityMode, IMovementStrategy>
        {
            { ComplexityMode.Basic,   new BasicMovementStrategy() },
            { ComplexityMode.Complex, new ComplexMovementStrategy() }
        };

        TransitionToState(SimpleState.Wander);
    }

    void Update(){
        bool regularUpdate = !isMoving;
        ExecuteMovementInterpolation();

        bool runAI = regularUpdate && ExecuteAILogic();
    }

    private void ExecuteMovementInterpolation()
    {
        bool process = isMoving && PerformStep();
    }

    private bool PerformStep()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, Time.deltaTime * 2f);
        isMoving = Vector3.Distance(transform.position, targetWorldPos) >= 0.01f;
        
        transform.position = isMoving ? transform.position : targetWorldPos;
        return true;
    }

    private bool ExecuteAILogic()
    {
        currentState.ExecuteState();
        currentState.CheckGuardConditions();
        return true;
    }

    public void TransitionToState(SimpleState newState)
    {
        currentState?.Exit();
        currentStateType = newState;
        currentState = stateMap[newState];
        currentState.Enter();
    }

    public void HandleGridMove(bool away)
    {
        Vector2Int currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        Vector2Int playerGridPos = GridManager.Instance.WorldToGrid(player.transform.position);

        Vector2Int moveDir = movementStrategies[aiMode].CalculateMove(currentGridPos, playerGridPos, away);
        Vector2Int targetGrid = currentGridPos + moveDir;

        bool walkable = GridManager.Instance.IsWalkable(targetGrid.x, targetGrid.y);
        targetWorldPos = walkable ? GridManager.Instance.GridToWorld(targetGrid.x, targetGrid.y) : targetWorldPos;
        isMoving = walkable ? true : isMoving;
    }
}

#region FSM State Engine Framework

public interface IEnemyState
{
    void Enter();
    void ExecuteState();
    void CheckGuardConditions();
    void Exit();
}

public class WanderState : IEnemyState
{
    private EnemyFSM ai;
    public WanderState(EnemyFSM fsm) { this.ai = fsm; }
    public void Enter() => Debug.Log("Wandering Started");
    public void ExecuteState() {}
    public void Exit() {}

    public void CheckGuardConditions()
    {
        float distance = Vector3.Distance(ai.transform.position, ai.player.transform.position);

        bool lowHealth = ai.health < ai.fleeThreshold;
        bool inChaseRange = distance < ai.chaseRange;
        bool inAttackRange = distance < ai.attackRange;

        SimpleState target = SimpleState.Wander;
        target = inChaseRange ? SimpleState.Chase : target;
        target = inAttackRange ? SimpleState.Attack : target;
        target = (lowHealth && inChaseRange) ? SimpleState.Flee : target;

        bool changeState = target != SimpleState.Wander;
        Action switchAction = () => ai.TransitionToState(target);
        
        Dictionary<bool, Action> conditionalRun = new Dictionary<bool, Action> { { true, switchAction }, { false, () => {} } };
        conditionalRun[changeState]();
    }
}

public class ChaseState : IEnemyState
{
    private EnemyFSM ai;
    public ChaseState(EnemyFSM fsm) { this.ai = fsm; }
    public void Enter() => Debug.Log($"Chasing Started ({ai.aiMode} Mode)");
    public void ExecuteState() => ai.HandleGridMove(false);
    public void Exit() {}

    public void CheckGuardConditions()
    {
        float distance = Vector3.Distance(ai.transform.position, ai.player.transform.position);
        bool lowHealth = ai.health < ai.fleeThreshold;
        bool inChaseRange = distance < ai.chaseRange;
        bool inAttackRange = distance < ai.attackRange;

        SimpleState target = SimpleState.Wander;
        target = inChaseRange ? SimpleState.Chase : target;
        target = inAttackRange ? SimpleState.Attack : target;
        target = (lowHealth && inChaseRange) ? SimpleState.Flee : target;

        bool changeState = target != SimpleState.Chase;
        Dictionary<bool, Action> conditionalRun = new Dictionary<bool, Action> { { true, () => ai.TransitionToState(target) }, { false, () => {} } };
        conditionalRun[changeState]();
    }
}

public class AttackState : IEnemyState
{
    private EnemyFSM ai;
    public AttackState(EnemyFSM fsm) { this.ai = fsm; }
    public void Enter() => Debug.Log("Attacking Started");
    public void ExecuteState() {}
    public void Exit() {}

    public void CheckGuardConditions()
    {
        float distance = Vector3.Distance(ai.transform.position, ai.player.transform.position);
        bool lowHealth = ai.health < ai.fleeThreshold;
        bool inChaseRange = distance < ai.chaseRange;
        bool inAttackRange = distance < ai.attackRange;

        SimpleState target = SimpleState.Wander;
        target = inChaseRange ? SimpleState.Chase : target;
        target = inAttackRange ? SimpleState.Attack : target;
        target = (lowHealth && inChaseRange) ? SimpleState.Flee : target;

        bool changeState = target != SimpleState.Attack;
        Dictionary<bool, Action> conditionalRun = new Dictionary<bool, Action> { { true, () => ai.TransitionToState(target) }, { false, () => {} } };
        conditionalRun[changeState]();
    }
}

public class FleeState : IEnemyState
{
    private EnemyFSM ai;
    public FleeState(EnemyFSM fsm) { this.ai = fsm; }
    public void Enter() => Debug.Log($"Fleeing Started ({ai.aiMode} Mode)");
    public void ExecuteState() => ai.HandleGridMove(true);
    public void Exit() {}

    public void CheckGuardConditions()
    {
        float distance = Vector3.Distance(ai.transform.position, ai.player.transform.position);
        bool lowHealth = ai.health < ai.fleeThreshold;
        bool inChaseRange = distance < ai.chaseRange;
        bool inAttackRange = distance < ai.attackRange;

        bool maintainFlee = lowHealth && inChaseRange;
        
        SimpleState breakTarget = SimpleState.Wander;
        breakTarget = inChaseRange ? SimpleState.Chase : breakTarget;
        breakTarget = inAttackRange ? SimpleState.Attack : breakTarget;

        SimpleState finalTarget = maintainFlee ? SimpleState.Flee : breakTarget;

        bool changeState = finalTarget != SimpleState.Flee;
        Dictionary<bool, Action> conditionalRun = new Dictionary<bool, Action> { { true, () => ai.TransitionToState(finalTarget) }, { false, () => {} } };
        conditionalRun[changeState]();
    }
}
#endregion

#region Pathfinding Strategy Classes (Replaces AI Mode checks)

public interface IMovementStrategy
{
    Vector2Int CalculateMove(Vector2Int current, Vector2Int player, bool away);
}

public class BasicMovementStrategy : IMovementStrategy
{
    public Vector2Int CalculateMove(Vector2Int current, Vector2Int player, bool away)
    {
        Vector2Int moveDir = Vector2Int.zero;

        int deltaX = player.x - current.x;
        int deltaY = player.y - current.y;

        Vector2Int chaseDir = new Vector2Int(
            deltaX != 0 ? Math.Sign(deltaX) : 0,
            (deltaX == 0 && deltaY != 0) ? Math.Sign(deltaY) : 0
        );

        Vector2Int fleeDir = new Vector2Int(current.x <= player.x ? -1 : 1, 0);

        return away ? fleeDir : chaseDir;
    }
}

public class ComplexMovementStrategy : IMovementStrategy
{
    public Vector2Int CalculateMove(Vector2Int current, Vector2Int player, bool away)
    {
        int deltaX = player.x - current.x;
        int deltaY = player.y - current.y;

        Vector2Int chaseDir = new Vector2Int(
            deltaX != 0 ? Math.Sign(deltaX) : 0,
            (deltaX == 0 && deltaY != 0) ? Math.Sign(deltaY) : 0
        );

        int fleeX = current.x <= player.x ? -1 : 1;
        bool obstacleAhead = !GridManager.Instance.IsWalkable(current.x + fleeX, current.y);

        int fallbackY = current.y <= player.y ? -1 : 1;
        Vector2Int fleeDir = obstacleAhead ? new Vector2Int(0, fallbackY) : new Vector2Int(fleeX, 0);

        return away ? fleeDir : chaseDir;
    }
}
#endregion