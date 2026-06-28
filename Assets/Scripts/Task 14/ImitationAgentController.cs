using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ImitationAgentController : Agent
{
    public Transform target;
    public float moveSpeed = 4f;
    public float targetDistance = 0.5f;

    private float previousDistance;

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0f, 0f, 0f);
        target.localPosition = new Vector3(4f, 2f, 0f);

        previousDistance = Vector2.Distance(transform.localPosition, target.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 agentPos = transform.localPosition;
        Vector2 targetPos = target.localPosition;

        sensor.AddObservation(agentPos.x);
        sensor.AddObservation(agentPos.y);
        sensor.AddObservation(targetPos.x);
        sensor.AddObservation(targetPos.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector3 movement = new Vector3(moveX, moveY, 0f) * moveSpeed * Time.deltaTime;
        transform.localPosition += movement;

        float currentDistance = Vector2.Distance(transform.localPosition, target.localPosition);

        if (currentDistance < previousDistance)
        {
            AddReward(0.01f);
        }
        else
        {
            AddReward(-0.01f);
        }

        previousDistance = currentDistance;

        AddReward(-0.001f);

        if (currentDistance < targetDistance)
        {
            SetReward(1f);
            EndEpisode();
        }

        if (Mathf.Abs(transform.localPosition.x) > 6f || Mathf.Abs(transform.localPosition.y) > 4f)
        {
            SetReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> actions = actionsOut.ContinuousActions;

        actions[0] = Input.GetAxisRaw("Horizontal");
        actions[1] = Input.GetAxisRaw("Vertical");
    }
}