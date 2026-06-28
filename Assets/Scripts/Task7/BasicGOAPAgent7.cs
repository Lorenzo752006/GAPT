using System.Collections;
using UnityEngine;

public class BasicGOAPAgent7 : MonoBehaviour
{
    public Transform weapon;
    public Transform player;
    public float moveSpeed = 3f;
    public float stopDistance = 0.2f;

    void Start()
    {
        StartCoroutine(BasicSequence());
    }

    IEnumerator BasicSequence()
    {
        Debug.Log("Basic GOAP: Find Weapon");
        yield return MoveToTarget2D(weapon);

        if (weapon != null)
        {
            weapon.gameObject.SetActive(false);
        }

        Debug.Log("Basic GOAP: Move To Player");
        yield return MoveToTarget2D(player);

        Debug.Log("Basic GOAP: Attack Player");
        Debug.Log("Basic GOAP Goal Achieved: Player defeated.");
    }

    IEnumerator MoveToTarget2D(Transform target)
    {
        while (target != null && Vector2.Distance(transform.position, target.position) > stopDistance)
        {
            Vector2 newPosition = Vector2.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

            transform.position = new Vector3(newPosition.x, newPosition.y, 0f);
            yield return null;
        }
    }
}