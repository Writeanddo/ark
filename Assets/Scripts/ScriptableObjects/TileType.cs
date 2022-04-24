using UnityEngine;

[CreateAssetMenu(fileName = "TileType", menuName = "Tile", order = 0)]
public class TileType : ScriptableObject
{
    public Sprite sprite;
    public string sortingLayer;
    public AudioClip onMouseEnterClip;
    public AudioClip onMouseExitClip;
    public AudioClip onMouseClickClip;
}
