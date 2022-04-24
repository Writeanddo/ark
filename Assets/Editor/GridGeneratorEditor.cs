using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridGenerator))]
public class GridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var grid = (GridGenerator)target;

        if(GUILayout.Button("Create Grid"))
            CreateGrid(grid);
    }

    void CreateGrid(GridGenerator grid)
    {
        DestroyChildren(grid);

        var scale = grid.Scale;
        for (int x = 0; x < (int)scale.x; x++)
        {
            for (int y = 0; y < (int)scale.y; y++)
            {
                var node = PrefabUtility.InstantiatePrefab(grid.NodePrefab, grid.transform) as GameObject;
                node.name = $"Node_{x}_{y}";
                node.transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        // Update position to center the grid
        // Negative since we need to offset it so that the center tile is the center point of this object
        grid.transform.position = new Vector3(
            -scale.x / 2,
            -scale.y / 2,
            grid.transform.position.z
        );
    }

    void DestroyChildren(GridGenerator grid)
    {
        grid.Children.ForEach(c => DestroyImmediate(c.gameObject));
    }
}
