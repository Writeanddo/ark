using UnityEngine;

[ExecuteInEditMode]
public class TileEditor : MonoBehaviour
{
    Tile tile;
    Tile Tile {
        get
        {
            if (tile == null)
                tile = GetComponent<Tile>();
            return tile;
        }
    }
    protected virtual void Update()
    {
        if (Application.isPlaying)
            return;

        if (Tile != null && Tile.TileType != null && Tile.Renderer != null)
        {
            Tile.Renderer.sprite = Tile.TileType.sprite;
            Tile.Renderer.sortingLayerName = Tile.TileType.sortingLayer;
        }
            
    }
}
