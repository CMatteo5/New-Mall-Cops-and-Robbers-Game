using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Cinemachine;

public class CustomPlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float groundDrag = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float riseMultiplier = 1.5f;
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
    [SerializeField] private float bodyRotationSpeed = 12f;

    [Header("Sprint / Stamina")]
    [Tooltip("Speed multiplier while sprinting (e.g. 1.6 = 60% faster).")]
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("Stamina drained per second while sprinting.")]
    [SerializeField] private float staminaDrainRate = 25f;
    [Tooltip("Stamina regenerated per second while not sprinting.")]
    [SerializeField] private float staminaRegenRate = 15f;
    [Tooltip("Seconds to wait after you stop sprinting before regen begins.")]
    [SerializeField] private float staminaRegenDelay = 1f;
    [Tooltip("Name of the UI Slider in the scene used as the on-screen stamina bar. Found automatically at spawn, like InteractPrompt/MoneyText.")]
    [SerializeField] private string staminaBarObjectName = "StaminaBar";

    [Header("Shop")]
    [SerializeField] private GameObject buyTerminal;

    private Rigidbody rb;
    private bool grounded;
    private bool readyToJump = true;
    private Vector2 moveInput;
    private bool jumpInput;
    private bool shopOpen = false;
    private PlayerCanBuy playerCanBuy;

    private float currentStamina;
    private float regenDelayTimer;
    private bool isSprinting;
    private Slider staminaBarSlider;

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed && grounded && readyToJump)
            jumpInput = true;
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;
        if (playerCanBuy == null || !playerCanBuy.playerCanBuy) return;

        if (!shopOpen)
            OpenShop();
        else
            CloseShop();
    }

    public void OpenShop()
    {
        shopOpen = true;
        moveInput = Vector2.zero;
        GameState.SetShopOpen(true);

        // Disable first person camera input
        if (CameraRegistry.FirstPersonCamera != null)
        {
            var controller = CameraRegistry.FirstPersonCamera
                             .GetComponent<CinemachineInputAxisController>();
            if (controller != null) controller.enabled = false;
        }

        if (buyTerminal != null)
        {
            buyTerminal.SetActive(true);
            ShopCloseButton closeBtn = buyTerminal.GetComponentInChildren<ShopCloseButton>();
            if (closeBtn != null) closeBtn.Initialize(this);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        shopOpen = false;
        GameState.SetShopOpen(false);

        // Re-enable first person camera input only if in first person
        if (CameraRegistry.FirstPersonCamera != null)
        {
            var controller = CameraRegistry.FirstPersonCamera
                             .GetComponent<CinemachineInputAxisController>();
            if (controller != null) controller.enabled = true;
        }

        if (buyTerminal != null) buyTerminal.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (!IsOwner)
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
            enabled = false;
            return;
        }

        // Owner only setup
        rb.freezeRotation = true;
        playerCanBuy = GetComponent<PlayerCanBuy>();

        if (buyTerminal != null) buyTerminal.SetActive(false);

        // Cursor lock is no longer set here - GameTimer now controls cursor state
        // based on the current game phase (unlocked during start/end screens, locked
        // once gameplay is InProgress), so it doesn't fight with the start screen.

        currentStamina = maxStamina;

        GameObject staminaObj = GameObject.Find(staminaBarObjectName);
        if (staminaObj != null) staminaBarSlider = staminaObj.GetComponent<Slider>();
        UpdateStaminaUI();
    }

    private void Update()
    {
        if (shopOpen)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (GameTimer.CurrentPhase != GamePhase.InProgress)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        grounded = Physics.Raycast(transform.position, Vector3.down,
                                   playerHeight * 0.5f + 0.2f, groundLayer);

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

        UpdateSprintAndStamina();
        SpeedControl();
    }

    private void UpdateSprintAndStamina()
    {
        bool sprintKeyHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        bool wantsToSprint = sprintKeyHeld && moveInput != Vector2.zero && currentStamina > 0f;

        if (wantsToSprint)
        {
            isSprinting = true;
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrainRate * Time.deltaTime);
            regenDelayTimer = staminaRegenDelay;
        }
        else
        {
            isSprinting = false;

            if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            }
        }

        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaBarSlider != null)
        {
            staminaBarSlider.value = currentStamina / maxStamina;
        }
    }

    private void FixedUpdate()
    {
        if (shopOpen || GameTimer.CurrentPhase != GamePhase.InProgress) return;

        StepClimb();
        MovePlayer();
        RotateBodyToCamera();
        ApplyFallMultiplier();
        ApplyRiseMultiplier();
    }

    private void ApplyRiseMultiplier()
    {
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (riseMultiplier - 1f) * Time.fixedDeltaTime;
    }

    private void ApplyFallMultiplier()
    {
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
    }

    private void MovePlayer()
    {
        Vector3 moveDir = orientation.forward * moveInput.y +
                          orientation.right * moveInput.x;

        float currentMoveSpeed = moveSpeed * (isSprinting ? sprintSpeedMultiplier : 1f);
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (grounded)
        {
            if (flatVel.magnitude < currentMoveSpeed)
                rb.AddForce(moveDir.normalized * currentMoveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDir.normalized * currentMoveSpeed * 10f * airMultiplier, ForceMode.Force);

            if (moveInput == Vector2.zero)
            {
                Vector3 airBrake = -flatVel * airMultiplier * 0.5f;
                rb.AddForce(airBrake, ForceMode.Force);
            }
        }
    }

    private void RotateBodyToCamera()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, orientation.eulerAngles.y, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, bodyRotationSpeed * Time.fixedDeltaTime));
    }

    private void SpeedControl()
    {
        float currentMoveSpeed = moveSpeed * (isSprinting ? sprintSpeedMultiplier : 1f);
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > currentMoveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentMoveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
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
