using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleMenu : MonoBehaviour
{
    [Header("InputAction Reference")]
    [SerializeField] private InputActionReference menuAction;
    [SerializeField] private GameObject menuObject;

    private bool isOpen = false;

    void Start()
    {
        menuObject.SetActive(false);
    }

    private void OnEnable()
    {
        menuAction.action.performed += ToggelMenu;
        menuAction.action.Enable();
    }

    private void OnDisable()
    {
        menuAction.action.Disable();
        menuAction.action.performed -= ToggelMenu;
    }

    private void ToggelMenu(InputAction.CallbackContext context)
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            menuObject.SetActive(false);
        }
        else
        {
            menuObject.SetActive(true);
        }
    }
}