using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class StickyNoteSpawner : MonoBehaviour
{
    [Header("Sticky Note Prefab")]
    [SerializeField] private GameObject stickyNotePrefab;

    [Header("Input Action Reference")]
    [SerializeField] private InputActionReference spawnNoteAction;

    [Header("Spawn Offset from Camera")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.3f, -0.2f, 0.7f); // Adjust as needed

    [Header("Rotation Offset (Euler angles)")]
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;  // Rotation offset in degrees

    private Transform cameraTransform;
    private Transform xrRigTransform;

    private void Awake()
    {
        cameraTransform = Camera.main.transform;
        xrRigTransform = FindFirstObjectByType<XROrigin>().transform;
    }

    private void OnEnable()
    {
        if (spawnNoteAction != null)
        {
            spawnNoteAction.action.performed += OnSpawnNote;
            spawnNoteAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (spawnNoteAction != null)
        {
            spawnNoteAction.action.performed -= OnSpawnNote;
            spawnNoteAction.action.Disable();
        }
    }

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

        // Calculate spawn position relative to camera using the offset
        Vector3 spawnPosition =
            cameraTransform.position +
            cameraTransform.right * spawnOffset.x +
            cameraTransform.forward * spawnOffset.z +
            Vector3.up * spawnOffset.y;

        // Face sticky note towards the camera
        Quaternion lookRotation = Quaternion.LookRotation(spawnPosition - cameraTransform.position);

        // Apply rotation offset
        Quaternion rotationOffset = Quaternion.Euler(rotationOffsetEuler);

        Quaternion finalRotation = lookRotation * rotationOffset;

        // Instantiate sticky note at calculated position and rotation, parented to xrRigTransform
        GameObject newNote = Instantiate(stickyNotePrefab, spawnPosition, finalRotation, xrRigTransform);

        // Optional: Activate input field for immediate typing
        TMP_InputField inputField = newNote.GetComponentInChildren<TMP_InputField>();
        if (inputField != null)
        {
            inputField.ActivateInputField();
        }

        Debug.Log($"Sticky Note spawned at {spawnPosition} facing camera with rotation offset.");
    }
}