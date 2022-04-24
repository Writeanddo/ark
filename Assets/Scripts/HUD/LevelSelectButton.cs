using UnityEngine;
using UnityEngine.UI;

public class LevelSelectButton : KeyboardUIButtonMapper
{
    [SerializeField] Button button;
    public Button Button { get { return button; } }

    [SerializeField] Text levelText;
    public string LevelText { set { levelText.text = value; } get { return levelText.text; } }

    public int LevelNumber
    {
        get
        {
            return int.TryParse(LevelText, out int level) ? level : 0;
        }
    }

    public override void OnButtonPressed() => GameManager.instance.LoadLevel(LevelNumber);
    public void IsInteractible(bool interactible) => Button.interactable = interactible;
}
