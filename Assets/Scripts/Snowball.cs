using UnityEngine;

public class Snowball : MonoBehaviour
{
    [SerializeField] private float snowballVelocity = 10f;
    private Rigidbody body;


    void Start()
    {
        body = GetComponent<Rigidbody>();
        body.linearVelocity = transform.forward * snowballVelocity;
    }


    void OnCollisionEnter(Collision collision)
    {
        TagAgent runner = transform.parent.gameObject.GetComponent<TagAgent>();
        if (collision.gameObject.CompareTag("Tagger"))
        {
            Movement movement = collision.gameObject.GetComponent<Movement>();
            TagAgent tagger = collision.gameObject.GetComponent<TagAgent>();
            if (movement != null && movement.CanFreeze)
            {
                movement.Freeze();
                tagger.envController.DistributeSnowballHitRewards(tagger, runner);
            }
        }
        runner.envController.DistributeSnowballMissRewards(null, runner);
        Destroy(gameObject);
    }
}
