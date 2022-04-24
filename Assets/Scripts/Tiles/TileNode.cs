using System.Collections.Generic;
using UnityEngine;

public class TileNode: MonoBehaviour
{
    [System.Serializable]
    struct TileTypePrefab
    {
        public TileType type;
        public GameObject prefab;
    }

    // A way for our editor scripts to spawn tile nodes
    [SerializeField, Tooltip("Which tile type to spawn")]
    TileType type;
    public TileType Type { get { return type; } }

    [SerializeField, Tooltip("Where the actual tile is spawned")]
    GameObject prefabContainer;
    public GameObject PrefabContainer { get { return prefabContainer; } set { prefabContainer = value; } }


    [SerializeField, Tooltip("The prefabs associated with each type")]
    List<TileTypePrefab> tileTypePrefabs;

    Dictionary<TileType, GameObject> prefabDatabase;
    public Dictionary<TileType, GameObject> PrefabDatabase
    {
        get
        {
            if (prefabDatabase == null)
            {
                prefabDatabase = new Dictionary<TileType, GameObject>();
                foreach (var t in tileTypePrefabs)
                    prefabDatabase[t.type] = t.prefab;
            }
            return prefabDatabase;
        }
    }
}
