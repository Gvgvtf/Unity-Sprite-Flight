using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{

    public float minSize = 0.5f;
    public float maxSize = 2.0f;
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float maxSpinSpeed = 10f;
    public float collisionBoostMultiplier = 0.3f; // Tune this in the Inspector
    public GameObject obstacleCollissionEffect;

    Rigidbody2D rb;

    void Start()
    {
        float randomSize = Random.Range(minSize, maxSize);
        transform.localScale = new Vector3(randomSize, randomSize, 1);
        rb = GetComponent<Rigidbody2D>();
        float randomSpeed = Random.Range(minSpeed, maxSpeed) / randomSize;
        Vector2 randomDirection = Random.insideUnitCircle;
        rb.AddForce(randomDirection * randomSpeed);
        float randomTorque = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomTorque);
    }

    void Update() { }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;

        // Boost this obstacle's velocity along the contact normal regardless
        // of which object wins the spawn check below
        Vector2 contactNormal = collision.GetContact(0).normal;
        float boostForce = rb.linearVelocity.magnitude * collisionBoostMultiplier;
        rb.AddForce(contactNormal * boostForce, ForceMode2D.Impulse);

        if (otherRb != null && collision.gameObject.GetInstanceID() > gameObject.GetInstanceID())
            return;

        Vector2 contactPoint = collision.GetContact(0).point;
        GameObject bounceEffect = Instantiate(obstacleCollissionEffect, contactPoint, Quaternion.identity);

        ParticleSystem ps = bounceEffect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float impactStrength = collision.relativeVelocity.magnitude;
            // 0.75 = 75% minimum, 1.5 = 150% maximum of the prefab's burst count
            float scale = Mathf.Clamp(impactStrength / maxSpeed, 0.75f, 1.5f);

            var main = ps.main;
            main.startSizeMultiplier *= scale;
            main.startSpeedMultiplier *= scale;

            var emission = ps.emission;
            if (emission.burstCount > 0)
            {
                var bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(bursts);
                for (int i = 0; i < bursts.Length; i++)
                    bursts[i].count = new ParticleSystem.MinMaxCurve(bursts[i].count.constant * scale);
                emission.SetBursts(bursts);
            }

            ps.Play();
        }

        Destroy(bounceEffect, 1f);
    }
}