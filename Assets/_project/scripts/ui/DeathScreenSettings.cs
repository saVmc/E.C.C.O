using TMPro;
using UnityEngine;

public sealed class DeathScreenSettings : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;

    private void Awake()
    {
        if (font != null)
            DeathScreenController.OverrideFont = font;
    }
}
