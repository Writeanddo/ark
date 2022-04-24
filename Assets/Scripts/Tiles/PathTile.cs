using UnityEngine;
using UnityEngine.EventSystems;

public class PathTile : Tile
{
    [SerializeField, Tooltip("For Debugging the bottom tile")] 
    PathType bottomPathType;
    [SerializeField, Tooltip("For Debugging the owner of the bottom tile")]
    ShepherdTile bottomShepherd;
    public PathType BottomPathType { get { return bottomPathType; } set { bottomPathType = value; } }
    public bool IsBottomStraightPath 
    {
        get { return bottomPathType == PathType.Vertical || bottomPathType == PathType.Horizontal; } 
    }

    public bool IsStraightOrEmpty 
    { 
        get 
        {
            return IsEmpty || (IsBottomStraightPath || IsTopStraightPath);
        } 
    }

    [SerializeField, Tooltip("For Debugging the top tile")]
    PathType topPathType;
    [SerializeField, Tooltip("For Debugging the owner of the top tile")]
    ShepherdTile topShepherd;
    public PathType TopPathType { get { return topPathType; } set { topPathType = value; } }
    public bool IsTopStraightPath
    {
        get { return topPathType == PathType.Vertical || topPathType == PathType.Horizontal; }
    }

    /// <summary>
    /// True when both the top/bottom are not empty
    /// Technically only checking the TOP is enough since bottom is always filled first
    /// but at least this way we are covered
    /// </summary>
    public bool IsDoubleBooked { get { return BottomPathType != PathType.Empty && TopPathType != PathType.Empty; } }
    public bool IsEmpty { get { return BottomPathType == PathType.Empty && TopPathType == PathType.Empty; } }
    [SerializeField, Tooltip("Tiles will default to being placed here")] 
    SpriteRenderer bottomSprite;

    [SerializeField, Tooltip("For when another wants to place a tile ontop of this one")]
    SpriteRenderer topSprite;

    [SerializeField, Tooltip("To mark it as being currently selected")]
    SpriteRenderer borderSprite;

    bool HasBeenClicked = false;

    public Tracks BottomTracks { get; set; }
    public Tracks TopTracks { get; set; }


    protected override void Start()
    {
        base.Start();
        Renderer.sprite = null; // hides the guide we use during dev
        borderSprite.enabled = false;
    }

    protected override void Update()
    {
        if (bottomSprite != null && BottomTracks != null && BottomTracks.Database.ContainsKey(BottomPathType))
            bottomSprite.sprite = BottomTracks.Database[BottomPathType];

        if (topSprite != null && TopTracks != null && TopTracks.Database.ContainsKey(TopPathType))
            topSprite.sprite = TopTracks.Database[TopPathType];
    }

    public bool EnableBorder { set { borderSprite.enabled = value; } }

    public virtual void ResetSprite(ShepherdTile owner)
    {
        EnableBorder = false;
        UpdatePathType(owner, PathType.Empty);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        HasBeenClicked = false;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        HasBeenClicked = false;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!LevelController.instance.OldPathMethod)
            return;

        base.OnPointerEnter(eventData);

        // Entering the tile while holding down the LMB is the same as if it was clicked on
        if (Input.GetMouseButton(0))
            OnPointerDown(eventData);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!LevelController.instance.OldPathMethod)
            return;

        // Since PointerDown means mouse cursor is still pressed
        // We don't want to call it until either they have stopped 
        // Pressing it or have moved to another tile
        if (HasBeenClicked)
            return;

        HasBeenClicked = true;
        LevelController.instance.OnPathTileClicked(this);
    }

    public void UpdateSpriteBasedOnNeigborsPositon(Vector2 prev, Vector2 next, ShepherdTile shepherd)
    {
        // Cannot trust the values are absolute zeros
        // Relying on approximation instead
        var hasNorthTile = Mathf.Approximately(prev.y, 1f) || Mathf.Approximately(next.y, 1f);
        var hasSouthTile = Mathf.Approximately(prev.y, -1f) || Mathf.Approximately(next.y, -1f);

        var hasWestTile = Mathf.Approximately(prev.x, -1f) || Mathf.Approximately(next.x, -1f);
        var hasEastTile = Mathf.Approximately(prev.x, 1f) || Mathf.Approximately(next.x, 1f);

        //Debug.Log($"Prev: {prev}, Next: {next}");
        //Debug.Log($"hasNorthTile: {hasNorthTile}, hasSouthTile: {hasSouthTile}");
        //Debug.Log($"hasWestTile: {hasWestTile}, hasEastTile: {hasEastTile}");

        // Default to empty
        PathType pathType = PathType.Empty;

        // Top Left Corner
        if (!hasNorthTile && !hasWestTile && hasSouthTile && hasEastTile)
            pathType = PathType.TopLeftCorner;

        // Bottom Left Corner
        else if (hasNorthTile && !hasWestTile && !hasSouthTile && hasEastTile)
            pathType = PathType.BottomLeftCorner;

        // Bottom Right Corner
        else if (hasNorthTile && hasWestTile && !hasSouthTile && !hasEastTile)
            pathType = PathType.BottomRightCorner;

        // Top Right Corner
        else if (!hasNorthTile && hasWestTile && hasSouthTile && !hasEastTile)
            pathType = PathType.TopRightCorner;

        // Horizontal
        else if (!hasNorthTile && !hasSouthTile && (hasEastTile || hasWestTile))
            pathType = PathType.Horizontal;

        // Vertical
        else if (!hasEastTile && !hasWestTile && (hasNorthTile || hasSouthTile))
            pathType = PathType.Vertical;

        else
            Debug.Log($"Could not determine sprite for {name} based on Prev: {prev} and Next: {next}");

        UpdatePathType(shepherd, pathType);
    }

    /// <summary>
    /// To determine which tile to update (bottom or top)
    /// We first need to know if this owner already owns one of the other
    /// If they own one already that's the one we want to update
    /// Otherwise whichever is available is the one we want to update
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="type"></param>
    public void UpdatePathType(ShepherdTile owner, PathType type)
    {
        // The shepherd might own BOTH tiles since you can cross over your own path
        // We se need to clear both when we are trying to clear things
        if (type == PathType.Empty && owner != null)
        {
            if (bottomShepherd == owner)
            {
                BottomPathType = type;
                bottomShepherd = null;
            }

            if (topShepherd == owner)
            {
                TopPathType = type;
                topShepherd = null;
            }       
        }

        // If it's not an Empty one we can then assign them according
        //Prioritizer the bottom tile always
        else if (bottomShepherd == owner)
            BottomPathType = type;
        else if (topShepherd == owner)
            TopPathType = type;

        // Assing them to the first available sprite
        else if (owner != null)
        {
            if(bottomShepherd == null)
            {
                BottomPathType = type;
                bottomShepherd = owner;
                BottomTracks = owner.Tracks;
            }
            else if (topShepherd == null)
            {
                TopPathType = type;
                topShepherd = owner;
                TopTracks = owner.Tracks;
            }
        }

        // Let's also make sure its neighbors are not highlighted
        RemoveHighlightFromNeighbors();
    }

    public PathType GetPathTypeOwnedByShepherd(ShepherdTile owner)
    {
        var type = PathType.Empty;

        if (bottomShepherd == owner)
            type = BottomPathType;
        else if (topShepherd == owner)
            type = TopPathType;

        return type;
    }

    public bool IsTileOwnedByShepherdStraight(ShepherdTile owner)
    {
        var isStraight = false;

        if (bottomShepherd == owner)
            isStraight = IsBottomStraightPath;
        else if (topShepherd == owner)
            isStraight = IsTopStraightPath;

        return isStraight;
    }

    /// <summary>
    /// True when the shepherd owns at least ONE of the tiles
    /// </summary>
    /// <param name="owner"></param>
    /// <returns></returns>
    public bool IsTileOwnedByShepherd(ShepherdTile owner)
    {
        return bottomShepherd == owner || topShepherd == owner;
    }
    
    /// <summary>
    /// Since a tile can be owned by two shepherds we will start with the bottom
    /// and then top returning the first of the two that is NOT null.
    /// However, if an expected owner is provided and one of the tile's owner
    /// if the expected, we will return the expected owner
    /// </summary>
    /// <param name="expectedOwner"></param>
    /// <returns></returns>
    public ShepherdTile GetOwner(ShepherdTile expectedOwner = null)
    {
        ShepherdTile owner = null;

        if(expectedOwner != null)
        {
            if (bottomShepherd == expectedOwner || topShepherd == expectedOwner)
                owner = expectedOwner;
        }

        if (owner == null)
            owner = bottomShepherd != null ? bottomShepherd : topShepherd;

        return owner;
    }
}
