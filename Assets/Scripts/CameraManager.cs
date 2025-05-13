using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Also manages User playing
public class CameraManager : MonoBehaviour
{
    // Third Person Camera, Tag Agent (Optional), First Person Camera for Agent (Optional)
    private Dictionary<KeyCode, Tuple<CinemachineCamera, TagAgent, CinemachineCamera>> cameras;
    private Tuple<CinemachineCamera, TagAgent, CinemachineCamera> current;
    private Dictionary<string, KeyCode> keyMapping;
    private bool playing;

    void Awake()
    {    
        playing = false;
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

        cameras = new Dictionary<KeyCode, Tuple<CinemachineCamera, TagAgent, CinemachineCamera>>();

        GameObject camerasParent = GameObject.Find("Cameras");
        foreach (Transform child in camerasParent.transform)
        {
            CinemachineCamera camera = child.GetComponent<CinemachineCamera>();
            AddCamera(camera, null, null, child.name);
        }

        current = cameras[keyMapping["Top Camera"]];
        current.Item1.gameObject.SetActive(true);
    }

    // Set agent to null if no Agent associated with Camera
    public void AddCamera(CinemachineCamera camera, TagAgent agent, CinemachineCamera firstPersonCamera, string code)
    {
        cameras.Add(keyMapping[code], new Tuple<CinemachineCamera, TagAgent, CinemachineCamera>(camera, agent, firstPersonCamera));
        camera.gameObject.SetActive(false);
        if (firstPersonCamera != null)
            firstPersonCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && current.Item2 != null)
            TogglePlay();


        if (!playing)
        {
            foreach (var item in cameras)
            {
                if (Input.GetKeyDown(item.Key))
                {
                    if (current.Item2 != null)
                        current.Item2.ToggleSpectating();
                    current.Item1.gameObject.SetActive(false);

                    current = item.Value;
                    if (current.Item2 != null)
                        current.Item2.ToggleSpectating();
                    current.Item1.gameObject.SetActive(true);

                    break;
                }
            }
        }
    }

    void TogglePlay()
    {
        if (!playing)
        {
            current.Item1.gameObject.SetActive(false);
            current.Item3.gameObject.SetActive(true);
        }
        else
        {
            current.Item3.gameObject.SetActive(false);
            current.Item1.gameObject.SetActive(true);
        }
        playing = !playing;
        current.Item2.ToggleHeuristic();
    }
}