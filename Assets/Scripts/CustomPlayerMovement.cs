using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class CustomPlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float groundDrag = 8f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airMultiplier = 0.4f;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Stair Handling")]
    [SerializeField] private float stepHeight = 0.4f;
    [SerializeField] private float stepSmooth = 0.1f;

    [Header("References")]
    [SerializeField] private Transform orientation;

    [Header("Body Rotation")]
    [Tooltip("How quickly the player body turns to face the camera's direction. Higher = snappier.")]
    [SerializeField] private float bodyRotationSpeed = 12f;

    private Rigidbody rb;
    private bool grounded;
    private bool readyToJump = true;
    private Vector2 moveInput;
    private bool jumpInput;

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        //Debug.Log($"OnMove received: {moveInput}");
    }
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            if (grounded && readyToJump)
                jumpInput = true;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // This is a copy of ANOTHER player's object being observed on our machine -
            // it must never process input or run movement logic locally, or its PlayerInput
            // component can cross-wire with our own local keyboard/device input.
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down,
                                   playerHeight * 0.5f + 0.2f, groundLayer);

        // Full stop when no input and grounded
        if (moveInput == Vector2.zero && grounded)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.linearDamping = 0f;
        }
        else
        {
            rb.linearDamping = grounded ? groundDrag : 0f;
        }

        if (jumpInput && readyToJump && grounded)
        {
            jumpInput = false;
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        SpeedControl();
    }

    private void FixedUpdate()
    {
        //Debug.Log("FixedUpdate firing");
        StepClimb();
        MovePlayer();
        RotateBodyToCamera();
    }

    private void MovePlayer()
    {
        Vector3 moveDir = orientation.forward * moveInput.y +
                          orientation.right * moveInput.x;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (grounded)
        {
            if (flatVel.magnitude < moveSpeed)
            {
                rb.AddForce(moveDir.normalized * moveSpeed * 10f, ForceMode.Force);
            }
        }
        else
        {
            // Air control: always allow steering, even above moveSpeed,
            // but scale it down so it doesn't feel like ground movement
            rb.AddForce(moveDir.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

            // Optional: gentle air braking when no input, so you don't
            // slide forever after releasing keys mid-jump
            if (moveInput == Vector2.zero)
            {
                Vector3 airBrake = -flatVel * airMultiplier * 0.5f;
                rb.AddForce(airBrake, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// Rotates the player body (and therefore the model) to face the same
    /// yaw direction as the camera/orientation transform, so the character
    /// visually turns to look where you're aiming.
    /// </summary>
    private float lastOrientationLog;

    private void RotateBodyToCamera()
    {
        if (Time.time - lastOrientationLog > 0.5f)
        {
            lastOrientationLog = Time.time;
            Debug.Log($"[CustomPlayerMovement] orientation.eulerAngles.y = {orientation.eulerAngles.y:F1} | rb.rotation.y = {rb.rotation.eulerAngles.y:F1}");
        }

        Quaternion targetRotation = Quaternion.Euler(0f, orientation.eulerAngles.y, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, bodyRotationSpeed * Time.fixedDeltaTime));
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y,
                                            limitedVel.z);
        }
    }

    private void StepClimb()
    {
        if (moveInput == Vector2.zero) return;

        Vector3 moveDir = orientation.forward * moveInput.y +
                          orientation.right * moveInput.x;

        RaycastHit hitLower;
        if (Physics.Raycast(transform.position, moveDir, out hitLower, 0.5f, groundLayer))
        {
            RaycastHit hitUpper;
            if (!Physics.Raycast(transform.position + new Vector3(0, stepHeight, 0),
                                 moveDir, out hitUpper, 0.6f, groundLayer))
            {
                rb.position += new Vector3(0f, stepSmooth, 0f);
            }
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump() => readyToJump = true;
}
