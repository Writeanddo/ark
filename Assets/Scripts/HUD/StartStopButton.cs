using UnityEngine;
using UnityEngine.UI;

public class StartStopButton : KeyboardUIButtonMapper
{
    [SerializeField] string StartString = "Start";
    [SerializeField] Color StartButtonColor = Color.white;
    [SerializeField] Color StartButtonTextColor = Color.black;
    [SerializeField] Color StartHighlightButtonColor = Color.green;

    [SerializeField] string StopString = "Stop";
    [SerializeField] Color StopButtonColor = Color.red;
    [SerializeField] Color StopButtonTextColor = Color.white;
    [SerializeField] Color StopHighlightButtonColor = Color.red;

    [SerializeField] Button button;
    Button Button
    {
        get
        {
            if (button == null)
                button = GetComponent<Button>();
            return button;
        }
    }

    [SerializeField] Text buttonText;
    Text ButtonText
    {
        get
        {
            if (buttonText == null)
                buttonText = GetComponentInChildren<Text>();
            return buttonText;
        }
    }

    void LateUpdate()
    {
        if (Button == null)
            return;

        var colors = Button.colors;
        Button.interactable = LevelController.instance.HasAvailablePath;

        switch (LevelController.instance.LevelMode)
        {
            case LevelMode.Edit:
            case LevelMode.Drawing:
                ButtonText.text = StartString;
                ButtonText.color = StartButtonTextColor;
                colors.normalColor = StartButtonColor;
                colors.highlightedColor = StartHighlightButtonColor;
                break;

            case LevelMode.Playing:
                ButtonText.text = StopString;
                ButtonText.color = StopButtonTextColor;
                colors.normalColor = StopButtonColor;
                colors.highlightedColor = StopHighlightButtonColor;
                break;
        }

        Button.colors = colors;
    }

    public override void OnButtonPressed()
    {
        switch (LevelController.instance.LevelMode)
        {
            case LevelMode.Edit:
            case LevelMode.Drawing:
                LevelController.instance.EnterPlayMode();
                break;

            case LevelMode.Playing:
                LevelController.instance.EnterEditMode();
                break;
        }
    }
}
