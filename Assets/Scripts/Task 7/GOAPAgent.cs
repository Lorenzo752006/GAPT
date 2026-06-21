using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Task 7 - Goal-Oriented Action Planning, integrated with the shared dungeon.
//
//   BASIC  (useComplexAI = false): a hardcoded, fixed script of steps
//          (Pick up weapon -> Move to player -> Attack). It never re-evaluates,
//          so if the player moves after it "arrives" it whiffs - the naive baseline.
//
//   COMPLEX (useComplexAI = true): only a GOAL ("PlayerDefeated") and a toolbox of
//          actions. GOAPPlanner formulates a plan by working backward from the goal,
//          and the agent continuously replans, so it skips the weapon if already armed
//          and re-approaches a fleeing player.
//
// Navigation uses the shared GridManager + Pathfinder (A*); combat uses the shared
// PlayerHealth. Drop this on an enemy in any scene that has a GridManager and a
// "Player"-tagged object.
[DisallowMultipleComponent]
public class GOAPAgent : MonoBehaviour
{
    [Header("AI Mode (Basic vs Complex)")]
    public bool useComplexAI = true;
    [Tooltip("Press T at runtime to flip Basic/Complex.")]
    public bool allowRuntimeToggle = true;

    [Header("References")]
    [Tooltip("Optional weapon pickup. If left empty the agent is considered already armed.")]
    public Transform weaponPickup;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float repathInterval = 0.3f;

    [Header("Combat")]
    public float attackInterval = 0.5f;
    public float attackDamage = 20f;
    [Tooltip("Basic mode gives up an attack after this long so it can't hang on a whiff.")]
    public float basicAttackTimeout = 3f;

    // --- World facts ---
    private readonly Dictionary<string, bool> worldState = new Dictionary<string, bool>();
    private readonly Dictionary<string, bool> goal = new Dictionary<string, bool>();
    private readonly List<GOAPAction> actions = new List<GOAPAction>();

    // --- Plumbing ---
    private GridManager gm;
    private Pathfinder pathfinder;
    private Transform player;
    private PlayerHealth playerHealth;

    private bool hasWeapon;

    // Planner / executor state
    private GOAPAction current;
    private bool currentEntered;
    private List<GOAPAction> plan = new List<GOAPAction>();

    // Basic-mode fixed script
    private List<GOAPAction> basicSequence;
    private int basicIndex;

    // Movement state
    private List<Vector2Int> path;
    private int pathIdx;
    private Vector2Int pathGoalCell;
    private bool hasPathGoal;
    private float repathTimer;

    // Combat state
    private float attackTimer;
    private float attackElapsed;

    private string status = "";

    private void Start()
    {
        gm = GridManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[GOAPAgent] No GridManager in scene - disabling. Place this in a dungeon scene.");
            enabled = false;
            return;
        }

        pathfinder = FindFirstObjectByType<Pathfinder>();
        if (pathfinder == null) pathfinder = gameObject.AddComponent<Pathfinder>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
        if (player == null)
            Debug.LogWarning("[GOAPAgent] No object tagged 'Player' found.");

        hasWeapon = (weaponPickup == null); // no weapon in scene => already armed

        goal["PlayerDefeated"] = true;
        BuildActions();
        BuildBasicSequence();
    }

    // ---------------------------------------------------------------- actions
    private void BuildActions()
    {
        // Pick up the weapon: walks onto the weapon cell, then latches HasWeapon.
        var pickUp = new GOAPAction("PickUpWeapon", 1f).Eff("HasWeapon", true);
        pickUp.OnTick = () =>
        {
            if (weaponPickup == null) return;
            MoveToCell(gm.WorldToGrid(weaponPickup.position), stopAdjacent: false);
        };
        pickUp.IsComplete = () =>
        {
            if (weaponPickup == null) return true;
            bool reached = GridDistance(CurrentCell, gm.WorldToGrid(weaponPickup.position)) == 0;
            if (reached)
            {
                hasWeapon = true;
                weaponPickup.gameObject.SetActive(false);
            }
            return reached;
        };
        actions.Add(pickUp);

        // Close the distance to the player.
        var approach = new GOAPAction("MoveToPlayer", 1f).Eff("NearPlayer", true);
        Vector2Int committedPlayerCell = default;
        bool committed = false;
        approach.OnEnter = () => { committed = false; };
        approach.OnTick = () =>
        {
            if (player == null) return;
            // Complex: chase the live player. Basic: commit to where the player WAS.
            if (useComplexAI)
            {
                MoveToCell(gm.WorldToGrid(player.position), stopAdjacent: true);
            }
            else
            {
                if (!committed) { committedPlayerCell = gm.WorldToGrid(player.position); committed = true; }
                MoveToCell(committedPlayerCell, stopAdjacent: false);
            }
        };
        approach.IsComplete = () =>
        {
            if (player == null) return true;
            if (useComplexAI) return GridDistance(CurrentCell, gm.WorldToGrid(player.position)) <= 1;
            return committed && CurrentCell == committedPlayerCell;
        };
        actions.Add(approach);

        // Attack: requires a weapon AND being next to the player.
        var attack = new GOAPAction("AttackPlayer", 1f)
            .Pre("HasWeapon", true).Pre("NearPlayer", true).Eff("PlayerDefeated", true);
        attack.OnEnter = () => { attackTimer = 0f; attackElapsed = 0f; };
        attack.OnTick = () =>
        {
            attackElapsed += Time.deltaTime;
            attackTimer -= Time.deltaTime;
            bool adjacent = player != null && GridDistance(CurrentCell, gm.WorldToGrid(player.position)) <= 1;
            if (adjacent && attackTimer <= 0f && playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                attackTimer = attackInterval;
            }
        };
        attack.IsComplete = () =>
        {
            if (PlayerDefeated()) return true;
            // Basic mode gives up after a timeout so a whiff doesn't hang the script.
            return !useComplexAI && attackElapsed >= basicAttackTimeout;
        };
        actions.Add(attack);
    }

    private void BuildBasicSequence()
    {
        // A naive, fixed plan: it ALWAYS does these three in order, no matter what.
        GOAPAction Find(string n) => actions.Find(a => a.name == n);
        basicSequence = new List<GOAPAction> { Find("PickUpWeapon"), Find("MoveToPlayer"), Find("AttackPlayer") };
        basicIndex = 0;
    }

    // ---------------------------------------------------------------- update
    private void Update()
    {
        if (allowRuntimeToggle && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            ToggleMode();

        RefreshWorldState();

        if (PlayerDefeated()) { status = "Goal reached: player defeated."; ClearMovement(); return; }

        if (useComplexAI) TickComplex();
        else TickBasic();
    }

    private void TickComplex()
    {
        // (Re)plan whenever we have no action in progress.
        if (current == null)
        {
            plan = pathfinder != null ? new GOAPPlanner().Plan(worldState, goal, actions) : null;
            if (plan == null || plan.Count == 0) { status = "COMPLEX: no valid plan."; return; }
            BeginAction(plan[0]);
        }

        // Abort if reality invalidated the running action's preconditions (e.g. player fled).
        if (current != null && !PreconditionsHold(current)) { current = null; return; }

        RunCurrent("COMPLEX");
    }

    private void TickBasic()
    {
        if (basicIndex >= basicSequence.Count) { status = "BASIC: script finished."; return; }
        if (current == null) BeginAction(basicSequence[basicIndex]);
        RunCurrent("BASIC");
        if (current == null) basicIndex++; // RunCurrent cleared it on completion
    }

    private void RunCurrent(string mode)
    {
        if (current == null) return;
        current.OnTick?.Invoke();
        status = $"{mode}: {current.name}  [{PlanPreview()}]";
        if (current.IsComplete == null || current.IsComplete())
        {
            // Apply symbolic effects (sensed facts will also refresh next frame).
            foreach (var e in current.effects) worldState[e.Key] = e.Value;
            current = null;
            ClearMovement();
        }
    }

    private void BeginAction(GOAPAction a)
    {
        current = a;
        ClearMovement();
        a.OnEnter?.Invoke();
    }

    // ---------------------------------------------------------------- world facts
    private void RefreshWorldState()
    {
        worldState["HasWeapon"] = hasWeapon;
        worldState["NearPlayer"] = player != null && GridDistance(CurrentCell, gm.WorldToGrid(player.position)) <= 1;
        worldState["PlayerDefeated"] = PlayerDefeated();
    }

    private bool PlayerDefeated() => playerHealth != null && playerHealth.currentHealth <= 0f;

    private bool PreconditionsHold(GOAPAction a)
    {
        foreach (var p in a.preconditions)
            if (!worldState.TryGetValue(p.Key, out bool v) || v != p.Value) return false;
        return true;
    }

    // ---------------------------------------------------------------- movement
    private Vector2Int CurrentCell => gm.WorldToGrid(transform.position);

    private static int GridDistance(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    // Steps one frame's worth of movement toward goalCell along an A* path.
    private void MoveToCell(Vector2Int goalCell, bool stopAdjacent)
    {
        Vector2Int cur = CurrentCell;
        if (stopAdjacent && GridDistance(cur, goalCell) <= 1) return;
        if (!stopAdjacent && cur == goalCell) return;

        repathTimer -= Time.deltaTime;
        if (!hasPathGoal || pathGoalCell != goalCell || repathTimer <= 0f)
        {
            path = pathfinder.FindPath(cur, goalCell);
            pathGoalCell = goalCell;
            hasPathGoal = true;
            pathIdx = (path != null && path.Count > 1) ? 1 : 0; // index 0 is the current cell
            repathTimer = repathInterval;
        }

        if (path == null || pathIdx >= path.Count) return; // unreachable

        Vector3 wp = gm.GridToWorld(path[pathIdx].x, path[pathIdx].y);
        transform.position = Vector3.MoveTowards(transform.position, wp, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, wp) < 0.05f) pathIdx++;
    }

    private void ClearMovement()
    {
        path = null;
        hasPathGoal = false;
        pathIdx = 0;
    }

    // ---------------------------------------------------------------- helpers / UI
    public void ToggleMode()
    {
        useComplexAI = !useComplexAI;
        current = null;
        basicIndex = 0;
        ClearMovement();
    }

    private string PlanPreview()
    {
        if (useComplexAI)
            return plan == null ? "-" : string.Join(" > ", plan.ConvertAll(a => a.name));
        var names = new List<string>();
        for (int i = basicIndex; i < basicSequence.Count; i++) names.Add(basicSequence[i].name);
        return string.Join(" > ", names);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 420, 64), "Task 7 - GOAP");
        GUI.Label(new Rect(20, 30, 400, 20), $"Mode: {(useComplexAI ? "COMPLEX (planner)" : "BASIC (fixed script)")}   (press T)");
        GUI.Label(new Rect(20, 50, 400, 20), status);
    }

    private void OnDrawGizmosSelected()
    {
        if (path == null || GridManager.Instance == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < path.Count; i++)
            Gizmos.DrawWireSphere(GridManager.Instance.GridToWorld(path[i].x, path[i].y), 0.12f);
    }
}
