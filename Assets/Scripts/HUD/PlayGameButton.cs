using UnityEngine;

public class PlayGameButton : KeyboardUIButtonMapper
{
    public override void OnButtonPressed()
    {
        GameManager.instance.PlayGame();
    }
}
