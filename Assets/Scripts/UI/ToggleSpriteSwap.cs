using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSpriteSwap : MonoBehaviour
{
    [Header("Mic UI Elements")]
    [SerializeField] private Image micIconImage;
    [SerializeField] private Sprite micOnSprite;
    [SerializeField] private Sprite micMutedSprite;

    private Toggle toggle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        toggle = GetComponent<Toggle>();

        toggle.onValueChanged.AddListener(SwapSprite);

        InitializeSprite();
    }

    private void InitializeSprite()
    {
        if (toggle.isOn)
        {
            if (micIconImage != null && micMutedSprite != null)
                micIconImage.sprite = micMutedSprite;

            Debug.Log("Mic is off on start");
        }
        else
        {
            if (micIconImage != null && micOnSprite != null)
                micIconImage.sprite = micOnSprite;
        }
    }

    private void SwapSprite(bool toggleValue)
    {
        if (toggleValue)
        {
            if (micIconImage != null && micMutedSprite != null)
                micIconImage.sprite = micMutedSprite;
        }
        else 
        {
            if (micIconImage != null && micOnSprite != null)
                micIconImage.sprite = micOnSprite;
        }
    }
}
