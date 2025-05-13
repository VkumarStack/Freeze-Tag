using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.UIElements;
using Unity.Cinemachine;

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
    private int id; 
    private VisualElement agentUI;
    public bool initialFreeze;
    private CameraManager cameraManager;
    public EnvController envController;

    public int Id
    {
        get => id;
        set { id = value; }
    }

    public override void Initialize()
    {
        initialFreeze = false;
        movement = GetComponent<Movement>();
        snowballTrigger = GetComponent<SnowballTrigger>();
        envController = GetComponentInParent<EnvController>();
    }

    // TODO: This should be altered to only connect the UI and Cameras if in inference mode rather than training mode
    void Start()
    {
        UIDocument document = FindFirstObjectByType<UIDocument>();
        VisualElement root = document.rootVisualElement;
        GroupBox box;
        agentUI = new VisualElement();
        agentUI.AddToClassList("agent");
        if (role == Role.Runner)
        {
            box = root.Q<GroupBox>("Runners");
            agentUI.AddToClassList("runner");
        }
        else 
        {
            box = root.Q<GroupBox>("Taggers");
            agentUI.AddToClassList("tagger");
        }
        box.Add(agentUI);

        cameraManager = GetComponentInParent<CameraManager>();
        cameraManager.AddCamera(gameObject.GetComponentInChildren<CinemachineCamera>(), this, id.ToString());
    }

    void FixedUpdate()
    {
        if ((movement.Frozen && !agentUI.ClassListContains("frozen")) || (!movement.Frozen && agentUI.ClassListContains("frozen")))
            agentUI.ToggleInClassList("frozen");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(movement.Frozen);
        sensor.AddObservation(movement.linearVelocity);
        sensor.AddObservation(movement.OnGround);
        sensor.AddObservation(movement.OnGround && movement.JumpTimer == 0);
        if (role == Role.Tagger)
            sensor.AddObservation(movement.DashTimer == 0);
        else 
            sensor.AddObservation(snowballTrigger.LastShotTime == 0);

        sensor.AddObservation(transform.position - transform.parent.position);
        sensor.AddObservation(transform.rotation.eulerAngles.y);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (role == Role.Runner && snowballTrigger.LastShotTime != 0)
            actionMask.SetActionEnabled(1, 1, false);
        
        if (role == Role.Tagger && movement.DashTimer != 0)
            actionMask.SetActionEnabled(1, 1, false);

        if (movement.JumpTimer != 0)
            actionMask.SetActionEnabled(0, 1, false);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        float horizontal = actions.ContinuousActions[0];
        float vertical = actions.ContinuousActions[1];
        float rotate = actions.ContinuousActions[2];
        int jump = actions.DiscreteActions[0];
        // Snowball for Runners, Dash for Taggers
        int special = actions.DiscreteActions[1];

        if (role == Role.Runner)
        {
            movement.Move(horizontal, vertical, rotate, jump, 0);
            if (special == 1)
                snowballTrigger.ShootSnowball();
        }
        else
            movement.Move(horizontal, vertical, rotate, jump, special);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
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
        TagAgent otherAgent = collision.gameObject.GetComponentInChildren<TagAgent>();
        if (otherAgent != null)
        {
            if (otherAgent.CompareTag("Tagger") && this.CompareTag("Runner"))
            {
                movement.Freeze();
                envController.DistributeTagRewards(otherAgent, currAgent);
            }
            else if (otherAgent.CompareTag("Runner") && this.CompareTag("FrozenRunner") && this.movement.CanUnfreeze)
            {
                movement.Unfreeze();
                envController.DistributeThawRewards(currAgent, otherAgent);
            }
        }
    }

    // Discourage hugging walls
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.0001f);
        }
    }

    public void ToggleSpectating()
    {
        agentUI.ToggleInClassList("spectating");
    }
}
