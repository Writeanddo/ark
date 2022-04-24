using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] string levelNamePrefix = "Level_";
    public bool GameOver { get; set; }
    public bool GamePaused { get; set; }
    public bool IsTransitioning { get; set; }
    public bool NotShowingMessage { get; set; } = true;
    public bool BlockButtons { get { return IsTransitioning && NotShowingMessage; } }

    int totalLevels;
    public int TotalLevels
    {
        get
        {
            if(totalLevels < 1)
            {
                for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string sceneName = $"{levelNamePrefix}{i}";
                    if (Application.CanStreamedLevelBeLoaded(sceneName))
                        totalLevels++;
                }
            }
            return totalLevels;
        }
    }

    int currentLevel;
    public int CurrentLevel
    {
        get
        {
            if (currentLevel == 0)
            {
                string sceneNumber = Regex.Match(CurrentSceneName, @"\d+").Value;
                if (!string.IsNullOrEmpty(sceneNumber))
                    currentLevel = int.Parse(sceneNumber);
            }
            return currentLevel;
        }

        set { currentLevel = value; }
    }

    string CurrentSceneName
    {
        get
        {
            return SceneManager.GetActiveScene().name;
        }
    }

    TextBox textBox;
    public TextBox TextBox
    {
        get
        {
            if (textBox == null)
                textBox = FindObjectOfType<TextBox>();
            return textBox;
        }
    }

    [SerializeField, Tooltip("Intro text")]
    MessagesText introMessage;
    public List<string> IntroMessage { get { return introMessage != null ? introMessage.messages.ToList() : null; } }

    [SerializeField, Tooltip("Game completed message")]
    MessagesText outroMessage;
    public List<string> OutroMessage { get { return outroMessage != null ? outroMessage.messages.ToList() : null; } }

    LevelSelect levelSelect;
    public LevelSelect LevelSelect
    {
        get
        {
            if (levelSelect == null)
                levelSelect = FindObjectOfType<LevelSelect>();
            return levelSelect;
        }
    }

    void Start()
    {
        if (gameObject == null)
            return;

        AudioManager.instance.PlayMusic(MusicLibrary.instance.gameMusic);
    }

    private void Update()
    {
        if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public void PlayGame()
    {
        LoadLevel(1);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        var buildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        LoadLevel(buildIndex);        
    }

    public void LoadLevel(int buildIndex)
    {
        LevelController.instance.OnLevelChanged();

        // There are no more levels so play outro and load main menu
        if (!Application.CanStreamedLevelBeLoaded(buildIndex))
            StartCoroutine(TransitionToOutroScene(0));
        else
        {
            currentLevel = buildIndex;
            
            if(buildIndex == 1)
                StartCoroutine(TransitionToIntroScene(buildIndex));
            else
                StartCoroutine(TransitionToScene(buildIndex));
        }
    }

    IEnumerator TransitionToIntroScene(int buildIndex)
    {
        yield return StartCoroutine(TransitionToSceneWithMessage(buildIndex, IntroMessage));
    }

    IEnumerator TransitionToOutroScene(int buildIndex)
    {
        yield return StartCoroutine(TransitionToSceneWithMessage(buildIndex, OutroMessage));
    }

    void HideUIForMessageTransition()
    {
        MenuController.instance.ShowSettingsMenu(true);
        MenuController.instance.ShowControlsMenu(true);

        MenuController.instance.ShowMainMenuButton(false);
        MenuController.instance.ShowLevelButtons(false);
        LevelLayout.instance.EnableGameTile(false);
    }

    void HudUIForLevelTransition()
    {
        MenuController.instance.ShowMainMenuButton(false);
        MenuController.instance.ShowLevelButtons(false);
        LevelLayout.instance.EnableGameTile(false);

        MenuController.instance.ShowControlsMenu(true);
        MenuController.instance.ShowSettingsMenu(true);
    }

    void OnLevelLoaded(int level)
    {
        // Main Menu
        if(level < 1)
        {
            MenuController.instance.ShowMainMenuButton(true);
            MenuController.instance.ShowControlsMenu(true);            
            MenuController.instance.ShowSettingsMenu(true);
            LevelLayout.instance.EnableGameTile(true);

            MenuController.instance.ShowLevelButtons(false);
        }
        else
        {
            MenuController.instance.ShowControlsMenu(true);
            MenuController.instance.ShowSettingsMenu(true);
            MenuController.instance.ShowLevelButtons(true);

            MenuController.instance.ShowMainMenuButton(false);
            LevelLayout.instance.EnableGameTile(false);
            LevelSelect.OnLevelLoaded(level);
        }
    }

    IEnumerator TransitionToSceneWithMessage(int buildIndex, List<string> message)
    {
        IsTransitioning = true;
        var src = AudioManager.instance.PlayClip(SFXLibrary.instance.waveClip);
        var time = src.clip.length * 0.5f;

        // Hide what we don't need
        HideUIForMessageTransition();

        // Raise the water and slide out the bushes
        StartCoroutine(MenuController.instance.SlideOutRoutine(time));
        yield return StartCoroutine(WaterTransition.instance.RiseRoutine(time));

        // Show the scripture
        // This already waits for the box to be closed
        NotShowingMessage = false;
        yield return StartCoroutine(TextBox.ShowMessageRoutine(message));
        NotShowingMessage = true;

        // Get the scene loaded
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);

        // Allow things on the scene to load before we continue
        // So that references are properly set
        yield return new WaitForEndOfFrame();

        // Lower the water and slide in the bushes
        StartCoroutine(MenuController.instance.SlideInRoutine(time));
        yield return StartCoroutine(WaterTransition.instance.DescendRoutine(time));

        OnLevelLoaded(buildIndex);
        IsTransitioning = false;
    }

    IEnumerator TransitionToScene(int buildIndex)
    {
        IsTransitioning = true;
        HudUIForLevelTransition();

        var src = AudioManager.instance.PlayClip(SFXLibrary.instance.waveClip);
        var time = src.clip.length * 0.5f;

        yield return StartCoroutine(WaterTransition.instance.RiseRoutine(time));
        SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
        yield return StartCoroutine(WaterTransition.instance.DescendRoutine(time));

        // Wait for things to update 
        yield return new WaitForEndOfFrame();
        OnLevelLoaded(buildIndex);
        IsTransitioning = false;
    }
}
