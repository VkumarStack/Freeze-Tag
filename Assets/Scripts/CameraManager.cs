using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private List<CinemachineCamera> cameras;
    private int currentCameraIndex = 0;

    public void AddCamera(CinemachineCamera camera)
    {
        cameras.Add(camera);
        camera.gameObject.SetActive(false);
    }

    void Awake()
    {
        cameras = new List<CinemachineCamera>();        
        GameObject camerasParent = GameObject.Find("Cameras");
        Debug.Log(camerasParent);
        if (camerasParent != null)
        {
            // Get all child cameras of the "Cameras" GameObject
            foreach (Transform child in camerasParent.transform)
            {
                CinemachineCamera camera = child.GetComponent<CinemachineCamera>();
                if (camera != null)
                {
                    AddCamera(camera);
                }
            }
        }
        cameras[0].gameObject.SetActive(true);
    }

    void Update()
    {
        // Switch cameras with a key press (e.g., Tab)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Disable the current camera
            cameras[currentCameraIndex].gameObject.SetActive(false);

            // Move to the next camera
            currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

            // Enable the new camera
            cameras[currentCameraIndex].gameObject.SetActive(true);
        }
    }
}