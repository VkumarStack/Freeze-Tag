using System;
using Random = UnityEngine.Random;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 2.4f;

    [SerializeField] private float maxAcceleration = 5f;
    [SerializeField, Range(0f, 100f)] float maxAirAcceleration = 3f;
    [SerializeField, Range(0f, 2f)] float jumpHeight = 0.2f;
    [SerializeField, Range(0f, 90f)] float maxGroundAngle = 45f;
    [SerializeField] float lookSensitivity = 150f;
    Rigidbody body;
    MeshRenderer meshRenderer;
    Vector3 velocity;
    Vector3 desiredVelocity;
    float rotateDir;
    bool desiredJump;
    bool onGround;
    bool frozen;
    public bool Frozen { get { return frozen; } }
    
    float minGroundDotProduct;
    Vector3 contactNormal;

    void Awake() 
    {        
        body = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        desiredJump = false;
        frozen = false;
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    public void Move(float horizontal, float vertical, float rotate, int jump)
    {
        Vector2 playerInput;
        playerInput.x = horizontal;
        playerInput.y = vertical;
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        rotateDir = rotate;
        desiredJump |= jump == 1;

        desiredVelocity = new Vector3(playerInput.x, 0, playerInput.y) * maxSpeed;
    }

    void FixedUpdate()
    {
        if (frozen)
            return;

        velocity = body.linearVelocity;
        if (!onGround)
            contactNormal = Vector3.up;

        AdjustVelocity();
        
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
		body.linearVelocity = velocity;

        transform.Rotate(transform.up * Math.Sign(rotateDir), Math.Abs(rotateDir) * Time.fixedDeltaTime * lookSensitivity);

        onGround = false;
    }

    public void Freeze()
    {
        if (frozen)
            return;

        frozen = true;

        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
        // Change material to frozen material
        var material = meshRenderer.materials;
        material[0] = Resources.Load<Material>("Materials/Frozen");
        meshRenderer.materials = material;
        // Change Tag to Frozen
        gameObject.tag = "Frozen";
    }

    public void Unfreeze()
    {
        if (!frozen)
            return;

        frozen = false;
        body.isKinematic = false;
        var material = meshRenderer.materials;
        material[0] = Resources.Load<Material>("Materials/Runner");
        meshRenderer.materials = material;
        // Change Tag to Runner
        gameObject.tag = "Runner";
    }

    void Jump()
    {
        if (onGround)
            velocity.y += Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
    }

    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(transform.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(transform.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);    

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }   

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }
    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                onGround = true;
                contactNormal = normal;
            }
        }
    }
}
