using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ArkEntranceTile : PathTile, IResetable
{
    [SerializeField] SpriteRenderer shepherdIconRenderer;
    [SerializeField] SpriteRenderer matchIconRenderer;
    [SerializeField, Tooltip("For Debugging the shepherd that will go here")]
    ShepherdTile shepherd;
    public ShepherdTile Shepherd 
    { 
        get { return shepherd; } 
        set
        {
            shepherd = value;
            if (shepherd == null)
                SetShepherdIcon(null);
            else
            {
                SetShepherdIcon(shepherd.Icon);
                AudioManager.instance.PlayClip(SFXLibrary.instance.pathIntoArkClip);
            }
        }
    }

    [System.Serializable]
    struct EntranceSpriteMapping
    {
        public EntranceState state;
        public Sprite sprite;
    }

    [SerializeField] EntranceState state;
    public EntranceState State 
    { 
        get { return state; } 
        set { 
            state = value;
            Renderer.sprite = StateSprite;
        } 
    }

    [SerializeField] EntranceSpriteMapping[] spriteMappings;
    Dictionary<EntranceState, Sprite> database;
    public Dictionary<EntranceState, Sprite> Database
    {
        get
        {
            if (database == null)
            {
                database = new Dictionary<EntranceState, Sprite>();
                foreach (var mapping in spriteMappings)
                    database[mapping.state] = mapping.sprite;
            }
            return database;
        }
    }

    public Sprite StateSprite
    {
        get
        {
            return Database.ContainsKey(State) ? Database[State] : null;
        }
    }

    public bool IsAvailable { get { return State == EntranceState.Opened && Shepherd == null; } }

    EntranceState startingState;

    /// <summary>
    /// Don't hide the tiles
    /// </summary>
    protected override void Start()
    {
        startingState = State;
    }

    protected override void Update()
    {
        ; // ignore the stuff to reset sprites since it does not happen with entrances
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        LevelController.instance.OnArkEntanceTileClicked(this);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (Input.GetMouseButton(0))
            OnPointerDown(eventData);
    }

    public void SetShepherdIcon(Sprite sprite) => shepherdIconRenderer.sprite = sprite;
    public void SetMatchIcon(Sprite sprite) => matchIconRenderer.sprite = sprite;
    public void CloseDoor() => State = EntranceState.Closed;
    public void OpenDoor() => State = EntranceState.Opened;


    /// <summary>
    /// Either the player hit the "R" or the "STOP" button
    /// Which means we want to remove the "you made a match icon"
    /// BUT we want to make sure that if a shepherd is here they stay here
    /// </summary>
    public void ResetObject()
    {
        // A closed door is not one we need to reset
        if (startingState == EntranceState.Closed)
            return;

        State = startingState;
        SetMatchIcon(null);
        if (shepherd != null)
            SetShepherdIcon(shepherd.Icon);
    }


    /// <summary>
    /// Tile was removed from the path
    /// </summary>
    /// <param name="owner"></param>
    public override void ResetSprite(ShepherdTile owner)
    {
        // A closed door is not one we need to reset
        if (startingState == EntranceState.Closed)
            return;

        Shepherd = null;
        SetShepherdIcon(null);
        SetShepherdIcon(null);
    }
}
