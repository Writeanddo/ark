using UnityEngine;

public class ResetLevelButton : KeyboardUIButtonMapper
{
    public override void OnButtonPressed() => LevelController.instance.OnResetButtonPressed();

    protected override void Update()
    {
        base.Update();

        MasterButton.interactable = LevelController.instance.LevelMode != LevelMode.Playing;
    }

}
