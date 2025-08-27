using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(Rigidbody))]
public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Vector2 moveInput;
    private int points = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        // Make sure your action is literally named "Move" in the Input Actions asset.
        moveAction = playerInput.actions["Move"];
    }

    void OnEnable()
    {
        // Enable and subscribe to get updates when pressed/released.
        moveAction.Enable();
        moveAction.performed += OnMove;
        moveAction.canceled  += OnMove;
    }

    void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled  -= OnMove;
        moveAction.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>(); // (x,y)
        // Uncomment to verify input is coming through
        // Debug.Log($"Move: {moveInput}");
    }

    void FixedUpdate()
    {
        // Convert 2D input (x,y) → world (x,0,z)
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    public void IncreasePoints(int amount) {  
        points += amount;
    }
}