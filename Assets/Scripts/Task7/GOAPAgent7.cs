using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOAPAgent7 : MonoBehaviour
{
    public Transform weapon;
    public Transform player;
    public float moveSpeed = 3f;
    public float stopDistance = 0.2f;

    private Dictionary<string, bool> worldState = new Dictionary<string, bool>();
    private Dictionary<string, bool> goal = new Dictionary<string, bool>();
    private List<GOAPAction7> actions = new List<GOAPAction7>();

    void Start()
    {
        worldState["HasWeapon"] = false;
        worldState["NearPlayer"] = false;
        worldState["PlayerDead"] = false;

        goal["PlayerDead"] = true;

        CreateActions();

        GOAPPlanner7 planner = new GOAPPlanner7();
        Queue<GOAPAction7> plan = planner.CreatePlan(worldState, goal, actions);

        if (plan != null && plan.Count > 0)
        {
            StartCoroutine(ExecutePlan(plan));
        }
        else
        {
            Debug.LogWarning("No GOAP plan could be created.");
        }
    }

    void CreateActions()
    {
        actions.Clear();

        GOAPAction7 findWeapon = new GOAPAction7("Find Weapon");
        findWeapon.effects["HasWeapon"] = true;
        findWeapon.target = weapon;
        actions.Add(findWeapon);

        GOAPAction7 moveToPlayer = new GOAPAction7("Move To Player");
        moveToPlayer.preconditions["HasWeapon"] = true;
        moveToPlayer.effects["NearPlayer"] = true;
        moveToPlayer.target = player;
        actions.Add(moveToPlayer);

        GOAPAction7 attackPlayer = new GOAPAction7("Attack Player");
        attackPlayer.preconditions["HasWeapon"] = true;
        attackPlayer.preconditions["NearPlayer"] = true;
        attackPlayer.effects["PlayerDead"] = true;
        attackPlayer.target = player;
        actions.Add(attackPlayer);
    }

    IEnumerator ExecutePlan(Queue<GOAPAction7> plan)
    {
        Debug.Log("GOAP Planner created plan.");

        while (plan.Count > 0)
        {
            GOAPAction7 action = plan.Dequeue();
            Debug.Log("Executing: " + action.actionName);

            if (action.target != null && action.actionName != "Attack Player")
            {
                yield return MoveToTarget2D(action.target);
            }

            foreach (KeyValuePair<string, bool> effect in action.effects)
            {
                worldState[effect.Key] = effect.Value;
            }

            if (action.actionName == "Find Weapon" && weapon != null)
            {
                weapon.gameObject.SetActive(false);
            }

            if (action.actionName == "Attack Player" && player != null)
            {
                Debug.Log("Player attacked.");
            }

            Debug.Log("Completed: " + action.actionName);
        }

        if (worldState["PlayerDead"])
        {
            Debug.Log("Goal Achieved: Player defeated through GOAP planning.");
        }
    }

    IEnumerator MoveToTarget2D(Transform target)
    {
        while (Vector2.Distance(transform.position, target.position) > stopDistance)
        {
            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = target.position;

            Vector2 newPosition = Vector2.MoveTowards(
                currentPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            transform.position = new Vector3(newPosition.x, newPosition.y, 0f);

            yield return null;
        }
    }
}