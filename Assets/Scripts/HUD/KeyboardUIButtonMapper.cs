using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public abstract class KeyboardUIButtonMapper : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] KeyCode keyCode;

    Button masterButton;
    protected Button MasterButton
    {
        get
        {
            if (masterButton == null)
                masterButton = GetComponent<Button>();
            return masterButton;
        }
    }


    protected virtual void Update()
    {
        if (GameManager.instance.BlockButtons || !MasterButton.interactable)
            return;

        if (Input.GetKeyDown(keyCode))
            OnPointerClick(null);            
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        OnButtonClicked();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!MasterButton.interactable)
            return;
        AudioManager.instance.PlayClip(SFXLibrary.instance.buttonHoverClip);
    }

    protected virtual void OnButtonClicked()
    {
        if (!MasterButton.interactable)
            return;

        OnButtonPressed();
        AudioManager.instance.PlayClip(SFXLibrary.instance.buttonMenuClip);
    }

    public abstract void OnButtonPressed();

}
