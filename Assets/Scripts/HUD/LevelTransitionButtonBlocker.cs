using UnityEngine;
using UnityEngine.UI;

public class LevelTransitionButtonBlocker : MonoBehaviour
{
    [SerializeField] Image image;
    private void Update()
    {
        // During transitions it makes this detect mouse clicks to block buttons
        image.raycastTarget = GameManager.instance.BlockButtons;
    }
}
