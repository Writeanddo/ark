using UnityEngine;

[ExecuteInEditMode]
public class EntranceTileEditor : TileEditor
{
    ArkEntranceTile entranceTile;
    ArkEntranceTile EntranceTile {
        get
        {
            if (entranceTile == null)
                entranceTile = GetComponent<ArkEntranceTile>();
            return entranceTile;
        }
    }

    override protected void Update()
    {
        if (Application.isPlaying)
            return;

        if (EntranceTile != null && EntranceTile.TileType != null && EntranceTile.Renderer != null)
        {
            EntranceTile.Renderer.sprite = EntranceTile.StateSprite;
            EntranceTile.Renderer.sortingLayerName = EntranceTile.TileType.sortingLayer;
        }            
    }
}
