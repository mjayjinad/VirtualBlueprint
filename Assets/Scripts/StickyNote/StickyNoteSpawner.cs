using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class StickyNoteSpawner : MonoBehaviour
{
    [Header("Sticky Note Prefab")]
    [SerializeField] private GameObject stickyNotePrefab; // Prefab to instantiate as sticky note

    [Header("Input Action Reference")]
    [SerializeField] private InputActionReference spawnNoteAction; // Input action to trigger spawning

    [Header("Spawn Offset from Camera")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.3f, -0.2f, 0.7f); // Offset relative to camera position

    [Header("Rotation Offset (Euler angles)")]
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero; // Additional rotation offset in degrees

    private Transform cameraTransform; // Reference to main camera transform
    private Transform xrRigTransform; // Reference to XR rig root transform

    private void Awake()
    {
        // Cache main camera transform
        cameraTransform = Camera.main.transform;

        // Find the XR Origin (rig) in the scene and cache its transform
        xrRigTransform = FindFirstObjectByType<XROrigin>().transform;
    }

    private void OnEnable()
    {
        // Subscribe to the spawn note input action
        if (spawnNoteAction != null)
        {
            spawnNoteAction.action.performed += OnSpawnNote;
            spawnNoteAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the input action to avoid memory leaks
        if (spawnNoteAction != null)
        {
            spawnNoteAction.action.performed -= OnSpawnNote;
            spawnNoteAction.action.Disable();
        }
    }

    /// <summary>
    /// Called when the spawn note input action is performed
    /// </summary>
    /// <param name="context"></param>
    private void OnSpawnNote(InputAction.CallbackContext context)
    {
        if (stickyNotePrefab == null)
        {
            Debug.LogWarning("Missing Sticky Note Prefab.");
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera Transform not assigned.");
            return;
        }

        // Calculate spawn position relative to the camera using specified offset
        Vector3 spawnPosition =
            cameraTransform.position +
            cameraTransform.right * spawnOffset.x +
            cameraTransform.forward * spawnOffset.z +
            Vector3.up * spawnOffset.y;

        // Calculate rotation so sticky note faces towards the camera
        Quaternion lookRotation = Quaternion.LookRotation(spawnPosition - cameraTransform.position);

        // Create additional rotation offset from Euler angles
        Quaternion rotationOffset = Quaternion.Euler(rotationOffsetEuler);

        // Combine base look rotation with rotation offset
        Quaternion finalRotation = lookRotation * rotationOffset;

        // Instantiate sticky note prefab at calculated position & rotation, parented to XR rig
        GameObject newNote = Instantiate(stickyNotePrefab, spawnPosition, finalRotation, xrRigTransform);

        // If sticky note contains a TMP_InputField, activate it immediately for user input
        TMP_InputField inputField = newNote.GetComponentInChildren<TMP_InputField>();
        if (inputField != null)
        {
            inputField.ActivateInputField();
        }
    }
}