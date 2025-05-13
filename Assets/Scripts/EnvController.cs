using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Unity.Cinemachine;
using UnityEngine.UIElements;

public class EnvController : MonoBehaviour
{
    public class AgentInfo
    {
        public TagAgent Agent;
        public GameObject gameObject;
    }

    // [SerializeField] private CameraManager cameraManager;
    [SerializeField] private int MaxEnvironmentSteps = 5000;
    [SerializeField] private int numRunners = 5;
    [SerializeField] private int numTaggers = 2;

    // During Training, the probability that the FIRST Runner will be frozen
    // Encourages learning unfreezing behavior without necessarily relying on 
    // Taggers causing a freeze during early learning
    [SerializeField] private float initiallyFrozenProbability = 0.2f;
    // During Training, the probability of the obstacle being hidden
    // Encourages randomized obstacles so Agents do not rely on their existence
    // during learning
    [SerializeField] private float obstacleProbability = 0.3f;

    // Reward variables
    [SerializeField] private float r_survival = 1f;
    [SerializeField] private float r_frozen = -0.25f;
    [SerializeField] private float r_unfreeze = 0.15f;
    [SerializeField] private float r_unfreeze_bonus = 0.1f; // Bonus for unfreezing a teammate if there are only a few runners left
    [SerializeField] private float r_existential = 0.5f;
    [SerializeField] private float distance_penalty_threshold = 0.35f;
    [SerializeField] private float distance_reward_factor = 5;

    [SerializeField] private float r_snowball = 0.175f;
    [SerializeField] private float r_snowball_miss = -0.075f;
    [SerializeField] private float rg_snowball = 0.1f;
    [SerializeField] private float rg_freeze = -0.2f;
    [SerializeField] private float rg_unfreeze_factor = 0.25f;
    [SerializeField] private float rg_win_base = 1f;
    [SerializeField] private float rg_win_extra_factor = 0.2f;
    [SerializeField] private float rg_existential = 0.1f;

    [SerializeField] private float t_catch = 0.1f;
    [SerializeField] private float t_win = 1f;
    [SerializeField] private float t_existential = -0.5f;
    [SerializeField] private float t_snowball = -0.1f;
    [SerializeField] private float tg_catch = 0.1f;
    [SerializeField] private float tg_win = 1f;
    [SerializeField] private float tg_existential = -0.5f;
    [SerializeField] private float tg_snowball = -0.1f;

    private SimpleMultiAgentGroup runners;
    private SimpleMultiAgentGroup taggers;
    private List<AgentInfo> runnerAgents;
    private List<AgentInfo> taggerAgents;

    private float scale;
    private int resetTimer;

    private List<Vector3> spawnPoints;
    private Label timerLabel;


    void Awake()
    {
        scale = GameObject.Find("Ground").transform.localScale.x;
        spawnPoints = new List<Vector3>() { new Vector3(-4f * scale, 0.1f, 4f * scale),  new Vector3(-4f * scale, 0.1f, 0f), new Vector3(-4f * scale, 0.1f, -4f * scale), 
                                            new Vector3(-2f * scale, 0.1f, -2f * scale), new Vector3(-2f * scale, 0.1f, 0f), new Vector3(-2f * scale, 0.1f, 2.6f * scale),
                                            new Vector3(-0.6f * scale, 0.1f, -2.6f * scale), new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.1f, 2.6f * scale),
                                            new Vector3(2f * scale, 0.1f, -4f * scale), new Vector3(2f * scale, 0.1f, 0f), new Vector3(3.2f * scale, 0.1f, 3.8f * scale),
                                            new Vector3(4f * scale, 0.1f, -0.8f * scale), new Vector3(4f * scale, 0.1f, 4f * scale), new Vector3(4f * scale, 0.1f, -4f * scale)
                                        };

        runners = new SimpleMultiAgentGroup();
        taggers = new SimpleMultiAgentGroup();
        runnerAgents = new List<AgentInfo>();
        taggerAgents = new List<AgentInfo>();

        UIDocument uiDocument = FindFirstObjectByType<UIDocument>();
        var root = uiDocument.rootVisualElement;
        timerLabel = root.Q<Label>("TimerLabel");

        for (int i = 0; i < numRunners; i++)
        {
            GameObject runner = Instantiate(Resources.Load("Prefabs/Runner"), this.transform) as GameObject;
            runner.SetActive(false); // Do not set active until their Spawn Points are first adequately set

            runnerAgents.Add(new AgentInfo { Agent = runner.GetComponent<TagAgent>(), gameObject = runner });
            runners.RegisterAgent(runner.GetComponent<TagAgent>());

            runner.GetComponent<TagAgent>().Id = i;
        }


        for (int i = 0; i < numTaggers; i++)
        {
            GameObject tagger = Instantiate(Resources.Load("Prefabs/Tagger"), this.transform) as GameObject;
            tagger.SetActive(false); // Do not set active until their Spawn Points are first adequately set

            taggerAgents.Add(new AgentInfo { Agent = tagger.GetComponent<TagAgent>(), gameObject = tagger });
            taggers.RegisterAgent(tagger.GetComponent<TagAgent>());

            tagger.GetComponent<TagAgent>().Id = numRunners + i;

            /*
            if (cameraManager != null)
                cameraManager.AddCamera(tagger.GetComponentInChildren<CinemachineCamera>());
            */
        }

        ResetScene();
    }

    void ResetScene()
    {
        resetTimer = 0;
        List<Vector3> sampledSpawnPoints = SampleSpawnPoints(numRunners + numTaggers);

        bool initiallyFrozen = Random.value < initiallyFrozenProbability;

        for (int i = 0; i <  runnerAgents.Count; i++)
        {
            Agent runner = runnerAgents[i].Agent;
            Vector3 runnerPosition = sampledSpawnPoints[i];
            runnerAgents[i].gameObject.transform.SetPositionAndRotation(transform.TransformPoint(runnerPosition), Quaternion.Euler(0, Random.Range(-180, 180), 0));
            runnerAgents[i].gameObject.SetActive(true);
        
            runner.GetComponent<TagAgent>().initialFreeze = initiallyFrozen && i == 0; 
        }

        for (int i = 0; i < taggerAgents.Count; i++)
        {
            Vector3 taggerPosition = sampledSpawnPoints[numRunners + i];

            taggerAgents[i].gameObject.transform.SetPositionAndRotation(transform.TransformPoint(taggerPosition), Quaternion.Euler(0, Random.Range(-180, 180), 0));
            taggerAgents[i].gameObject.SetActive(true);
        }

        HideObstacles(obstacleProbability);
    }

    void HideObstacles(float prob)
    {
        Transform obstaclesParent = transform.Find("Obstacles");
        if (obstaclesParent != null)
        {
            foreach (Transform obstacle in obstaclesParent)
            {
                if (Random.value < prob)
                    obstacle.gameObject.SetActive(false);
                else
                    obstacle.gameObject.SetActive(true);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        resetTimer += 1;
        timerLabel.text = "Time Left: " + (int) (MaxEnvironmentSteps * 0.02 - resetTimer * 0.02) + " seconds";
        if (resetTimer > MaxEnvironmentSteps)
        {
            ResetScene();

            DistributeTimeoutRewards();
            
            runners.EndGroupEpisode();
            taggers.EndGroupEpisode();
        }
        else
        {
            bool allRunnersCaught = true;
            foreach (AgentInfo info in runnerAgents)
            {
                if (!info.Agent.isFrozen())
                {
                    allRunnersCaught = false;
                    break;
                }
            }

            if (allRunnersCaught)
            {
                DistributeWinRewards();
                
                ResetScene();

                runners.EndGroupEpisode();
                taggers.EndGroupEpisode();
            }
            else
            {
                DistributeExistentialRewards();
                DistributeProximityRewards();
            }
        }
    }

    public void DistributeTagRewards(TagAgent tagger, TagAgent runner)
    {
        tagger.AddReward(t_catch);
        taggers.AddGroupReward(tg_catch);

        runner.AddReward(r_frozen);
        runners.AddGroupReward(rg_freeze);

    }

    public void DistributeSnowballHitRewards(TagAgent tagger, TagAgent runner)
    {
        tagger.AddReward(t_snowball);
        taggers.AddGroupReward(tg_snowball);
        
        runner.AddReward(r_snowball);
        runners.AddGroupReward(rg_snowball);
    }

    public void DistributeSnowballMissRewards(TagAgent tagger, TagAgent runner)
    {
        runner.AddReward(r_snowball_miss);
    }
    
    public void DistributeThawRewards(TagAgent frozen, TagAgent runner)
    {
        int numAlive = 0;
        foreach (AgentInfo info in runnerAgents)
        {
            if (!info.Agent.isFrozen())
                numAlive++;
        }

        runner.AddReward(r_unfreeze + r_unfreeze_bonus * (1 - ((float) numAlive / numRunners)));
        runners.AddGroupReward(rg_unfreeze_factor);
    }

    public void DistributeTimeoutRewards()
    {
        int numAlive = 0;
        foreach (AgentInfo info in runnerAgents)
        {
            if (!info.Agent.isFrozen())
            {
                numAlive++;
                info.Agent.AddReward(r_survival);
            }
        }
        runners.AddGroupReward(rg_win_base + rg_win_extra_factor * numAlive);
    }

    public void DistributeWinRewards()
    {
        foreach (AgentInfo info in taggerAgents)
        {
            info.Agent.AddReward(t_win);
        }
        taggers.AddGroupReward(tg_win);
    }

    public void DistributeExistentialRewards()
    {
        int numAlive = 0;
        foreach (AgentInfo info in runnerAgents)
        {
            if (!info.Agent.isFrozen())
            {
                numAlive++;
                info.Agent.AddReward(r_existential / MaxEnvironmentSteps);
            }
        }
        runners.AddGroupReward(rg_existential * numAlive / MaxEnvironmentSteps);

        foreach (AgentInfo info in taggerAgents)
        {
            info.Agent.AddReward(t_existential / MaxEnvironmentSteps);
        }
        taggers.AddGroupReward(tg_existential / MaxEnvironmentSteps);
    }


    public void DistributeProximityRewards()
    {
        foreach (AgentInfo runner in runnerAgents)
        {
            if (!runner.Agent.isFrozen())
            {
                TagAgent closest;
                float closestDist = float.PositiveInfinity; 
                foreach (AgentInfo tagger in taggerAgents)
                {
                    float dist = Vector3.Distance(runner.gameObject.transform.position, tagger.gameObject.transform.position);
                    if (dist < closestDist)
                    {
                        closest = tagger.Agent;
                        closestDist = dist;
                    }
                }

                closestDist = Mathf.Clamp(closestDist / scale / 20.0f, 0f, 1f);
                float dist_reward = (-Mathf.Pow(distance_reward_factor, -(closestDist / distance_penalty_threshold)) + (1 / distance_reward_factor)) * (-1 / (-1 + 1 / distance_reward_factor));
                runner.Agent.AddReward(dist_reward / MaxEnvironmentSteps);
            }
        }
    }

    List<Vector3> SampleSpawnPoints(int count)
    {
        List<Vector3> sampledPoints = new List<Vector3>();
        List<Vector3> availablePoints = new List<Vector3>(spawnPoints);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, availablePoints.Count);
            sampledPoints.Add(availablePoints[index]);
            availablePoints.RemoveAt(index);
        }

        return sampledPoints;
    }
}
