using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;

    private Vector2 moveInput;

    // dodano zaradi animacij:
    private float lastInputX;
    private float lastInputY;
    private float inputX;
    private float inputY;
    private bool isWalking;
    public float LastInputX() => lastInputX;
    public float LastInputY() => lastInputY;
    public float InputX() => inputX;
    public float InputY() => inputY;
    public bool IsWalking() => isWalking;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {

        rb.linearVelocity = moveInput * moveSpeed;
  
        // --- Update current input axes ---
        inputX = moveInput.x;
        inputY = moveInput.y;
        
        // --- Check if walking ---
        isWalking = moveInput.sqrMagnitude > 0.01f; // avoids floating-point noise
        
        // --- Update last direction only when moving ---
        if (isWalking){
            lastInputX = inputX;
            lastInputY = inputY;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}