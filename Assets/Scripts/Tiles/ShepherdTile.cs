using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShepherdTile : Tile, IResetable
{
    [SerializeField] Animator animator;
    public Tracks Tracks { get { return ShepherdType.tracks; } }

    [SerializeField, Tooltip("The xForm to move")] Transform shepherdModel;
    public Transform ShepherdModel { get { return shepherdModel; } }

    [SerializeField, Tooltip("The xForm to rotate")] Transform shepherdContainer;
    public Transform ShepherdContainer { get { return shepherdContainer; } }

    [SerializeField, Tooltip("The block the shepherd starts at")] SpriteRenderer blockRenderer;

    [SerializeField, Tooltip("To visually see the path during testing")]
    List<PathTile> path;
    public List<PathTile> Path
    {
        get
        {
            if (path == null)
                path = new List<PathTile>();
            return path;
        }
    }

    /// <summary>
    /// Updates/Uses the ShepherdModel position since that is what we visually move
    /// We also want to use "world" and not "local" since we move it while still attached to the parent
    /// </summary>
    public override Vector2 Position
    {
        get { return new Vector2(ShepherdModel.position.x, ShepherdModel.position.y); }
        set { ShepherdModel.position = new Vector3(value.x, value.y, ShepherdModel.position.z); }
    }

    protected int pathIndex = 0;
    public void ResetIndex() => pathIndex = 0;
    public bool HasTileToWalkOn { get { return pathIndex < Path.Count; } }
    public PathTile NextTile
    {
        get
        {
            PathTile tile = null;

            if(HasTileToWalkOn)
            {
                tile = Path[pathIndex];
                pathIndex++;
            }

            return tile;
        }
    }

    public bool LastTileIsEntrance 
    { 
        get 
        {
            var last = Path.LastOrDefault();
            return last != null && last.GetComponent<ArkEntranceTile>() != null;
        } 
    }

    List<AnimalTile> animals;
    public List<AnimalTile> Animals
    {
        get
        {
            if (animals == null)
                animals = new List<AnimalTile>();
            return animals;
        }
    }

    public bool HasAnimals { get { return Animals.Count > 0; } }
    public ShepherdType ShepherdType { get { return (ShepherdType)TileType; } }
    public Sprite Icon { get { return ShepherdType.EntranceIcon; } }
    public Sprite MatchIcon { get { return HasPair ? ShepherdType.ValidPairIcon : ShepherdType.InvalidPairIcon; } }

    public void AnimalPickedUp(AnimalTile animal)
    {
        if (animal != null && !Animals.Contains(animal))
        {
            animal.IsPickedUp = true;
            Animals.Add(animal);
            SetAnimalsSortingOrder();
        }
    }

    public void AnimalDroppedOff(AnimalTile animal)
    {
        if (animal != null && Animals.Contains(animal))
        {
            Animals.Remove(animal);
            SetAnimalsSortingOrder();
        }   
    }

    void SetAnimalsSortingOrder()
    {
        for (int i = 0; i < Animals.Count; i++)
        {
            var animal = Animals[i];
            animal.SortingOrder = i;
        }
    }

    public bool CanCarryMoreAnimals(int maxAnimals)
    {
        return Animals.Count < maxAnimals;
    }
    
    public bool HasPair
    {
        get
        {
            bool hasPair = false;
            if(Animals.Count == 2)
                hasPair = Animals.First().TileType == Animals.Last().TileType;

            return hasPair;
        }
    }

    public int PriorityOrder { get { return ShepherdType.priority; } }
    protected override void Start()
    {
        base.Start();

        // Show the block and detach it
        // So that it does not move with the shepherd
        if (blockRenderer != null)
            blockRenderer.sprite = Tracks.BlockSprite;

        initialRotation = ShepherdContainer.rotation;
        ShepherdModel.SetParent(null);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);        
        LevelController.instance.OnShepheredTileClicked(this);
        animator.SetTrigger("Clicked");
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        ; // Ignore it
    }

    public void ResetPath()
    {
        Path.ForEach(p => p.ResetSprite(this));
        Path.Clear();
    }

    public void AddPathTile(PathTile tile)
    {
        if (tile != null)
        {
            Path.Add(tile);
            AudioManager.instance.PlayClip(SFXLibrary.instance.pathPlaceClip, false, Random.Range(0.75f, 1f));
        }
    }

    public void RemovePathTile(PathTile tile)
    {
        if (tile != null && Path.Contains(tile))
        {
            tile.ResetSprite(this);
            Path.Remove(tile);
        }   
    }

    public void RemovePathTilesFrom(PathTile tile)
    {
        if (tile == null || !Path.Contains(tile))
            return;

        AudioManager.instance.PlayClip(SFXLibrary.instance.pathRemoveClip, false, Random.Range(0.75f, 1f));

        // LastIndexOf since we could have multiples instances of this tile (a.k.a. CROSS)
        // But we will always go from last to first
        var index = Path.LastIndexOf(tile);

        // First make sure to reset them to empty
        for (int i = index; i < Path.Count; i++)
        {
            var pathTile = Path[i];
            pathTile.ResetSprite(this);
        }

        // Now we can remove them
        Path.RemoveRange(index, Path.Count - index);
    }

    public void Hide() => ShepherdModel.gameObject.SetActive(false);
    public void Show() => ShepherdModel.gameObject.SetActive(true);

    public void ResetObject()
    {
        ShepherdModel.position = startingPosition;
        ShepherdContainer.rotation = initialRotation;
        Show();
        Animals.Clear();
    }

    public void DisableHighlights()
    {
        Path.ForEach(t => { t.RemoveHighlightFromNeighbors(); t.EnableBorder = false; });
        RemoveHighlightFromNeighbors();
    }
}
