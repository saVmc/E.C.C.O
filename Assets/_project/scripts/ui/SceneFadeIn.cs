using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [SerializeField] private float duration = 1.2f;

    private IEnumerator Start()
    {
        if (overlay == null) yield break;

        overlay.gameObject.SetActive(true);
        Color c = Color.black;
        c.a = 1f;
        overlay.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            overlay.color = c;
            yield return null;
        }

        c.a = 0f;
        overlay.color = c;
        overlay.gameObject.SetActive(false);
    }
}
