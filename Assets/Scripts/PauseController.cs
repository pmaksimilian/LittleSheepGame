using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    public bool IsGamePaused { get; private set; } = false;

    public void SetPause(bool pause)
    {
        IsGamePaused = pause;
        if (pause)
        {
            Time.timeScale = 0f;
            playerInput.SwitchCurrentActionMap("UI");
        }
        else
        {
            Time.timeScale = 1f;
            playerInput.SwitchCurrentActionMap("Player");
        }
        
    }
}