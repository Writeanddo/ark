using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [SerializeField, Tooltip("How many columns (x) and rows (y)")] Vector2 scale;
    public Vector2 Scale { get { return scale; } }

    [SerializeField, Tooltip("Empty node that can become whatever we want")] TileNode nodePrefab;
    public TileNode NodePrefab { get { return nodePrefab; } }

    public List<Transform> Children
    {
        get
        {
            List<Transform> children = new List<Transform>();

            for (int i = 0; i < transform.childCount; i++)
                children.Add(transform.GetChild(i));

            return children;
        }
    }

    public void CreateGrid()
    {
        DestroyChildren();

        for (int x = 0; x < (int)scale.x; x++)
        {
            for (int y = 0; y < (int)scale.y; y++)
            {
                var node = Instantiate(nodePrefab, transform) as TileNode;
                node.name = $"{node.Type.name}_{x}_{y}";
                node.transform.localPosition = new Vector3(x, y, 0f);
            }
        }

        // Update position to center the grid
        // Negative since we need to offset it so that the center tile is the center point of this object
        transform.position = new Vector3(
            -scale.x / 2,
            -scale.y / 2,
            transform.position.z
        ); 
    }

    private void DestroyChildren()
    {
        Children.ForEach(c => Destroy(c.gameObject));
    }
}
