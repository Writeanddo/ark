using UnityEngine;
using System.Collections;

public class MenuController : Singleton<MenuController>
{
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject mainMenuButton;
    [SerializeField] GameObject levelButtons;
    [SerializeField] GameObject controlsMenu;

    [SerializeField] Vector3 slideInPosition;
    [SerializeField] Vector3 slideOutPosition;

    void Start() 
    {
        slideInPosition = settingsMenu.transform.localPosition;
        var isMainMenu = GameManager.instance.CurrentLevel < 1;

        // Always show settings
        ShowSettingsMenu(true);
        ShowControlsMenu(true);

        // These only need to be there when in main menu      
        ShowMainMenuButton(isMainMenu);
        ShowLevelButtons(!isMainMenu);
    }
    
    public void ShowSettingsMenu(bool show) => ChangeMenuState(settingsMenu, show);
    public void ShowControlsMenu(bool show) => ChangeMenuState(controlsMenu, show);
    public void ShowMainMenuButton(bool show) => ChangeMenuState(mainMenuButton, show);
    public void ShowLevelButtons(bool show) => ChangeMenuState(levelButtons, show);
    void ChangeMenuState(GameObject menu, bool opened) => menu.SetActive(opened);

    public IEnumerator SlideOutRoutine(float time)
    {
        StartCoroutine(MoveRoutine(controlsMenu.transform, time, slideOutPosition));
        yield return StartCoroutine(MoveRoutine(settingsMenu.transform, time, -slideOutPosition));
    }

    public IEnumerator SlideInRoutine(float time)
    {
        StartCoroutine(MoveRoutine(controlsMenu.transform, time, slideInPosition));
        yield return StartCoroutine(MoveRoutine(settingsMenu.transform, time, slideInPosition));
    }

    public IEnumerator MoveRoutine(Transform target, float time, Vector3 destination)
    {
        var distance = Vector3.Distance(destination, target.transform.localPosition);
        var speed = distance / time;

        while (Vector3.Distance(destination, target.transform.localPosition) > .01f)
        {
            target.transform.localPosition = Vector3.MoveTowards(target.transform.localPosition, destination, speed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        target.transform.localPosition = destination;
    }
}
