using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class EnvController : MonoBehaviour
{
    public class AgentInfo
    {
        public TagAgent Agent;
        public GameObject gameObject;
    }

    [SerializeField] public int MaxEnvironmentSteps = 5000;
    [SerializeField] private int numRunners = 5;
    [SerializeField] private int numTaggers = 2;

    [SerializeField] private float initiallyFrozenProbability = 0.2f;
    [SerializeField] private float obstacleProbability = 0.3f;

    [SerializeField] private float r_survival = 1f;
    [SerializeField] private float r_frozen = -0.25f;
    [SerializeField] private float r_unfreeze = 0.15f;
    [SerializeField] private float r_existential = 0.5f;
    [SerializeField] private float r_snowball = 0.125f;
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
    public int resetTimer;

    private List<Vector3> spawnPoints;

    void Awake()
    {
        scale = GameObject.Find("Ground").transform.localScale.x;
        spawnPoints = new List<Vector3>() { new Vector3(-4f * scale, 0.1f, 4f * scale),  new Vector3(-4f * scale, 0.1f, 0f), new Vector3(-4f * scale, 0.1f, -4f * scale), 
                                            new Vector3(-2f * scale, 0.1f, -2f * scale), new Vector3(-2f * scale, 0.1f, 0f), new Vector3(-2f * scale, 0.1f, 2.6f * scale),
                                            new Vector3(-0.6f * scale, 0.1f, -2.6f * scale), new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0.1f, 2.6f * scale),
                                            new Vector3(2f * scale, 0.1f, -4f * scale), new Vector3(2f * scale, 0.1f, 0f), new Vector3(3.2f * scale, 0.1f, 3.8f * scale),
                                            new Vector3(4f * scale, 0.1f, -0.8f * scale), new Vector3(4f * scale, 0.1f, 4f * scale), new Vector3(4f * scale, 0.1f, -4f * scale)};

        runners = new SimpleMultiAgentGroup();
        taggers = new SimpleMultiAgentGroup();
        runnerAgents = new List<AgentInfo>();
        taggerAgents = new List<AgentInfo>();

        bool initiallyFrozen = Random.value < initiallyFrozenProbability;

        List<Vector3> sampledSpawnPoints = SampleSpawnPoints(numRunners + numTaggers);

        for (int i = 0; i < numRunners; i++)
        {
            Vector3 runnerPosition = sampledSpawnPoints[i];

            GameObject runner = Instantiate(Resources.Load("Prefabs/Runner"), transform.TransformPoint(runnerPosition), Quaternion.Euler(0, Random.Range(-180, 180), 0), this.transform) as GameObject;
            runnerAgents.Add(new AgentInfo { Agent = runner.GetComponent<TagAgent>(), gameObject = runner });
            runners.RegisterAgent(runner.GetComponent<TagAgent>());

            runner.GetComponent<TagAgent>().initialFreeze = initiallyFrozen && i == 0; 
        }


        for (int i = 0; i < numTaggers; i++)
        {
            Vector3 taggerPosition = sampledSpawnPoints[numRunners + i];

            GameObject tagger = Instantiate(Resources.Load("Prefabs/Tagger"), transform.TransformPoint(taggerPosition), Quaternion.Euler(0, Random.Range(-180, 180), 0), this.transform) as GameObject;
            taggerAgents.Add(new AgentInfo { Agent = tagger.GetComponent<TagAgent>(), gameObject = tagger });
            taggers.RegisterAgent(tagger.GetComponent<TagAgent>());
        }

        HideObstacles(obstacleProbability);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        resetTimer += 1;
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
    
    public void DistributeThawRewards(TagAgent frozen, TagAgent runner)
    {
        runner.AddReward(r_unfreeze);
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

    public void Interrupt()
    {
        ResetScene();
        
        runners.GroupEpisodeInterrupted();
        taggers.GroupEpisodeInterrupted();
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
        
            runner.GetComponent<TagAgent>().initialFreeze = initiallyFrozen && i == 0; 
        }

        for (int i = 0; i < taggerAgents.Count; i++)
        {
            Vector3 taggerPosition = sampledSpawnPoints[numRunners + i];

            taggerAgents[i].gameObject.transform.SetPositionAndRotation(transform.TransformPoint(taggerPosition), Quaternion.Euler(0, Random.Range(-180, 180), 0));
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
