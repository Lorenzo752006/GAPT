using UnityEngine;

public class BasicImitationAgent : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3f;
    public float targetDistance = 0.3f;

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < targetDistance)
        {
            transform.position = Vector3.zero;
        }
    }
}