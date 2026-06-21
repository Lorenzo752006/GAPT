using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOAPAgent : MonoBehaviour
{
    public Transform weapon;
    public Transform player;
    public float moveSpeed = 3f;

    private Dictionary<string, bool> worldState = new Dictionary<string, bool>();
    private Dictionary<string, bool> goal = new Dictionary<string, bool>();
    private List<GOAPAction> actions = new List<GOAPAction>();

    void Start()
    {
        worldState["HasWeapon"] = false;
        worldState["NearPlayer"] = false;
        worldState["PlayerDead"] = false;

        goal["PlayerDead"] = true;

        CreateActions();

        GOAPPlanner planner = new GOAPPlanner();
        Queue<GOAPAction> plan = planner.CreatePlan(worldState, goal, actions);

        StartCoroutine(ExecutePlan(plan));
    }

    void CreateActions()
    {
        GOAPAction findWeapon = new GOAPAction("Find Weapon");
        findWeapon.effects["HasWeapon"] = true;
        findWeapon.target = weapon;
        actions.Add(findWeapon);

        GOAPAction moveToPlayer = new GOAPAction("Move To Player");
        moveToPlayer.effects["NearPlayer"] = true;
        moveToPlayer.target = player;
        actions.Add(moveToPlayer);

        GOAPAction attackPlayer = new GOAPAction("Attack Player");
        attackPlayer.preconditions["HasWeapon"] = true;
        attackPlayer.preconditions["NearPlayer"] = true;
        attackPlayer.effects["PlayerDead"] = true;
        attackPlayer.target = player;
        actions.Add(attackPlayer);
    }

    IEnumerator ExecutePlan(Queue<GOAPAction> plan)
    {
        Debug.Log("GOAP Planner created plan:");

        while (plan.Count > 0)
        {
            GOAPAction action = plan.Dequeue();
            Debug.Log("Executing: " + action.actionName);

            if (action.target != null && action.actionName != "Attack Player")
            {
                yield return MoveToTarget(action.target);
            }

            foreach (KeyValuePair<string, bool> effect in action.effects)
            {
                worldState[effect.Key] = effect.Value;
            }

            if (action.actionName == "Find Weapon")
            {
                weapon.gameObject.SetActive(false);
            }

            Debug.Log("Completed: " + action.actionName);
        }

        if (worldState["PlayerDead"])
        {
            Debug.Log("Goal Achieved: Player defeated through GOAP planning");
        }
    }

    IEnumerator MoveToTarget(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }
}