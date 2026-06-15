using UnityEngine;

/// <summary>
/// World-space pickup dropped by bosses.
/// Player walks over it to receive a single boss-weighted upgrade offer.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class AbilityDropPickup : MonoBehaviour
{
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float rotateSpeed = 45f;
    [SerializeField] private Color tintColor = new Color(1f, 0.82f, 0.1f, 1f);

    private Vector3 startPos;
    private SpriteRenderer sr;

    private void Awake()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tintColor;

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
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

        if (AbilityManager.Instance == null) return;

        UpgradeOffer offer = AbilityManager.Instance.GenerateBossDropOffer();
        if (offer == null) return;

        LevelUpDisplay display = Object.FindFirstObjectByType<LevelUpDisplay>();
        display?.ShowDropOffer(offer);

        Destroy(gameObject);
    }
}
