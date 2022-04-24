using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : Singleton<LevelController>
{
    [SerializeField, Tooltip("On: keeps playing the sound while moving. Off: plays the steps once")]
    bool loopStepsClip;

    [SerializeField, Range(0.1f, 1f), Tooltip("How long actions take (in seconds) when not in fast-forward mode. Walking, picking up/dropping off")] 
    float actionTime = .4f;

    [SerializeField, Range(0.1f, 100f), Tooltip("The smaller the number the faster they rotate")] 
    float rotationTime = 20f;

    [SerializeField, Tooltip("How many animals at once a shepherd can cary")] 
    int maximumAnimalsPerShepherd = 2;

    float movementSpeed = 0f;
    float MovementSpeed
    {
        set { movementSpeed = value; }
        get { return movementSpeed * (IsFastForwardOn ? 2f : 1f);  }
    }

    float rotationSpeed = 0f;
    float RotationSpeed
    {
        set { rotationSpeed = value; }
        get { return rotationSpeed * (IsFastForwardOn ? 2f : 1f); }
    }

    [SerializeField] GameObject tilesContainer;
    GameObject TilesContainer
    {
        get
        {
            if (tilesContainer == null)
                tilesContainer = GameObject.FindGameObjectWithTag("TilesContainer");
            return tilesContainer;
        }
    }

    public LevelMode LevelMode { get; protected set; } = LevelMode.Edit;

    [SerializeField]
    List<Vector2> priorityDirections = new List<Vector2>()
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    List<ShepherdTile> shepherds;
    List<ShepherdTile> Shepherds
    {
        get
        {
            if (shepherds == null)
                shepherds = FindObjectsOfType<ShepherdTile>().ToList();
            return shepherds;
        }
    }

    List<ArkEntranceTile> entrances;
    List<ArkEntranceTile> Entrances
    {
        get
        {
            if (entrances == null)
                entrances = FindObjectsOfType<ArkEntranceTile>().ToList();
            return entrances;
        }
    }

    /// <summary>
    /// Current shephered whose path is being drawn
    /// </summary>
    ShepherdTile currentShepherd;

    List<ArkEntranceTile> EntrancesConnected
    {
        get
        {
            // Get all the entrances currently in each of the shepher's path
            var connected = Shepherds
                            .Select(s => s.Path.OfType<ArkEntranceTile>().FirstOrDefault())
                            .Where(v => v != null).ToList();

            return connected;
        }
    }

    public bool AllEntrancesConnected
    {
        get
        {
            return Shepherds.Count == EntrancesConnected.Count;
        }
    }

    List<IResetable> resetables;

    public bool HasAvailablePath
    {
        get
        {
            return Shepherds.Where(s => s.Path.Count > 0).FirstOrDefault() != null;
        }
    }

    AudioSource walkingAudioSource;
    void ChangeMode(LevelMode mode) => LevelMode = mode;

    public bool IsFastForwardOn { get; protected set; } = false;
    public void ToggleFastForward()
    {
        if (InPlayMode)
            IsFastForwardOn = !IsFastForwardOn;
    }

    public bool InPlayMode { get { return LevelMode == LevelMode.Playing; } }

    /// <summary>
    /// Must save everything soon as we are done restarting since we change parents
    /// </summary>
    private void Start()
    {
        resetables = TilesContainer?.GetComponentsInChildren<IResetable>().ToList();
    }

    Tile lastTile = null;
    public bool OldPathMethod = false;
    private void Update()
    {
        if (OldPathMethod || InPlayMode || !Input.GetMouseButton(0))
            return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = transform.position.z;
        var hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePos), Vector2.zero);

        if (!hit)
            return;

        if(hit)
        {
            var tile = hit.collider.GetComponent<PathTile>();
            if (lastTile != tile)
            {
                lastTile = tile;
                if(tile != null)
                    OnPathTileClicked(tile);
            }
        }  
    }

    public void OnLevelChanged()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (walkingAudioSource != null)
            walkingAudioSource.Stop();

        ChangeMode(LevelMode.Edit);
    }

    public void OnResetButtonPressed()
    {
        EnterEditMode();
        Shepherds.ForEach(s => s.ResetPath());
    }

    public void OnMousePointerUp()
    {
        switch (LevelMode)
        {
            case LevelMode.Drawing:
                ChangeMode(LevelMode.Edit);
                break;
        }
    }

    IEnumerator currentRoutine;
    public void EnterPlayMode()
    {
        if (LevelMode != LevelMode.Playing)
        {
            // Default to off
            // Players will need to turn it on each time
            IsFastForwardOn = false;
            ChangeMode(LevelMode.Playing);
            currentRoutine = PlayModeRoutine();
            StartCoroutine(currentRoutine);
        }
    }

    public void EnterEditMode()
    {
        // Ignore for now
        if (GameManager.instance.GameOver || LevelMode != LevelMode.Playing)
            return;

        StartCoroutine(EnterEditModeRoutine());
    }

    IEnumerator EnterEditModeRoutine()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        // Wait for all routines to stop
        yield return new WaitForEndOfFrame();

        // Reset everything
        resetables.ForEach(r => r.ResetObject());

        // Wait for the frame update to ensure it takes
        yield return new WaitForEndOfFrame();
        ChangeMode(LevelMode.Edit);
    }

    IEnumerator PlayModeRoutine()
    {
        // TODO: remove looping walking in favor for when they actually move
        //walkingAudioSource = AudioManager.instance.PlayLoopingClip(SFXLibrary.instance.sonWalkingLoopClip);

        // Create a queue for the shepherds that have a path to walk
        // ordering them by their "priority" so that we process them
        // in the expected order
        var activeShepherds = new List<ShepherdTile>(
            Shepherds.OrderBy(s => s.PriorityOrder).Where(t => t.Path.Count > 0)
        );

        // We will loop through each shephered and a process a complete turn before moving to the next shephered
        // This will be true until we have no more shepherds to move
        // A complete turn means:
        //  - Move a shepherds (all must move at the same time)
        //  - Based on order of priority, drop off and pickup when available
        //  - repeat this until all shepherds have exhausted their turns

        // We first need to ensure we've reset their path index to always start with the first tile on the path
        // We also want to disable any highlights that might still be active
        Shepherds.ForEach(s => { s.ResetIndex(); s.DisableHighlights(); });

        var shepherdsMoving = new List<ShepherdTile>();
        var shepherdsWaiting = new List<ShepherdTile>();

        while (InPlayMode && activeShepherds.Count > 0)
        {
            // Remove any active shepherds waiting for action to complete such as:
            // picking up, dropping off, or entering the ark
            // TODO: does not take into account if the last tile the shepherd move to 
            //       is the end of the path but has animals to drop/pickup
            activeShepherds = activeShepherds.Except(shepherdsWaiting).ToList();

            // This is a situation where the active shepherds are currently performing an action such as: pick up/drop off
            // and there are no other shepherds moving. What we want to do is wait unti all of them are done with their actions
            // Then we will continue to the next iteration so that we can move them
            if(activeShepherds.Count < 1)
            {                
                // Wait for all the actions currently to finish since it should all happen at the same time?
                while (InPlayMode && shepherdsWaiting.Count > 0)
                    yield return new WaitForEndOfFrame();

                // Wait one more frame to ensure that all actions have in deed completed
                yield return new WaitForEndOfFrame();

                // We need to now re-build the list before we move on to the next iterations
                activeShepherds = new List<ShepherdTile>(
                    Shepherds.OrderBy(s => s.PriorityOrder).Where(t => t.HasTileToWalkOn)
                );
                continue;
            }

            // Move all the sphereds a single step
            foreach (var shepherd in activeShepherds)
            {
                shepherdsMoving.Add(shepherd);
                StartCoroutine(MoveShepherdRoutine(shepherd, shepherdsMoving));
            }

            // Choose a random step to play
            var stepClip = SFXLibrary.instance.RandomClip(SFXLibrary.instance.walkingSteps);
            AudioSource audioSrc = null;

            // Play once
            if(!loopStepsClip && shepherdsMoving.Count > 0)
                audioSrc = AudioManager.instance.PlayClip(stepClip, .80f, 1.01f, true);

            // Must wait for everyone to stop moving before we can determine which actions can be performed
            while (shepherdsMoving.Count > 0)
            {
                // Random pitch plus only play once while it still plays 
                if (loopStepsClip)
                    audioSrc = AudioManager.instance.PlayClip(stepClip, .80f, 1.01f, true);
                yield return new WaitForEndOfFrame();
            }

            if (audioSrc != null)
                audioSrc.Stop();

            // One by one see if they can drop off/pick up an animal
            // Mark the barrels as not available for this step so that 
            // the other shepherds can ignore them
            List<AnimalTile> animalsToIgnore = new List<AnimalTile>();
            List<AnimalBarrelTile> barrelsToIgnore = new List<AnimalBarrelTile>();
            foreach (var shepherd in activeShepherds)
            {
                // Weird we could not find a tile for them
                var tile = TileGrid.instance.GetTile<PathTile>(shepherd.Position);
                if (tile == null)
                {
                    Debug.LogError($"{shepherd.name} has not tile at Position: {shepherd.Position}");
                    continue;
                }

                // FIRST 
                // Drop off animals if the shepherd has anything to drop off 
                if (shepherd.HasAnimals)
                {
                    // Get all surrounding barrels to locate an empty one
                    var barrels = GetNeighborsOfType(tile, priorityDirections.Count, barrelsToIgnore).Values.Where(b => b.IsEmpty).ToList();

                    if (barrels.Count > 0)
                    {
                        shepherdsWaiting.Add(shepherd);
                        barrelsToIgnore.AddRange(barrels);

                        // Clone the animals because we are going to remove them to free up space to grab more
                        var animals = new List<AnimalTile>();
                        for (int i = 0; i < barrels.Count; i++)
                        {
                            // Always start with the last animal the shepherd is carrying
                            var animal = shepherd.Animals.LastOrDefault();
                            if (animal == null)
                                break;

                            shepherd.AnimalDroppedOff(animal);
                            animals.Add(animal);
                        }

                        StartCoroutine(DropOffAnimalsRoutine(shepherd, animals, barrels, shepherdsWaiting));
                    }
                }

                // SECOND
                // Pick up animals with the following priority
                //  - if there's a matching animal then it is picked up first
                //  - else we will iterate through the priority directions
                if (shepherd.CanCarryMoreAnimals(maximumAnimalsPerShepherd))
                {
                    var animalsToPickup = new List<AnimalTile>();
                    var barrelsWithAnimalsToPickup = new List<AnimalBarrelTile>();
                                        
                    // Get all the adjancent animals/barrels to this current tile
                    // We want to get all sides so that we can find the animal we want/need
                    var adjacentAnimals = GetNeighborsOfType(tile, priorityDirections.Count, animalsToIgnore);
                    var adjacentBarrels = GetNeighborsOfType(tile, priorityDirections.Count, barrelsToIgnore);

                    // Extract the animals/barrels so that we can quickly look for a match
                    var animalNeighbors = adjacentAnimals.Values.Where(a => !a.IsPickedUp).ToList();
                    var animalsInBarrel = adjacentBarrels.Values.Where(b => b.HasAnimal).ToList();

                    // Try finding a match first 
                    if (shepherd.HasAnimals)
                    {
                        var animal = shepherd.Animals.First();
                        var matchingAnimal = animalNeighbors.Where(a => a.TileType == animal.TileType).FirstOrDefault();
                        var matchingBarrelAnimal = animalsInBarrel.Where(b => b.Animal.TileType == animal.TileType).FirstOrDefault();

                        // Add the matches we found if any
                        // We will also remove it from the available list in case we have room to collect others
                        if (matchingAnimal != null)
                        {
                            animalsToPickup.Add(matchingAnimal);
                            var item = adjacentAnimals.First(kvp => kvp.Value == matchingAnimal);
                            adjacentAnimals.Remove(item.Key);
                        }

                        if (matchingBarrelAnimal != null)
                        {
                            barrelsWithAnimalsToPickup.Add(matchingBarrelAnimal);

                            var item = adjacentBarrels.First(kvp => kvp.Value == matchingBarrelAnimal);
                            adjacentBarrels.Remove(item.Key);
                        }
                    }

                    // If the sum of the animals we are going to collect os less than our maximum allowed
                    // We will attempt to grab more animals
                    var maxAnimals = maximumAnimalsPerShepherd - shepherd.Animals.Count;
                    if (animalsToPickup.Count + barrelsWithAnimalsToPickup.Count < maxAnimals)
                    {
                        // We need to prioritize the pickups based on a predetermined order of directions 
                        foreach (var direction in priorityDirections)
                        {
                            // Tile might be the starting position of the animal
                            // therefore we will avoid it if it has been picked up
                            var animal = adjacentAnimals.ContainsKey(direction) ? adjacentAnimals[direction] : null;
                            if (animal != null && !animal.IsPickedUp)
                            {
                                shepherd.AnimalPickedUp(animal);
                                animalsToPickup.Add(animal);
                            }

                            var barrel = adjacentBarrels.ContainsKey(direction) ? adjacentBarrels[direction] : null;
                            if (barrel != null && barrel.HasAnimal)
                            {
                                shepherd.AnimalPickedUp(barrel.Animal);
                                barrelsWithAnimalsToPickup.Add(barrel);
                            }

                            // Cannot collect anymore animals
                            if (animalsToPickup.Count + barrelsWithAnimalsToPickup.Count >= maxAnimals)
                                break;
                        }
                    }

                    // Pick up the animal from their platform
                    if (animalsToPickup.Count > 0)
                    {
                        shepherdsWaiting.Add(shepherd);
                        animalsToIgnore.AddRange(animalsToPickup);
                        StartCoroutine(PickupAnimalsRoutine(animalsToPickup, shepherd, shepherdsWaiting));
                    }

                    // Pick up the animal from barrels
                    if (barrelsWithAnimalsToPickup.Count > 0)
                    {
                        shepherdsWaiting.Add(shepherd);
                        barrelsToIgnore.AddRange(barrelsWithAnimalsToPickup);
                        StartCoroutine(PickupAnimalsRoutine(barrelsWithAnimalsToPickup, shepherd, shepherdsWaiting));
                    }
                }
            }

            // For each shepherd that no longer has a tile to move on to
            // we wan to see if they have reached the end
            if (InPlayMode)
            {
                // Let's check if each shepherd(s) reached an entrance with a pair so that we can close the door or show the missmatching icon
                foreach (var shepherd in activeShepherds.OrderBy(s => s.PriorityOrder).Where(t => !t.HasTileToWalkOn))
                {
                    shepherdsWaiting.Add(shepherd);
                    StartCoroutine(CloseArkDoorRoutine(shepherd, shepherdsWaiting));
                }   
            }

            // Update the list to continue to work with the shepherds that still have a path
            activeShepherds = new List<ShepherdTile>(
                Shepherds.OrderBy(s => s.PriorityOrder).Where(t => t.HasTileToWalkOn)
            );

            // TODO: if a shepherd is entering the ark while another is picking up/dropping off
            //       their animations don't happen at the same time and forces the other shepherds to wait
            // If we no longer have tiles to move to 
            // then it is possible that all shepherds are at the closing door animation part
            // we want to wait for the door to finish closing before we move on
            if (activeShepherds.Count < 1 && shepherdsWaiting.Count > 0)
            {
                while (InPlayMode && shepherdsWaiting.Count > 0)
                    yield return new WaitForEndOfFrame();
            }
        }

        // The assumption here is that all shepherds have reached the end of their path
        // AllEntrancesConnected let's us know that each shepherd has a destination of an entrance
        // Therefore, so long as we are in playing mode all we need to do is check if we have pairs
        // and if so, winner is you!
        var totalShepherdsWithPairsAtAnEntrance = shepherds.Where(s => s.LastTileIsEntrance && s.HasPair).Count();

        // If all the shepherds in this level (Captipal S as the lower s is just for the ones moving)
        // have reached an arc's entrance with a pair then the level is cmpleted
        if (InPlayMode && Shepherds.Count == totalShepherdsWithPairsAtAnEntrance)
        {
            // Remove the icon since the level is completed
            Entrances.ForEach(e => e.SetMatchIcon(null));
            GameManager.instance.NextLevel();
        }

        //walkingAudioSource.Stop();
    }

    Dictionary<Vector2, T> GetNeighborsOfType<T>(Tile tile, int maxNeighbors, List<T> tilesToIngore = null) where T : Tile
    {
        // So that we can still test against it
        if (tilesToIngore == null)
            tilesToIngore = new List<T>();

        var neighbors = new Dictionary<Vector2, T>();
        foreach (var direction in priorityDirections)
        {
            var neighbor = tile.GetNeighbor<T>(direction);
            if (neighbor != null && !tilesToIngore.Contains(neighbor))
                neighbors[direction] = neighbor;

            if (neighbors.Count >= maxNeighbors)
                break;
        }

        return neighbors;
    }

    IEnumerator CloseArkDoorRoutine(ShepherdTile shepherd, List<ShepherdTile> shepherdsWaiting = null)
    {
        if (InPlayMode && shepherd.LastTileIsEntrance)
        {
            var entrance = shepherd.Path.Last().GetComponent<ArkEntranceTile>();
            // This will either be a broken heart or a whole heart
            entrance.SetMatchIcon(shepherd.MatchIcon);

            if (shepherd.HasPair)
            {
                // Enter by hiding the shepherd and the icon
                shepherd.Hide();
                entrance.SetShepherdIcon(null);
                var src = AudioManager.instance.PlayClip(SFXLibrary.instance.arkEnterClip);

                // Wait a bit to close the door
                yield return new WaitForSeconds(src.clip.length * .5f);

                if (InPlayMode)
                {
                    entrance.CloseDoor();
                    AudioManager.instance.PlayClip(SFXLibrary.instance.arkDoorsClip);
                    yield return new WaitForEndOfFrame();
                    shepherdsWaiting.Remove(shepherd);
                }
            }
        }

        // Make sure to remove them from waiting list
        if (InPlayMode)
            shepherdsWaiting.Remove(shepherd);
    }

    IEnumerator MoveShepherdRoutine(ShepherdTile shepherd, List<ShepherdTile> movingShepherds = null)
    {
        var tile = shepherd.NextTile;
        var xForm = shepherd.ShepherdModel;
        var direction = tile.transform.localPosition - xForm.localPosition;

        // Look at destination
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        var rotationRoutine = RotateToTargetRotationRoutine(shepherd, targetRotation, rotationTime);

        StartCoroutine(rotationRoutine);

        // Making sure we have speed set
        MovementSpeed = Vector3.Distance(xForm.localPosition, tile.transform.localPosition) / actionTime;

        // Move into the tile
        while (tile != null &&
               InPlayMode &&
               Vector3.Distance(xForm.localPosition, tile.transform.localPosition) > 0.01f)
        {
            xForm.localPosition = Vector3.MoveTowards(
                xForm.localPosition,
                tile.transform.localPosition,
                MovementSpeed * Time.deltaTime
            );

            yield return new WaitForEndOfFrame();
        }

        if (InPlayMode)
        {
            // Wait for the routine to stop or else we will have a conflict of interest
            StopCoroutine(rotationRoutine);
            yield return new WaitForEndOfFrame();

            shepherd.ShepherdContainer.rotation = targetRotation;
            xForm.localPosition = tile.transform.localPosition;
            if (movingShepherds != null && movingShepherds.Contains(shepherd))
                movingShepherds.Remove(shepherd);
        }
    }

    IEnumerator RotateToTargetRotationRoutine(ShepherdTile shepherd, Quaternion targetRotation, float time)
    {
        if (time < 0f)
            time = 1f;

        var xForm = shepherd.ShepherdContainer;

        // DO NOT OVERRIDE THE SPEED IF WE ARE NOT ROTATING
        // otherwise any shepherds rotating will stop rotating
        if(Vector3.Distance(xForm.eulerAngles, targetRotation.eulerAngles) > 0.01f)
            RotationSpeed = Vector3.Distance(xForm.eulerAngles, targetRotation.eulerAngles) / rotationTime;

        while (InPlayMode && Vector3.Distance(xForm.eulerAngles, targetRotation.eulerAngles) > 0.01f)
        {
            xForm.rotation = Quaternion.Lerp(xForm.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            yield return null;
        }

        // Ensure the objects rotation is what we expect
        if(InPlayMode)
            xForm.rotation = targetRotation;
    }

    IEnumerator DropOffAnimalsRoutine(ShepherdTile shepherd, List<AnimalTile> animals, List<AnimalBarrelTile> barrels, List<ShepherdTile> shepherdsWaiting = null)
    {
        var barrelQueue = new Queue<AnimalBarrelTile>(barrels);
        var animalQueue = new Queue<AnimalTile>(animals);
        var animalMoving = new List<AnimalTile>();
        while (InPlayMode && barrelQueue.Count > 0 && animalQueue.Count > 0)
        {
            var barrel = barrelQueue.Dequeue();
            if (barrel == null)
                break;

            var animal = animalQueue.Dequeue();
            if (animal == null)
                break;

            // We need to set it back to the original parent so that the 
            // the dropping off and picking up movement still works
            animalMoving.Add(animal);
            animal.transform.SetParent(animal.OriginalParent);
            StartCoroutine(MoveAnimalToDestination(animal, barrel.transform, animalMoving));

            // Remove the animal from the barrel
            if (InPlayMode)
                barrel.AddAnimal(animal);
        }

        while (InPlayMode && animalMoving.Count > 0)
            yield return new WaitForEndOfFrame();

        if (shepherdsWaiting != null && shepherdsWaiting.Contains(shepherd))
            shepherdsWaiting.Remove(shepherd);
    }

    IEnumerator PickupAnimalsRoutine(List<AnimalBarrelTile> barrels, ShepherdTile shepherd, List<ShepherdTile> shepherdsWaiting = null)
    {
        var queue = new Queue<AnimalBarrelTile>(barrels);
        var animalMoving = new List<AnimalTile>();
        while (InPlayMode && queue.Count > 0)
        {
            var barrel = queue.Dequeue();
            if (barrel == null || barrel.IsEmpty) continue;

            // No animal
            var animal = barrel.Animal;
            if (animal == null) continue;

            // Remove the animal from the barrel
            barrel.RemoveAnimal(animal);
            animalMoving.Add(animal);
            shepherd.AnimalPickedUp(animal);
            animal.transform.SetParent(animal.OriginalParent);
            StartCoroutine(MoveAnimalToDestination(animal, shepherd.ShepherdModel, animalMoving));
        }

        while (InPlayMode && animalMoving.Count > 0)
            yield return new WaitForEndOfFrame();

        if (shepherdsWaiting != null && shepherdsWaiting.Contains(shepherd))
            shepherdsWaiting.Remove(shepherd);
    }

    IEnumerator PickupAnimalsRoutine(List<AnimalTile> animals, ShepherdTile shepherd, List<ShepherdTile> shepherdsWaiting = null)
    {
        var queue = new Queue<AnimalTile>(animals);

        var animalMoving = new List<AnimalTile>();
        while (InPlayMode && queue.Count > 0)
        {
            var animal = queue.Dequeue();
            if (animal == null) continue;

            animalMoving.Add(animal);
            shepherd.AnimalPickedUp(animal);
            StartCoroutine(MoveAnimalToDestination(animal, shepherd.ShepherdModel, animalMoving));
        }

        while (InPlayMode && animalMoving.Count > 0)
            yield return new WaitForEndOfFrame();

        if (shepherdsWaiting != null && shepherdsWaiting.Contains(shepherd))
            shepherdsWaiting.Remove(shepherd);
    }

    IEnumerator MoveAnimalToDestination(AnimalTile animal, Transform destination, List<AnimalTile> animalMoving = null)
    {
        animal.Jump(true);
        AudioManager.instance.PlayClip(animal.TileType.onMouseClickClip);

        // Making sure we have speed set
        MovementSpeed = Vector3.Distance(destination.localPosition, animal.transform.localPosition) / actionTime;

        // Move to the shepher while in play mode and destination not reached
        while (InPlayMode && Vector3.Distance(destination.localPosition, animal.transform.localPosition) > 0.01f)
        {
            animal.transform.localPosition = Vector3.MoveTowards(
                animal.transform.localPosition,
                destination.localPosition,
                MovementSpeed * Time.deltaTime
            );

            yield return new WaitForEndOfFrame();
        }

        if (InPlayMode)
        {
            // Make sure it is snapped into place and attached to the shepherd so that it moves with it
            animal.transform.localPosition = destination.localPosition;
            animal.transform.SetParent(destination.transform);

            if(animalMoving != null && animalMoving.Contains(animal))
                animalMoving.Remove(animal);
        }

        animal.Jump(false);
    }

    public void OnShepheredTileClicked(ShepherdTile shepherd)
    {
        // Cannot do anything while in playing mode
        if (InPlayMode || GameManager.instance.GameOver)
            return;

        if (currentShepherd != null)
        {
            currentShepherd.DisableHighlights();
            currentShepherd = null;
        }

        currentShepherd = shepherd;

        // Show available neighbors from this shepherd when they don't have a path
        if (currentShepherd.Path.Count < 1)
            currentShepherd.HighlightNeighbors();
        else if (currentShepherd.Path.Count == 1)
            currentShepherd.ResetPath();
        else
        {
            // Highlight the last tile on the path's neighbors
            var lastTile = currentShepherd.Path.Last();
            var entrance = lastTile.GetComponent<ArkEntranceTile>();
            if (entrance == null)
            {
                lastTile.EnableBorder = true;
                lastTile.HighlightNeighbors();
            }
        }

        // Enter draw mode to avoid either deleting tiles since it looks like the player
        // might be trying to change shepherds only 
        // But also to avoid attempting to draw a second path from the shepherd
        // Since the logic to only allow drawing from the last tile should be triggered
        ChangeMode(LevelMode.Drawing);
    }

    public void OnArkEntanceTileClicked(ArkEntranceTile tile)
    {
        // Debug.Log($"{tile.name} was clicked on");

        // Ignore clicks when in playing mode
        // or if the door is closed
        if (InPlayMode || tile.State == EntranceState.Closed)
            return;

        // Entrance already has an owner so we don't want to double book them
        // Besides if the player wants to remove it they just have to click 
        // on the tile before the entrace
        if (tile.Shepherd != null)
            return;

        // Unlike a path tile you cannot "draw" from this tile
        // However we do need to add it to the of the current shepherd
        // and update all the previous tile if we have one
        if (currentShepherd == null)
            return; // Not working with a shephered

        // Shepherd's path must connect with the entrance
        // which means the last tile on their path must be a neighbor
        var lastTile = currentShepherd.Path.LastOrDefault();
        if (lastTile == null)
            return;

        // If the last tile is already an entrance then we want to ignore this one
        var entrace = lastTile.GetComponent<ArkEntranceTile>();
        if (entrace != null)
            return;

        // Looks like the player clicked on an entrance not next to the last tile of the path
        if (!tile.HasNeighbor(lastTile))
            return;

        // Looks like we can add this entrance to the path to complete it
        // We also want to disengage drawing mode since the player completed the path
        ChangeMode(LevelMode.Edit);
        currentShepherd.AddPathTile(tile);

        // Update the entrance to show the shepherd will go there
        tile.Shepherd = currentShepherd;

        // -2 to avoid updating the entrance
        UpdatePathSprites(currentShepherd, currentShepherd.Path.Count - 2);
    }

    public void OnPathTileClicked(PathTile tile)
    {
        // Ignore trying to place tiles while in play mode
        if (InPlayMode)
            return;

        // Before we can draw a path we need to have a track owner
        // The tile might already have a track owner which we want to identify
        // The current shepherd might be one of the owners therefore we send
        // it as the expected owner to get it if it owns it
        var owner = tile.GetOwner(currentShepherd);

        // Now we need to assign that to our current owner if we don't already have one
        if (currentShepherd == null)
            currentShepherd = owner;

        var lastTile = currentShepherd != null ? currentShepherd.Path.LastOrDefault() : null;

        // if (lastTile != null && !lastTile.HasNeighbor(tile))
        //    Debug.Log($"Tile: {tile} not a neighbor of {lastTile}");

        // When we are moving onto a node that already has a path that belongs to 
        // another shepher we need logic to determine when and how the overlap is done
        if (owner != null && currentShepherd != owner)
        {
            // Since the player could hold down the LMB to paint a path they might 
            // go over another shepherd's path. However, when in EDIT MODE they are 
            // allowed to click on another's shepherd's tile to SWITCH to that shepherd
            
            // Clicked somewhere not near the last tile so probably wants to switch
            // Only when in Edit mode since dragging the mouse we don't want to break it
            if (LevelMode == LevelMode.Edit && lastTile != null && !lastTile.HasNeighbor(tile))
            {
                // Since we are changing owners let's be save and remove highlights
                currentShepherd.DisableHighlights();
                currentShepherd = owner;
                lastTile = currentShepherd.Path.LastOrDefault();
            }
            else
            {
                // The player is attempting to place a tile on top of someone else's path
                // We cannot allow this if:

                // If this is the LAST TILE of that other shepherd
                // the do not allow placement of new tiles
                if (owner.Path.Last() == tile)
                    return;

                // - The tile is already double booked
                if (tile.IsDoubleBooked)
                    return;

                // - The tile is not straight
                if (!tile.IsTileOwnedByShepherdStraight(owner))
                    return;

                // The rest of the conditions only applies if the current shepherd 
                // has tiles in its path. If this is the first one then we've confirmed
                // that they are allowed to place it already
                if (currentShepherd.Path.Count > 0)
                {
                    // Let's make sure the path will be connected to the last tile on the
                    // current path by ensuring they are neighbors
                    var finalTile = currentShepherd.Path.Last();
                    if (!tile.HasNeighbor(finalTile))
                        return;

                    // The following only applies if the LAST tile on the current shepherd's path
                    // is already crossing another because we want to prevent the player from
                    // changing the direction of the cross section
                    // Therefore if the final tile is double booked then we need to do this logic
                    if (finalTile.IsDoubleBooked)
                    {
                        // Because the tile that we are crossing over does not belong to the current shepherd
                        // The logic later that determines if the player is still moving in the same direction
                        // when attempting to cross over an existing tile will not work since the tile exist 
                        // in someone else's path.
                        // Instead, we need to determine here if the move is legal

                        // First, we want to know the previous direction 
                        // Which we will default to be from the shepherd's starting position
                        var previousDirection = currentShepherd.Position - finalTile.Position;

                        // We need to prevent the playe from turning on a cross
                        // since they are only allowed to move forward through it
                        // We can determine this by ensuring that their previous and new directions
                        // are the same 

                        // Let's grab the direction the player is intending to move
                        var newDirection = finalTile.Position - tile.Position;

                        // If the player has more than one tile then we need to base
                        // the previous direction off the tile before the final tile
                        if (currentShepherd.Path.Count > 1)
                        {
                            var finalIndex = currentShepherd.Path.LastIndexOf(finalTile);
                            var tileBeforeFinal = currentShepherd.Path[finalIndex - 1];
                            previousDirection = tileBeforeFinal.Position - finalTile.Position;
                        }

                        // Directions do not match therefore the player cannot place the tile
                        if (newDirection != previousDirection)
                            return;
                    }
                }
            }
        }

        // We still don't have an onwer? 
        // Then we are done. 
        // The player must click first on a Shepherd tile before a path can be created
        if (currentShepherd == null)
            return;

        // We need to know the last tile (if any) for this Sphered's path
        // var lastTile = currentShepherd.Path.LastOrDefault();

        // If the tile we are working on is already the last tile
        // Then the player must be re-engaging drawing mode
        // but we don't need to do anything else until they move to a diff tile
        if (lastTile == tile)
        {
            ChangeMode(LevelMode.Drawing);
            return;
        }

        // Playing it safe in case the player moved the mouse really fast
        // We want to make sure that the tile clicked ON is a neighborg 
        // of either the shepherd when they don't have a path 
        // otheriwse it must be a neighbor of the last tile
        if (!currentShepherd.Path.Contains(tile))
        {
            // No tiles in the path and this tiles is NOT a neighbor of the current shepherd
            // We cannot buld there
            if (currentShepherd.Path.Count < 1 && !currentShepherd.HasNeighbor(tile))
                return;
            else if (currentShepherd.Path.Count > 0 && !lastTile.HasNeighbor(tile))
            {
                // But if the is a neighbor to the shephered then the player might be trying to start a brand new path
                if (currentShepherd.HasNeighbor(tile))
                    currentShepherd.ResetPath();
                else
                    return; // Cannot build since the tile is not next to the shepherd or the last tile
            }

            // Finally, if the last tile on the path is an entrance to the ark
            // then they cannot build any more. They would have to return to the last
            // tile before the entrance to resume building
            if (lastTile != null && lastTile.GetComponent<ArkEntranceTile>() != null)
                return;
        }

        // Using last index since we could have multiple tiles on the same node
        // But the "LAST" tile is the one that must be selected first
        var lastIndex = currentShepherd.Path.LastIndexOf(lastTile);
        var tileIndex = currentShepherd.Path.LastIndexOf(tile);

        // There is no last tile
        // Or this new tile does not already exist in the path
        if (lastTile == null || tileIndex < 0)
            currentShepherd.AddPathTile(tile);

        // The player has clicked on a tile that is already part of their path
        else if (tileIndex < lastIndex)
        {
            // if this tile is NOT a neighbor to the last tile
            // OR it is a neighbor but the previous tile
            // Then the player is trying to remove the tile
            var previousTile = currentShepherd.Path[lastIndex - 1];
            if (tile == previousTile)
            {
                var fromTile = currentShepherd.Path[tileIndex + 1];
                currentShepherd.RemovePathTilesFrom(fromTile);
            }
            else if (!lastTile.HasNeighbor(tile))
            {
                // To avoid the player accidentantly removing their path
                // while they are drawing it we will prevent the removal
                // of tracks from anywhere other than the last tile unless
                // they are in edit mode. 
                if (LevelMode != LevelMode.Edit)
                    return;

                var fromTile = currentShepherd.Path[tileIndex + 1];
                currentShepherd.RemovePathTilesFrom(fromTile);
            }
            else
            {
                var tileType = tile.GetPathTypeOwnedByShepherd(currentShepherd);
                var isTileStraight = tile.IsTileOwnedByShepherdStraight(currentShepherd);

                // The player is trying to make a CROSS
                // The tile MUST be a straight path otherwise we will ignore it
                if (tileType != PathType.Empty && !isTileStraight)
                    return;

                //Debug.Log($"{name} is attempting to cross over from {lastTile.name} to {tile.name} which is owned by {tile.GetOwner()}");

                // If the last tile is already a CROSS
                // then the player must move in the same direction
                // as when the previous CROSS was made or else we cannot 
                // create a new cross
                var lastTileType = lastTile.GetPathTypeOwnedByShepherd(currentShepherd);
                if (lastTileType == PathType.Cross)
                {
                    var secondToLastTile = currentShepherd.Path[lastIndex - 1];
                    var prevDir = (lastTile.Position - secondToLastTile.Position).normalized;
                    var newDir = (tile.Position - lastTile.Position).normalized;

                    // Not moving in the same direction from a "CROSS" so cannot create another cross
                    if (prevDir != newDir)
                        return;
                }

                tile.UpdatePathType(currentShepherd, PathType.Cross);
                currentShepherd.AddPathTile(tile);
            }
        }

        // Just making sure that the shephered is not highlighting neighborgs when it does not need to
        if (currentShepherd != null && currentShepherd.Path.Count > 0)
            currentShepherd.DisableHighlights();

        // Since the player has clicked on a tile
        // We will assume they want to "DRAW" so we will leave this in draw mode
        // It switches to EDIT when the player released the LMB
        ChangeMode(LevelMode.Drawing);
        UpdatePathSprites(currentShepherd, currentShepherd.Path.Count - 1);
    }

    public bool IsMovementFromCrossOverAllowed(PathTile tile)
    {
        // Let's make sure the path will be connected to the last tile on the
        // current path by ensuring they are neighbors
        var finalTile = currentShepherd.Path.Last();
        if (!tile.HasNeighbor(finalTile))
            return false;

        // The following only applies if the LAST tile on the current shepherd's path
        // is already crossing another because we want to prevent the player from
        // changing the direction of the cross section
        // Therefore if the final tile is double booked then we need to do this logic
        if (finalTile.IsDoubleBooked)
        {
            // Because the tile that we are crossing over does not belong to the current shepherd
            // The logic later that determines if the player is still moving in the same direction
            // when attempting to cross over an existing tile will not work since the tile exist 
            // in someone else's path.
            // Instead, we need to determine here if the move is legal

            // First, we want to know the previous direction 
            // Which we will default to be from the shepherd's starting position
            var previousDirection = currentShepherd.Position - finalTile.Position;

            // We need to prevent the playe from turning on a cross
            // since they are only allowed to move forward through it
            // We can determine this by ensuring that their previous and new directions
            // are the same 

            // Let's grab the direction the player is intending to move
            var newDirection = finalTile.Position - tile.Position;

            // If the player has more than one tile then we need to base
            // the previous direction off the tile before the final tile
            if (currentShepherd.Path.Count > 1)
            {
                var finalIndex = currentShepherd.Path.LastIndexOf(finalTile);
                var tileBeforeFinal = currentShepherd.Path[finalIndex - 1];
                previousDirection = tileBeforeFinal.Position - finalTile.Position;
            }

            // Directions do not match therefore the player cannot place the tile
            if (newDirection != previousDirection)
                return false;
        }

        return true;
    }

    private void UpdatePathSprites(ShepherdTile shepherd, int fromIndex)
    {
        // Now we need to update:
        //  - The last tile
        //  - The tile before that
        // but to do so we need to know what the previous and next position
        // relative the tile we are working on are
        for (int i = fromIndex; i >= 0; i--)
        {
            var curTile = shepherd.Path[i];

            // Best if we make sure no neighobrs are highlighted
            curTile.RemoveHighlightFromNeighbors();

            // ignore crosses since we already updated their type
            var tileType = curTile.GetPathTypeOwnedByShepherd(shepherd);
            if (tileType == PathType.Cross)
                continue;

            // Default prev based on shepherds position in case this is the only tile
            var prev = (shepherd.Position - curTile.Position).normalized;
            var next = Vector2.zero;

            // We have a tile after this one so we can get the next
            if (i + 1 < shepherd.Path.Count)
            {
                var nextTile = shepherd.Path[i + 1];
                next = (nextTile.Position - curTile.Position).normalized;
            }

            // We have a tile before this one so we can get the previous
            if (i - 1 >= 0)
            {
                var prevTile = shepherd.Path[i - 1];
                prev = (prevTile.Position - curTile.Position).normalized;
            }

            curTile.EnableBorder = false;
            curTile.UpdateSpriteBasedOnNeigborsPositon(prev, next, shepherd);
        }

        // Now we can highlight the last tile on the list
        // Or the shephered when we don't have any
        var lastTile = shepherd.Path.LastOrDefault();
        if (lastTile == null)
        {
            // shepherd.EnableBorder = true;
            shepherd.HighlightNeighbors();
        }
        else
        {
            // Don't highlight if this is an entrance
            if (lastTile.GetComponent<ArkEntranceTile>() == null)
            {
                lastTile.EnableBorder = true;
                lastTile.HighlightNeighbors();
            }
        }
    }
}
