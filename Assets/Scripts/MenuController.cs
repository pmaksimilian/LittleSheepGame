using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private PauseController pauseController;
    void Start()
    {
        menuCanvas.SetActive(false);
    }



    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!menuCanvas.activeSelf && pauseController.IsGamePaused) return;

            menuCanvas.SetActive(!menuCanvas.activeSelf);
            pauseController.SetPause(menuCanvas.activeSelf);
        }
    }
}