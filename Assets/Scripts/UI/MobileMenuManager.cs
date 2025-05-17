using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MobileMenuManager : MonoBehaviour
{
    [SerializeField] private Button watchStreamBtn; // Button to start watching the stream
    [SerializeField] private Button closeStreamBtn; // Button to stop watching the stream
    [SerializeField] private GameObject welcomeScreen; // UI panel shown before streaming starts
    [SerializeField] private GameObject streamScreen; // UI panel shown during streaming
    [SerializeField] private TMP_Text errorMessage; // Text element to display error messages
    [SerializeField] private WebRTCManager webRTCManager; // Reference to the WebRTC manager script

    void Start()
    {
        watchStreamBtn.onClick.AddListener(() => LoadStream());
        closeStreamBtn.onClick.AddListener(() => CloseStream());
    }

    /// <summary>
    /// Handles closing the stream and switching UI back to welcome screen
    /// </summary>
    private void CloseStream()
    {
        ToggleOrientation();
        welcomeScreen.SetActive(true);
        streamScreen.SetActive(false);
    }

    /// <summary>
    /// Handles loading and starting the video stream
    /// </summary>
    private void LoadStream()
    {
        LoadErrorMessage(); // Check for errors before starting stream
        if (errorMessage.text != "") // If there is an error message, do not proceed
            return;

        webRTCManager.StartVideoAudio();
        ToggleOrientation();
        streamScreen.SetActive(true);
        welcomeScreen.SetActive(false);
    }

    /// <summary>
    /// Retrieves the video stream status and updates the error message UI
    /// </summary>
    private void LoadErrorMessage()
    {
        errorMessage.text = webRTCManager.OnVideoStreamStarted();
    }

    /// <summary>
    /// Toggles between portrait and landscape screen orientations
    /// </summary>
    public void ToggleOrientation()
    {
        if (Screen.orientation == ScreenOrientation.Portrait ||
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        else
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
