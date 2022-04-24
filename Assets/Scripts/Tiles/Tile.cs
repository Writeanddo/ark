using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TileEditor))]
public class Tile : MonoBehaviour, IComparable<Tile>, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Tooltip("Type of tile this is")] TileType type;
    public TileType TileType { get { return type; }  set { type = value; } }

    [SerializeField, Tooltip("The renderer to change the image to match the type")] 
    SpriteRenderer spriteRenderer;
    public SpriteRenderer Renderer { get { return spriteRenderer; } }
    public int SortingOrder 
    {
        set
        {
            spriteRenderer.sortingOrder = value;
        }
    }

    [SerializeField, Tooltip("The sprite renderer that shows this is the current tile highlighted")] 
    SpriteRenderer highlightSpriteRenderer;
    public bool IsHighlighted { get { return highlightSpriteRenderer.enabled; } }

    /// <summary>
    /// How much priority this node has over other nodes
    /// This is based on the distances traveled to that node
    /// </summary>
    public float Priority { get; set; }

    /// <summary>
    /// How much it cost to travel from one node to this one
    /// </summary>
    public float DistanceTraveled { get; set; } = Mathf.Infinity;

    /// <summary>
    /// Previous node used to get to this one
    /// </summary>
    public Tile PreviousNode { get; set; }

    protected Vector2 startingPosition;
    protected Quaternion initialRotation;
    protected Transform originalParent;
    public Transform OriginalParent { get { return originalParent; } }

    /// <summary>
    /// Position on the grid
    /// </summary>
    public virtual Vector2 Position 
    { 
        get { return new Vector2(transform.localPosition.x, transform.localPosition.y); }
        set
        {
            transform.localPosition = new Vector3(value.x, value.y, transform.localPosition.z);
        }
    }
    public int X { get { return (int)Position.x; } }
    public int Y { get { return (int)Position.y; } }
    
    Dictionary<Vector2, Tile> neighbors;
    public Dictionary<Vector2, Tile> Neighbors
    {
        protected set { neighbors = value; }
        get
        {
            if (neighbors == null)
                neighbors = new Dictionary<Vector2, Tile>();

            return neighbors;
        }
    }

    protected List<PathTile> GetPathNeighbors(bool includeOccupiedDoors = true)
    {
        var tiles = Neighbors.Values.OfType<PathTile>();

        // But exclude the closed doors
        if (includeOccupiedDoors)
            return tiles.ToList();
        else
            return tiles.Where(t => { return (t is ArkEntranceTile) ? ((ArkEntranceTile)t).IsAvailable : true; }).ToList();
    }

    protected virtual void Start()
    {
        name = $"{TileType.name}_{X}_{Y}";
        startingPosition = Position;
        initialRotation = transform.rotation;
        originalParent = transform.parent;
    }

    protected virtual void Update() {; } // just making it accessible to the childrens

    public bool HasNeighbor(Tile tile)
    {
        return Neighbors.Values.ToList().Contains(tile);
    }

    public Tile GetNeighbor(Vector2 position)
    {
        return Neighbors.ContainsKey(position) ?  Neighbors[position] : null;
    }
    public T GetNeighbor<T>(Vector2 position) where T: Tile
    {
        var tile = Neighbors.ContainsKey(position) ? Neighbors[position] : null;
        return tile != null ? tile.GetComponent<T>() : null;
    }

    public void AddNeighbor(Vector2 position, Tile neighbor)
    {
        Neighbors[position] = neighbor;
    }

    public void Initialize(int x, int y)
    {
        transform.localPosition = new Vector2(x, y);
        ResetNodes();
    }

    /// <summary>
    /// Returns sorting order based on priority
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Tile other)
    {
        if (Priority < other.Priority)
            return -1;
        else if (Priority > other.Priority)
            return 1;

        return 0;
    }

    public void ResetNodes()
    {
        PreviousNode = null;
        DistanceTraveled = Mathf.Infinity;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        LevelController.instance.OnMousePointerUp();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.instance.PlayClip(TileType.onMouseClickClip);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.instance.PlayClip(TileType.onMouseEnterClip);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        AudioManager.instance.PlayClip(TileType.onMouseExitClip);
    }

    public void HighlightNeighbors()
    {
        foreach (var neighbor in GetPathNeighbors(false))
            neighbor.HighlightTile(neighbor.IsStraightOrEmpty && !neighbor.IsDoubleBooked);
    }

    public void RemoveHighlightFromNeighbors()
    {
        foreach (var neighbor in GetPathNeighbors())
            neighbor.HighlightTile(false);
    }

    public void HighlightTile(bool showHighlight = true)
    {
        highlightSpriteRenderer.enabled = showHighlight;
    }
}
