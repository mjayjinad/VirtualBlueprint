using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class DeleteStickyNote : MonoBehaviour
{
    [Header("Input Action Reference for Delete")]
    [SerializeField] private InputActionReference deleteAction;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor currentInteractor;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        deleteAction.action.Enable();
        deleteAction.action.performed += OnDelete;

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        deleteAction.action.performed -= OnDelete;
        deleteAction.action.Disable();

        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        currentInteractor = null;
    }

    private void OnDelete(InputAction.CallbackContext context)
    {
        if (currentInteractor != null)
        {
            // End manual interaction safely
            if (currentInteractor is XRBaseInteractor baseInteractor)
            {
                baseInteractor.EndManualInteraction();
            }
        }

        Destroy(gameObject);
        Debug.Log($"{gameObject.name} was deleted via DeleteObject script.");
    }
}
