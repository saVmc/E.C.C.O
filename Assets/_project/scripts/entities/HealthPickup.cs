using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healPercent = 0.2f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float rotateSpeed = 60f;
    [SerializeField] private Color tintColor = new Color(1f, 0.2f, 0.2f, 1f);

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tintColor;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return;

        if (PlayerHealth.Instance != null)
        {
            int amount = Mathf.CeilToInt(PlayerHealth.Instance.MaxHealth * healPercent);
            PlayerHealth.Instance.Heal(amount);
        }

        Destroy(gameObject);
    }
}
