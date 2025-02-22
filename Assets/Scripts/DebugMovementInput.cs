using UnityEngine;

public class DebugMovementInput : MonoBehaviour
{
    public enum Role
    {
        Runner,
        Tagger
    }

    [SerializeField] private Role role;
    Movement movement;

    void Awake()
    {
        movement = GetComponent<Movement>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float rotate = Input.GetAxis("Mouse X");
        int jump = Input.GetKey(KeyCode.Space) ? 1 : 0;
        int dash = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;

        movement.Move(horizontal, vertical, rotate, jump, dash);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Tagger"))
        {
            if (role == Role.Runner)
            {
                movement.Freeze();
            }
        }
        if (col.gameObject.CompareTag("Runner"))
        {
            if (role == Role.Runner)
            {
                movement.Unfreeze();
            }
        }
    }
}
