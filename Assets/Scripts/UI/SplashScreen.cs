using System.Collections;
using UnityEngine;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private GameObject welcomePage;
    [SerializeField] private float waitTime;

    private void Start()
    {
        StartCoroutine(DisableScreen());
    }

    private IEnumerator DisableScreen()
    {
        yield return new WaitForSeconds(waitTime);

        welcomePage.SetActive(true);
        gameObject.SetActive(false);
    }
}
