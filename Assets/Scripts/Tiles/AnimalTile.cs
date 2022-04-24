using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[ExecuteInEditMode]
public class AnimalTile : Tile, IResetable
{
    [SerializeField, Tooltip("The block the animal starts at")] 
    SpriteRenderer blockRenderer;

    [SerializeField] Animator animator;

    /// <summary>
    /// True when the animal has been picked up from their starting location
    /// This is only true to indicate that they are not in their starting position
    /// </summary>
    public bool IsPickedUp { get; set; }

    public void ResetObject()
    {
        IsPickedUp = false;
        Jump(false);
        transform.SetParent(originalParent);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        Position = startingPosition;
    }
    protected override void Start()
    {
        // Cannot run this while in edit mode
        if (!Application.isPlaying)
            return;

        base.Start();

        // Show the block and detach it
        // So that it does not move with the shepherd
        if (blockRenderer != null)
            blockRenderer.transform.SetParent(null);
    }

    protected override void Update()
    {
        if (!Application.isPlaying)
            return;

        var multiplier = LevelController.instance.IsFastForwardOn ? 2 : 1;
        animator?.SetFloat("AnimationSpeed", multiplier);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        animator.SetTrigger("Clicked");
    }

    public void Jump(bool isJumping) => animator.SetBool("Jump", isJumping);
}
