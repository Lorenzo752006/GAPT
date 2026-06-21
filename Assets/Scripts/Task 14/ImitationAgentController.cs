using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ImitationAgentController : Agent
{
    public Transform target;
    public float moveSpeed = 5f;

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0, 1, 0);
        target.localPosition = new Vector3(4, 1, 4);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.z);
        sensor.AddObservation(target.localPosition.x);
        sensor.AddObservation(target.localPosition.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0, moveZ);
        transform.localPosition += move * moveSpeed * Time.deltaTime;

        float distance = Vector3.Distance(transform.localPosition, target.localPosition);

        if (distance < 0.7f)
        {
            SetReward(1f);
            EndEpisode();
        }
        else
        {
            AddReward(-0.001f);
        }
    }

public override void Heuristic(in ActionBuffers actionsOut)
{
    ActionSegment<float> actions = actionsOut.ContinuousActions;

    float x = Input.GetKey(KeyCode.RightArrow) ? 1 :
              Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;

    float z = Input.GetKey(KeyCode.UpArrow) ? 1 :
              Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

    Debug.Log("X: " + x + " Z: " + z);

    actions[0] = x;
    actions[1] = z;
}
}