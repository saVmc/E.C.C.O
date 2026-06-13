using UnityEngine;

public class ExpOrb : MonoBehaviour
{
    [SerializeField] private ExpOrbProfile profile;

    private Transform player;
    private bool isAttracting = false;
    private int expValueOverride = -1;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (profile != null)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = profile.OrbColor;
            transform.localScale = Vector3.one * profile.OrbScale;
        }
    }

    private void Update()
    {
        if (player == null || profile == null)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= profile.AttractRadius)
            isAttracting = true;

        if (isAttracting)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                profile.FlySpeed * Time.deltaTime
            );

            if (dist < 0.15f)
                Collect();
        }
    }

    private void Collect()
{
    if (PlayerProgression.Instance != null)
    {
        int expToAdd = expValueOverride >= 0 ? expValueOverride : profile.ExpValue;
        PlayerProgression.Instance.AddExp(expToAdd);
    }

    Destroy(gameObject);
}

    public void SetProfile(ExpOrbProfile newProfile)
    {
        profile = newProfile;
    }
    public void SetExpValue(int value)
{
    expValueOverride = value;
}
}