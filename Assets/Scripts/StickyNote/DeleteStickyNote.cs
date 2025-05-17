using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class DeleteStickyNote : MonoBehaviour
{
    [Header("Input Action Reference for Delete")]
    [SerializeField] private InputActionReference deleteAction; // Input action for deleting the note

    private XRGrabInteractable grabInteractable; // Reference to XRGrabInteractable component
    private IXRSelectInteractor currentInteractor; // Reference to the current interactor grabbing the note

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        // Enable the delete input action and subscribe to its performed event
        deleteAction.action.Enable();
        deleteAction.action.performed += OnDelete;

        // Subscribe to grab and release events to track current interactor
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        // Unsubscribe from input action and grab/release events to avoid memory leaks
        deleteAction.action.performed -= OnDelete;
        deleteAction.action.Disable();

        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    /// <summary>
    /// Called when the sticky note is grabbed
    /// </summary>
    /// <param name="args"></param>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Store the interactor that grabbed this object
        currentInteractor = args.interactorObject;
    }

    /// <summary>
    /// Called when the sticky note is released
    /// </summary>
    /// <param name="args"></param>
    private void OnReleased(SelectExitEventArgs args)
    {
        // Clear the stored interactor reference
        currentInteractor = null;
    }

    /// <summary>
    /// Called when the delete action is performed
    /// </summary>
    /// <param name="context"></param>
    private void OnDelete(InputAction.CallbackContext context)
    {
        // Only allow deletion if the object is currently grabbed
        if (currentInteractor != null)
        {
            // End manual interaction to avoid lingering grab issues before destroying
            if (currentInteractor is XRBaseInteractor baseInteractor)
            {
                baseInteractor.EndManualInteraction();
            }
        }

        // Destroy the sticky note GameObject
        Destroy(gameObject);
    }
}