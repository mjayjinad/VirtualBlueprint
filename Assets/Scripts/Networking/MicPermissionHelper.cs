using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class MicCameraPermissionHelper : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID
        // Request Microphone Permission
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        // Request Camera Permission
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }

        // Log available microphones for debugging
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Microphone device found: " + device);
        }
#endif
    }
}