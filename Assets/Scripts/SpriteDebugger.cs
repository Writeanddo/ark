using UnityEngine;

[ExecuteInEditMode]
public class SpriteDebugger : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (!Application.isPlaying)
            spriteRenderer.gameObject.SetActive(true);
        else
            spriteRenderer.gameObject.SetActive(false);
    }
}
