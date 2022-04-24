using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileNode))]
public class TileNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TileNode node = (TileNode)target;

        if (node.Type == null)
            return;

        if (!node.PrefabDatabase.ContainsKey(node.Type))
            return;
        
        if (node.PrefabContainer == null)
        {
            node.PrefabContainer = new GameObject($"{node.Type.name}_Container");
            node.PrefabContainer.transform.SetParent(node.transform);
        }

        var prefabName = $"{node.PrefabDatabase[node.Type].name}";
        // Prefab already exist - no need to re-create
        if (node.PrefabContainer.transform.Find(prefabName) != null)
            return;

        // Ensure the prefab is clean
        for (int i = 0; i < node.PrefabContainer.transform.childCount; i++)
        {
            var child = node.PrefabContainer.transform.GetChild(i);
            DestroyImmediate(child.gameObject);
        }

        var go = PrefabUtility.InstantiatePrefab(node.PrefabDatabase[node.Type], node.PrefabContainer.transform) as GameObject;
        go.transform.localPosition = Vector3.zero;
        go.name = prefabName;

        var t = go.GetComponent<Tile>();
        t.TileType = node.Type;
    }
}
