using UnityEngine;

/// <summary>
/// A collection of the sound effects available 
/// It is suggested that the names of the properties match
/// the names of the audio clip but they can be called whatever you want
/// </summary>
public class SFXLibrary : Singleton<SFXLibrary>
{
    public AudioClip sonSelectClip;
    public AudioClip pathPlaceClip;
    public AudioClip pathRemoveClip;
    public AudioClip pathIntoArkClip;

    [Space(10)] // 10 pixels of spacing here.

    public AudioClip buttonHoverClip;
    public AudioClip buttonStartClip;
    public AudioClip buttonStopClip;
    public AudioClip buttonMenuClip;

    [Space(10)] // 10 pixels of spacing here.

    public AudioClip[] walkingSteps;
    public AudioClip arkEnterClip;
    public AudioClip arkDoorsClip;

    [Space(10)] // 10 pixels of spacing here.

    public AudioClip openTextBoxClip;
    public AudioClip typeTextClip;

    [Space(10)] // 10 pixels of spacing here.

    public AudioClip waveClip;

    public AudioClip RandomClip(AudioClip[] clips)
    {
        return clips[Random.Range(0, clips.Length)];
    }
}
