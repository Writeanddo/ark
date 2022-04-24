using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileGrid : Singleton<TileGrid>
{
    public Tile[,] Tiles { get; protected set; }

    void Start() => CreateGrid();
    void CreateGrid()
    {
        var tiles = FindObjectsOfType<Tile>().ToList();
        var xMax = tiles.Max(t => t.X);
        var yMax = tiles.Max(t => t.Y);

        // Plus 1 because of zero based counting?
        Tiles = new Tile[xMax+1, yMax+1];

        foreach (var tile in tiles)
            SetTile(tile.X, tile.Y, tile);

        foreach (var tile in tiles)
            SetNeighborgs(tile);
    }

    public void SetNeighborgs(Tile tile)
    {
        foreach (var direction in Utility.cardinalDirections)
        {
            var point = tile.Position + direction;
            var neighbor = GetTile(point);

            // We want to save the direction and not the position
            // since position is in relations to the GRID position
            tile.AddNeighbor(direction, neighbor);
        }
    }

    public bool TileInBounds(int x, int y)
    {
        return x >= 0 && x < Tiles.GetLength(0) &&
               y >= 0 && y < Tiles.GetLength(1);
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if (TileInBounds(x, y))
        {
            if(tile != null)
                tile.name = $"{tile.name}_{tile.X}_{tile.Y}";
            Tiles[x, y] = tile;
        }
            
    }

    public Tile GetTile(int x, int y)
    {
        Tile tile = null;
        if (TileInBounds(x, y))
            tile = Tiles[x, y];

        return tile;
    }

    public Tile GetTile(Vector2 position)
    {
        return GetTile((int)position.x, (int)position.y);
    }

    public T GetTile<T>(Vector2 position) where T : Tile
    {
        var tile = GetTile(position);
        return tile != null ? tile.GetComponent<T>() : null;
    }

    public T GetTile<T>(int x, int y) where T : Tile
    {
        var tile = GetTile(x, y);
        return tile != null ? tile.GetComponent<T>() : null;
    }
}
