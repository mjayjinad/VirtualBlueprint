using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MobileMenuManager : MonoBehaviour
{
    [SerializeField] private Button watchStreamBtn;
    [SerializeField] private Button closeStreamBtn;
    [SerializeField] private GameObject welcomeScreen;
    [SerializeField] private GameObject streamScreen;
    [SerializeField] private TMP_Text errorMessage;
    [SerializeField] private WebRTCManager webRTCManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        watchStreamBtn.onClick.AddListener(() => LoadStream());  
        closeStreamBtn.onClick.AddListener(() => CloseStream());  
    }

    private void CloseStream()
    {
        ToggleOrientation();
        welcomeScreen.SetActive(true);
        streamScreen.SetActive(false);
    }

    private void LoadStream()
    {
        LoadErrorMessage();
        if (errorMessage.text != "") return;
        webRTCManager.StartVideoAudio();
        ToggleOrientation();
        streamScreen.SetActive(true);
        welcomeScreen.SetActive(false);
    }

    private void LoadErrorMessage()
    {
        errorMessage.text = webRTCManager.OnVideoStreamStarted();
    }

    public void ToggleOrientation()
    {
        if (Screen.orientation == ScreenOrientation.Portrait ||
            Screen.orientation == ScreenOrientation.PortraitUpsideDown)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft; // or LandscapeRight
            Debug.Log("Switched to Landscape");
        }
        else
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Debug.Log("Switched to Portrait");
        }
    }
}
