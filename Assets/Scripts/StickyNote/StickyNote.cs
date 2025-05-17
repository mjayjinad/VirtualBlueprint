using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

public class StickyNote : MonoBehaviour
{
    [SerializeField] private TMP_InputField noteInputField; // Reference to the input field for note text

    [Header("Spawn Offset from Camera")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.3f, -0.2f, 0.7f); // Position offset relative to camera

    [Header("Rotation Offset (Euler angles)")]
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero; // Rotation offset in degrees

    private float lerpDuration = 0.5f; // Duration of lerp animation in seconds
    private Transform cameraTransform; // Reference to main camera transform
    private Transform xrRigTransform; // Reference to XR rig transform (for parenting)

    private Vector3 initialPosition; // Store initial position before editing
    private Quaternion initialRotation; // Store initial rotation before editing

    private Coroutine lerpCoroutine; // Reference to running lerp coroutine

    private void Awake()
    {
        // Cache main camera transform
        cameraTransform = Camera.main.transform;

        // Find and cache XR rig transform
        xrRigTransform = FindFirstObjectByType<XROrigin>().transform;

        // Register listener for input field selection event
        noteInputField.onSelect.AddListener(OnInputFieldSelected);
    }

    /// <summary>
    /// Returns the current text of the sticky note input field.
    /// </summary>
    public string GetNoteText() => noteInputField?.text ?? "";

    /// <summary>
    /// Sets the text of the sticky note input field.
    /// </summary>
    public void SetNoteText(string text)
    {
        if (noteInputField != null)
            noteInputField.text = text;
    }

    /// <summary>
    /// Called when the input field is selected (focused). Begins the editing process.
    /// </summary>
    private void OnInputFieldSelected(string text)
    {
        BeginEdit();
    }

    /// <summary>
    /// Called when editing ends.
    /// Resets the note's transform to its original position and detaches from parent.
    /// </summary>
    public void OnEndEdit()
    {
        // Detach from parent (xrRigTransform)
        transform.parent = null;

        // Restore initial position and rotation
        transform.SetPositionAndRotation(initialPosition, initialRotation);
    }

    /// <summary>
    /// Starts the editing state by smoothly moving the sticky note
    /// in front of the camera with proper rotation.
    /// </summary>
    private void BeginEdit()
    {
        // Save current position and rotation for later restoration
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Calculate target position relative to the camera using the spawn offset
        Vector3 targetPos =
            cameraTransform.position +
            cameraTransform.right * spawnOffset.x +
            cameraTransform.forward * spawnOffset.z +
            Vector3.up * spawnOffset.y;

        // Calculate rotation so the note faces the camera
        Quaternion lookRotation = Quaternion.LookRotation(targetPos - cameraTransform.position);

        // Apply additional rotation offset
        Quaternion rotationOffset = Quaternion.Euler(rotationOffsetEuler);

        // Combine look rotation and rotation offset to get final target rotation
        Quaternion targetRot = lookRotation * rotationOffset;

        // Stop any existing lerp coroutine before starting a new one
        if (lerpCoroutine != null)
            StopCoroutine(lerpCoroutine);

        // Parent the sticky note to XR rig for smooth following of user movement
        transform.SetParent(xrRigTransform);

        // Start coroutine to smoothly lerp position and rotation over time
        lerpCoroutine = StartCoroutine(LerpToPositionRotation(transform.position, targetPos, transform.rotation, targetRot, lerpDuration));
    }

    /// <summary>
    /// Coroutine that smoothly interpolates position and rotation of the sticky note.
    /// </summary>
    private IEnumerator LerpToPositionRotation(Vector3 startPos, Vector3 targetPos, Quaternion startRot, Quaternion targetRot, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final position and rotation are set precisely at the end
        transform.SetPositionAndRotation(targetPos, targetRot);
        lerpCoroutine = null;
    }

    /// <summary>
    /// Remove listener when this component is destroyed
    /// </summary>
    private void OnDestroy()
    {
        noteInputField.onSelect.RemoveListener(OnInputFieldSelected);
    }
}
