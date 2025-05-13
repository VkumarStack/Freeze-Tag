using System;
using UnityEngine;

public class SnowballTrigger : MonoBehaviour
{
    [SerializeField] private GameObject snowballPrefab;
    [SerializeField] private int cooldownTime = 500;
    [SerializeField] private float snowballSpawnOffset;
    

    private Movement movement;
    private int lastShotTime;

    public int LastShotTime => lastShotTime;

    void Start()
    {
        movement = GetComponent<Movement>();
        lastShotTime = 0;
    }

    void FixedUpdate()
    {
        lastShotTime = Mathf.Max(0, lastShotTime - 1);
    }

    public void Reset()
    {
        lastShotTime = 0;   
    }

    public void ShootSnowball()
    {
        if (lastShotTime == 0 && !movement.Frozen)
        {
            GameObject snowball = Instantiate(snowballPrefab, transform.position + transform.forward * snowballSpawnOffset, transform.rotation, gameObject.transform);

            lastShotTime = cooldownTime;
        }
    }
}