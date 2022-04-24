using UnityEngine;

[CreateAssetMenu(fileName = "ShepherdType", menuName = "Shepherd", order = 0)]
public class ShepherdType : TileType
{
    [Range(1, 3), Tooltip("Which order to process the shepherd's logic. Lowest means first")]
    public int priority;
    public Tracks tracks;
    public Sprite EntranceIcon;
    public Sprite ValidPairIcon;
    public Sprite InvalidPairIcon;
}