using UnityEngine.UI;
using UnityEngine;
public class FastFowardButton : KeyboardUIButtonMapper
{
    ColorBlock buttonColors;
    ColorBlock ButtonColors
    {
        get
        {
            if (buttonColors == null)
                buttonColors = MasterButton.colors;
            return buttonColors;
        }
    }

    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color highlightColor = Color.green;

    public override void OnButtonPressed() => LevelController.instance.ToggleFastForward();
    
    protected override void Update()
    {
        base.Update();

        if (LevelController.instance.IsFastForwardOn)
            buttonColors.normalColor = highlightColor;
        else
            buttonColors.normalColor = normalColor;

        MasterButton.colors = buttonColors;
        MasterButton.interactable = LevelController.instance.LevelMode == LevelMode.Playing;
    }
}
