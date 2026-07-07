using UnityEngine;
using UnityEngine.InputSystem;

public class CustomPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float groundDrag = 8f;
    [SerializeField] private float jumpForce = 6f;
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

    private Rigidbody rb;
    private bool grounded;
    private bool readyToJump = true;
    private Vector2 moveInput;
    private bool jumpInput;

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
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
        Debug.Log($"CustomPlayerMovement Start - Rigidbody found: {rb != null}, isKinematic: {rb.isKinematic}");
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down,
                                   playerHeight * 0.5f + 0.2f, groundLayer);

        //Debug.Log($"isKinematic: {rb.isKinematic}, grounded: {grounded}, position Y: {transform.position.y}");

        rb.linearDamping = grounded ? groundDrag : 0f;

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
    }

    private void MovePlayer()
    {
        Vector3 moveDir = orientation.forward * moveInput.y +
                          orientation.right * moveInput.x;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude < moveSpeed)
        {
            float forceMult = grounded ? 10f : 10f * airMultiplier;
            rb.AddForce(moveDir.normalized * moveSpeed * forceMult, ForceMode.Force);
        }
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