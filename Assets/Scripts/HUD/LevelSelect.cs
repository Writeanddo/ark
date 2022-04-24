using UnityEngine;
using System.Collections.Generic;

public class LevelSelect : MonoBehaviour
{
    [SerializeField] LevelSelectButton buttonPrefab;
    List<LevelSelectButton> buttons;

    // Start is called before the first frame update
    void Start()
    {
        buttons = new List<LevelSelectButton>();
        var currentLevel = GameManager.instance.CurrentLevel;
        for (int i = 1; i <= GameManager.instance.TotalLevels; i++)
        {
            var button = Instantiate(buttonPrefab, transform);
            button.name = $"Level_{i}_Button";
            button.LevelText = $"{i}";            
            buttons.Add(button);
        }

        OnLevelLoaded(currentLevel);
    }

    public void OnLevelLoaded(int level)
    {
        // Disable the button when it is the current level
        // So that the button is highiligted to let the player know it is the current level
        // and there's no reason to re-load this level when they have a "reload" buton
        foreach (var button in buttons)
            button.IsInteractible(button.LevelNumber != level);
    }
}
