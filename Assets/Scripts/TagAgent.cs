using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class TagAgent : Agent
{
    public enum Role
    {
        Runner,
        Tagger
    }
    [SerializeField] private Role role;
    private Movement movement;
    private SnowballTrigger snowballTrigger;
    public bool initialFreeze;
    public EnvController envController;

    public override void Initialize()
    {
        initialFreeze = false;
        movement = GetComponent<Movement>();
        snowballTrigger = GetComponent<SnowballTrigger>();
        envController = GetComponentInParent<EnvController>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(movement.Frozen);
        sensor.AddObservation(movement.body.linearVelocity);
        sensor.AddObservation(movement.onGround);
        sensor.AddObservation(movement.onGround && movement.jumpTimer == 0);
        if (role == Role.Tagger)
            sensor.AddObservation(movement.dashTimer == 0);
        else 
            sensor.AddObservation(snowballTrigger.lastShotTime == 0);

        sensor.AddObservation(transform.position - transform.parent.position);
        sensor.AddObservation(transform.rotation.eulerAngles.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        float horizontal = actions.ContinuousActions[0];
        float vertical = actions.ContinuousActions[1];
        float rotate = actions.ContinuousActions[2];
        int jump = actions.DiscreteActions[0];
        int dash = actions.DiscreteActions[1];

        if (role == Role.Runner)
        {
            movement.Move(horizontal, vertical, rotate, jump, 0);
            if (dash == 1)
                snowballTrigger.ShootSnowball();
        }
        else
            movement.Move(horizontal, vertical, rotate, jump, dash);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Cursor.lockState = CursorLockMode.Locked;
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        discreteActionsOut[1] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Mouse X");
    }

    public override void OnEpisodeBegin()
    {
        movement.Reset();
        if (role == Role.Runner)
            snowballTrigger.Reset();
        if (initialFreeze)
            movement.Freeze();

        initialFreeze = false;
    }

    public bool isFrozen()
    {
        return movement.Frozen;
    }

    void OnCollisionEnter(Collision collision)
    {
        TagAgent currAgent = gameObject.GetComponentInChildren<TagAgent>();
        TagAgent tagAgent = collision.gameObject.GetComponentInChildren<TagAgent>();
        if (tagAgent != null)
        {
            if (tagAgent.CompareTag("Tagger") && this.CompareTag("Runner"))
            {
                movement.Freeze();
                envController.DistributeTagRewards(tagAgent, currAgent);
            }
            else if (tagAgent.CompareTag("Runner") && this.CompareTag("FrozenRunner") && this.movement.CanUnfreeze)
            {
                movement.Unfreeze();
                envController.DistributeThawRewards(currAgent, tagAgent);
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.0001f);
        }
    }
}
