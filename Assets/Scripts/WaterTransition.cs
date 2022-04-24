using System.Collections;
using UnityEngine;

public class WaterTransition : Singleton<WaterTransition>
{
    [SerializeField] Vector3 risenPosition;
    [SerializeField] Vector3 startingPosition;

    private void Start()
    {
        startingPosition = transform.position;
    }

    public IEnumerator RiseRoutine(float time)
    {
        var position = transform.position;
        position.y = risenPosition.y;
        yield return StartCoroutine(MoveToPosition(position, time));
    }

    public IEnumerator DescendRoutine(float time)
    {
        var position = transform.position;
        position.y = startingPosition.y;
        yield return StartCoroutine(MoveToPosition(position, time));
    }

    IEnumerator MoveToPosition(Vector3 position, float time)
    {
        var distance = Vector3.Distance(position, transform.position);        
        var speed = distance / time;

        while (Vector3.Distance(position, transform.position) > .01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        transform.position = position;
    }
}
