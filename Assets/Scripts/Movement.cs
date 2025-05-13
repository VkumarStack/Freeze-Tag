using System;
using UnityEngine;
using UnityEngine.UIElements;

// Movement controller incorporated from Catlike Coding tutorial:
// https://catlikecoding.com/unity/tutorials/movement/
public class Movement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 2.4f;
    [SerializeField] private float maxAcceleration = 5f;
    [SerializeField, Range(0f, 100f)] float maxAirAcceleration = 3f;
    [SerializeField, Range(0f, 2f)] float jumpHeight = 0.2f;
    [SerializeField, Range(0f, 90f)] float maxGroundAngle = 45f;
    [SerializeField] int jumpCoolDown = 50;
    [SerializeField] int dashCooldown = 100;
    [SerializeField] float lookSensitivity = 150f;
    // Number of time steps required for the agent to get frozen again when they just become unfrozen
    // Goal here is to prevent a Tagger agent from getting hit by a snowball right as they just unthaw
    [SerializeField] int freezeGracePeriod = 100;
    // Number of time steps required for the agent to get unfrozen again when they just became unfrozen earlier
    // Goal here is to prevent a Runner agent from indefinitely saving their teammate by always colliding with them
    [SerializeField] int unfreezeGracePeriod = 100;
    // Set to true for Taggers (thaw from the snowball naturally)
    [SerializeField] bool unfreezesNaturally;
    // Time steps for natural thawing
    [SerializeField] int naturalThawPeriod = 1000;
    [SerializeField] Color frozenColor = new Color(71, 230, 255);

    Rigidbody body;
    Vector3 velocity;
    Vector3 desiredVelocity;
    float minGroundDotProduct;
    Vector3 contactNormal;
    float rotateDir;
    bool desiredJump;
    bool desiredDash;
    bool onGround;
    bool frozen;
    int freezeGraceTimer;
    int unfreezeGraceTimer;
    int jumpTimer;
    int dashTimer;
    int naturalThawTimer;
    Color originalColor;
    string originalTag;
    MeshRenderer meshRenderer;

    public bool CanUnfreeze { get { return frozen && unfreezeGraceTimer == 0; } }
    public bool CanFreeze { get { return !frozen && freezeGraceTimer == 0; } }
    public bool Frozen => frozen;
    public int JumpTimer => jumpTimer;
    public int DashTimer => dashTimer;
    public bool OnGround => onGround;
    public Vector3 linearVelocity => body.linearVelocity;

    // public VisualElement uiElement;


    void Awake() 
    {        
        body = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        originalColor = meshRenderer.material.color;
        originalTag = gameObject.tag;

        Reset();
    }

    public void Reset()
    {
        Unfreeze();
        freezeGraceTimer = 0;
        unfreezeGraceTimer = 0;
        jumpTimer = 0;
        naturalThawTimer = 0;
        dashTimer = 0;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        desiredJump = false;
        desiredVelocity = Vector3.zero;
        rotateDir = 0;
    }

    public void Move(float horizontal, float vertical, float rotate, int jump, int dash)
    {
        Vector2 playerInput;
        playerInput.x = horizontal;
        playerInput.y = vertical;
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        rotateDir = rotate;
        desiredJump |= jump == 1;
        desiredDash |= dash == 1;

        desiredVelocity = new Vector3(playerInput.x, 0, playerInput.y) * maxSpeed;
    }

    void FixedUpdate()
    {
        unfreezeGraceTimer = Mathf.Max(0, unfreezeGraceTimer - 1);

        if (frozen)
        {
            body.linearVelocity = new Vector3(0, body.linearVelocity.y, 0);
            
            if (unfreezesNaturally)
            {
                naturalThawTimer = Mathf.Max(0, naturalThawTimer - 1);
                if (naturalThawTimer == 0)
                    Unfreeze();
            }
            return;
        }

        velocity = body.linearVelocity;
        if (!onGround)
            contactNormal = Vector3.up;

        AdjustVelocity();
        
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        if (desiredDash)
        {
            desiredDash = false;
            Dash();
        }

		body.linearVelocity = velocity;

        transform.Rotate(transform.up * Math.Sign(rotateDir), Math.Abs(rotateDir) * Time.fixedDeltaTime * lookSensitivity);

        onGround = false;
        freezeGraceTimer = Mathf.Max(0, freezeGraceTimer - 1);
        jumpTimer = Mathf.Max(0, jumpTimer - 1);
        dashTimer = Mathf.Max(0, dashTimer - 1);
    }

    public void Freeze()
    {
        if (frozen || freezeGraceTimer > 0)
            return;

        frozen = true;
        naturalThawTimer = naturalThawPeriod;

        body.linearVelocity = Vector3.zero;
        meshRenderer.material.color = frozenColor; 
        gameObject.tag = "Frozen" + originalTag;
    }

    public void Unfreeze()
    {
        if (!frozen || unfreezeGraceTimer > 0)
            return;

        frozen = false;
        unfreezeGraceTimer = unfreezeGracePeriod;
        freezeGraceTimer = freezeGracePeriod;
        meshRenderer.material.color = originalColor;
        gameObject.tag = originalTag;
    }

    void Jump()
    {
        if (onGround && jumpTimer == 0)
        {
            velocity.y += Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        }
    }

    void Dash()
    {
        if (dashTimer == 0)
        {
            velocity += transform.forward * maxSpeed * 2;
            dashTimer = dashCooldown;
        }
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
        if (onGround)
            jumpTimer = jumpCoolDown;
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag("Runner") || collision.gameObject.CompareTag("Tagger") || collision.gameObject.CompareTag("FrozenRunner") || collision.gameObject.CompareTag("FrozenTagger"))
            return;
            
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
