using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tracks", menuName = "Tracks", order = 0)]
public class Tracks : ScriptableObject
{
    [System.Serializable]
    protected struct SpriteMapping
    {
        public PathType type;
        public Sprite sprite;
    }

    [SerializeField] Sprite blockSprite;
    public Sprite BlockSprite { get { return blockSprite; } }
    [SerializeField] protected SpriteMapping[] mappings;
    Dictionary<PathType, Sprite> database;
    public Dictionary<PathType, Sprite> Database
    {
        get
        {
            if (database == null)
            {
                database = new Dictionary<PathType, Sprite>();
                foreach (var mapping in mappings)
                    database[mapping.type] = mapping.sprite;
            }
            return database;
        }
    }
}
