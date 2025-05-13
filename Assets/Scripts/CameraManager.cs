using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Dictionary<KeyCode, Tuple<CinemachineCamera, TagAgent>> cameras;
    private CinemachineCamera currentCamera;
    private TagAgent spectatingAgent;
    private Dictionary<string, KeyCode> keyMapping;

    void Awake()
    {    
        keyMapping = new Dictionary<string, KeyCode>
        {
            {"0", KeyCode.Alpha1}, 
            {"1", KeyCode.Alpha2}, 
            {"2", KeyCode.Alpha3}, 
            {"3", KeyCode.Alpha4}, 
            {"4", KeyCode.Alpha5}, 
            {"5", KeyCode.Alpha6},
            {"6", KeyCode.Alpha7},
            {"Top Camera", KeyCode.Q},
            {"Front Camera", KeyCode.W},
            {"Back Camera", KeyCode.E}   
        };

        cameras = new Dictionary<KeyCode, Tuple<CinemachineCamera, TagAgent>>();

        GameObject camerasParent = GameObject.Find("Cameras");
        foreach (Transform child in camerasParent.transform)
        {
            CinemachineCamera camera = child.GetComponent<CinemachineCamera>();
            AddCamera(camera, null, child.name);
        }

        Tuple<CinemachineCamera, TagAgent> pair = cameras[keyMapping["Top Camera"]];
        currentCamera = pair.Item1;
        spectatingAgent = null;
        currentCamera.gameObject.SetActive(true);
    }

    // Set agent to null if no Agent associated with Camera
    public void AddCamera(CinemachineCamera camera, TagAgent agent, string code)
    {
        Debug.Log(cameras);
        cameras.Add(keyMapping[code], new Tuple<CinemachineCamera, TagAgent>(camera, agent));
        camera.gameObject.SetActive(false);
    }

    void Update()
    {
        foreach (var item in cameras)
        {
            if (Input.GetKeyDown(item.Key))
            {
                if (spectatingAgent)
                    spectatingAgent.ToggleSpectating();
                currentCamera.gameObject.SetActive(false);

                spectatingAgent = item.Value.Item2;
                if (spectatingAgent)    
                    spectatingAgent.ToggleSpectating();
                currentCamera = item.Value.Item1;
                currentCamera.gameObject.SetActive(true);

                break;
            }
        }
    }
}