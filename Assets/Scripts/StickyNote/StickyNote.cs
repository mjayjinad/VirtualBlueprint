using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class StickyNote : MonoBehaviour
{
    [SerializeField] private TMP_InputField noteInputField;

    [Header("Spawn Offset from Camera")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.3f, -0.2f, 0.7f); // Adjust as needed

    [Header("Rotation Offset (Euler angles)")]
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;  // Rotation offset in degrees

    private float lerpDuration = 0.5f;
    private Transform cameraTransform;
    private Transform xrRigTransform;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Coroutine lerpCoroutine;

    private void Awake()
    {
        cameraTransform = Camera.main.transform;
        xrRigTransform = FindFirstObjectByType<XROrigin>().transform;

        noteInputField.onSelect.AddListener(OnInputFieldSelected);
    }

    public string GetNoteText() => noteInputField?.text ?? "";

    public void SetNoteText(string text)
    {
        if (noteInputField != null)
            noteInputField.text = text;
    }

    private void OnInputFieldSelected(string text)
    {
        BeginEdit();
    }

    public void OnEndEdit()
    {
        transform.parent = null;
        transform.SetPositionAndRotation(initialPosition,initialRotation);
    }

    private void BeginEdit()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        Vector3 targetPos =
            cameraTransform.position +
            cameraTransform.right * spawnOffset.x +
            cameraTransform.forward * spawnOffset.z +
            Vector3.up * spawnOffset.y;

        // Calculate look rotation to face the camera (same as spawner)
        Quaternion lookRotation = Quaternion.LookRotation(targetPos - cameraTransform.position);

        // Apply rotation offset as quaternion from Euler angles
        Quaternion rotationOffset = Quaternion.Euler(rotationOffsetEuler);

        Quaternion targetRot = lookRotation * rotationOffset;

        if (lerpCoroutine != null)
            StopCoroutine(lerpCoroutine);

        // Parent to XR Rig for smooth follow
        transform.SetParent(xrRigTransform);

        lerpCoroutine = StartCoroutine(LerpToPositionRotation(transform.position, targetPos, transform.rotation, targetRot, lerpDuration));
    }

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

        transform.SetPositionAndRotation(targetPos, targetRot);
        lerpCoroutine = null;
    }

    private void OnDestroy()
    {
        noteInputField.onSelect.RemoveListener(OnInputFieldSelected);
    }
}