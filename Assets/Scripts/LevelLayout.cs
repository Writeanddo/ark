using System.Collections;
using UnityEngine;

public class LevelLayout : Singleton<LevelLayout>
{
    [SerializeField] GameObject bushes;
    [SerializeField] GameObject gameTile;
    [SerializeField] GameObject controls;

    [SerializeField] Vector3 bushesDescendPosition;
    [SerializeField] Vector3 bushesStartingPosition;

    private void Start()
    {
        bushesStartingPosition = bushes.transform.localPosition;
        bushesDescendPosition.x = bushesStartingPosition.x;
        bushesDescendPosition.z = bushesStartingPosition.z;
        OnLevelLoaded();
    }

    public void OnLevelLoaded()
    {
        var isMainMenu = GameManager.instance.CurrentLevel < 1;
        EnableGameTile(isMainMenu);
    }

    public void EnableGameTile(bool enable) => gameTile.SetActive(enable);
    public void EnableControls(bool enable) { }

    public IEnumerator DescendBushesRoutine(float time)
    {
        yield return StartCoroutine(MoveRoutine(time, bushesDescendPosition));
    }

    public IEnumerator RiseBushesRoutine(float time)
    {
        yield return StartCoroutine(MoveRoutine(time, bushesStartingPosition));
    }

    public IEnumerator MoveRoutine(float time, Vector3 destination)
    {
        var distance = Vector3.Distance(destination, bushes.transform.localPosition);
        var speed = distance / time;

        while (Vector3.Distance(destination, bushes.transform.localPosition) > .01f)
        {
            bushes.transform.localPosition = Vector3.MoveTowards(bushes.transform.localPosition, destination, speed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        bushes.transform.localPosition = destination;
    }
}
