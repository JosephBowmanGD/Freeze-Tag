using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    public bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool shouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAniomation && characterController.isGrounded;

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canDoHeadbob = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool useFootSteps = true;
    [SerializeField] private bool useStamina = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode zoomKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode interactKey = KeyCode.Mouse0;

    [Header("Movement Parametres")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchingSpeed = 1f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Health Parametres")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float timeBeforeStartRegenerating = 5;
    [SerializeField] private float healthValueIncrement = 1;
    [SerializeField] private float healthtimeIncrement = 0.1f;
    private float currentHealth;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

    [Header("Stamina Parametres")]
    [SerializeField] private float maxStamina = 100;
    [SerializeField] private float staminaUseMultiplier = 5;
    [SerializeField] private float timeBeforeStaminaRegenerating = 5;
    [SerializeField] private float StaminaValueIncrement = 2;
    [SerializeField] private float StaminaTimeIncrement = 0.1f;
    private float curentStamina;
    private Coroutine regenaratingStamina;
    public static Action<float> OnStaminaChange;

    [Header("Look Parametres")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float lookLimitUP = 80f;
    [SerializeField, Range(1, 180)] private float lookLimitDN = 80f;

    [Header("Jump Parametres")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30f;

    [Header("Crouch Parametres")]
    [SerializeField] private float crouchHieght = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeTocrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    public bool IsCrouching;
    private bool duringCrouchAniomation;

    [Header("Headbob Parametres")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0f;
    private float timer;

    [Header("Zoom Parametres")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFov;
    private Coroutine zoomRoutine;

    [Header("Footstep Parametres")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepSpeed = 1.5f;
    [SerializeField] private float sprintStepSpeed = 0.6f;
    [SerializeField] private AudioSource footStepAudioSource = default;
    [SerializeField] private AudioClip[] concreateClips = default;
    [SerializeField] private AudioClip[] wetFloorClips = default;
    [SerializeField] private AudioClip[] MetalClips = default;
    private float footstepTimer = 0;
    private float getCurrentOffset => IsCrouching ? baseStepSpeed * crouchStepSpeed : IsSprinting ? baseStepSpeed * sprintStepSpeed : baseStepSpeed;


    // sliding parametres
    private Vector3 hitPointNormal;

    public Vector2 currentInput;

    private bool IsSlliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopehit, 2f))
            {
                hitPointNormal = slopehit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }

    [Header("Interaction")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private LayerMask interactionLayerMask = default;
    private Interactables currentInteractible;

    [Header("Game Objects")]
    private Camera playerCamera;
    [SerializeField] private GameObject deathCamera;
    [SerializeField] private GameObject deathPanel;
    private CharacterController characterController;

    [Header("Vectors")]
    private Vector3 moveDirection;

    [Header("Float")]
    [SerializeField] private float RaycastLenght = 2f;
    private float rotationX = 0f;

    private void OnEnable()
    {
        OnTakeDamage += ApplyDamage;
    }

    private void OnDisable()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        defaultFov = playerCamera.fieldOfView;
        currentHealth = maxHealth;
        curentStamina = maxStamina;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();

            if (canJump)
                HandleJump();

            if (canCrouch)
                HandleCrouch();

            if (canDoHeadbob)
                HandleHeadBob();

            if (canZoom)
                HandleZoom();

            if (canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
            }

            if (useFootSteps)
                HandleFootsteps();

            if (useStamina)
                HandleStamina();

            ApplyFinalMovememts();
        }
    }

    private void HandleMovementInput()
    {
        currentInput = new Vector2((IsCrouching ? crouchingSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (IsCrouching ? crouchingSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -lookLimitUP, lookLimitDN);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void HandleJump()
    {
        if (ShouldJump)
            moveDirection.y = jumpForce;
    }

    private void HandleCrouch()
    {
        if (shouldCrouch)
            StartCoroutine(CrouchStand());
    }

    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;

        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (IsCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * (IsCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localRotation.z);
        }
    }

    private void HandleStamina()
    {
        if (IsSprinting && !IsCrouching && currentInput != Vector2.zero)
        {
            if (regenaratingStamina != null)
            {
                StopCoroutine(regenaratingStamina);
                regenaratingStamina = null;
            }

            curentStamina -= staminaUseMultiplier * Time.deltaTime;

            if (curentStamina < 0)
                curentStamina = 0;

            OnStaminaChange?.Invoke(curentStamina);

            if (curentStamina <= 0)
                canSprint = false;
        }
        if (!IsSprinting && curentStamina < maxStamina && regenaratingStamina == null)
        {
            regenaratingStamina = StartCoroutine(Regeneratestamina());
        }
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }
        if (Input.GetKeyUp(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }

    private void HandleInteractionCheck()
    {
        if (Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.gameObject.layer == 6 && (currentInteractible == null || hit.collider.gameObject.GetInstanceID() != currentInteractible.GetInstanceID()))
            {
                hit.collider.TryGetComponent(out currentInteractible);

                if (currentInteractible)
                    currentInteractible.OnFocus();
            }
        }
        else if (currentInteractible)
        {
            currentInteractible.OnLoseFocus();
            currentInteractible = null;
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractible != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayerMask))
        {
            currentInteractible.OnInteract();
        }
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded) return;
        if (currentInput == Vector2.zero) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                switch (hit.collider.tag)
                {
                    case "FootSteps/Metal":
                        footStepAudioSource.PlayOneShot(MetalClips[UnityEngine.Random.Range(0, MetalClips.Length - 1)]);
                        break;
                    case "FootSteps/WetFloor":
                        footStepAudioSource.PlayOneShot(wetFloorClips[UnityEngine.Random.Range(0, wetFloorClips.Length - 1)]);
                        break;
                    case "FootSteps/Concreate":
                        footStepAudioSource.PlayOneShot(concreateClips[UnityEngine.Random.Range(0, concreateClips.Length - 1)]);
                        break;
                    default:
                        footStepAudioSource.PlayOneShot(MetalClips[UnityEngine.Random.Range(0, MetalClips.Length - 1)]);
                        break;
                }
            }

            footstepTimer = getCurrentOffset;

        }
    }

    private void ApplyDamage(float damage)
    {
        currentHealth -= damage;
        OnDamage?.Invoke(currentHealth);

        if (currentHealth <= 0)
            Die();
        else if (regeneratingHealth != null)
            StopCoroutine(regeneratingHealth);

        regeneratingHealth = StartCoroutine(RegenHealth());
    }

    private void Die()
    {
        currentHealth = 0;

        if (regeneratingHealth != null)
            StopCoroutine(regeneratingHealth);

        Destroy(gameObject);
        deathCamera.SetActive(true);
        deathPanel.SetActive(true);
    }

    private void ApplyFinalMovememts()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (willSlideOnSlopes && IsSlliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);

        if (characterController.velocity.y < -1 && characterController.isGrounded)
            moveDirection.y = 0;
    }

    private IEnumerator CrouchStand()
    {
        if (IsCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, RaycastLenght))
        {
            yield break;
        }

        duringCrouchAniomation = true;

        float timeElapsed = 0;
        float targetHieght = IsCrouching ? standingHeight : crouchHieght;
        float currentHieght = characterController.height;
        Vector3 targetCenter = IsCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeTocrouch)
        {
            characterController.height = Mathf.Lerp(currentHieght, targetHieght, timeElapsed / timeTocrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeTocrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHieght;
        characterController.center = targetCenter;

        IsCrouching = !IsCrouching;
        duringCrouchAniomation = false;
    }

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFov = isEnter ? zoomFOV : defaultFov;
        float startingFov = playerCamera.fieldOfView;
        float timeElapsed = 0;

        while (timeElapsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startingFov, targetFov, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFov;
        zoomRoutine = null;
    }

    private IEnumerator RegenHealth()
    {
        yield return new WaitForSeconds(timeBeforeStartRegenerating);
        WaitForSeconds timeToWait = new WaitForSeconds(healthtimeIncrement);

        while (currentHealth < maxHealth)
        {
            currentHealth += healthValueIncrement;

            if (currentHealth > maxHealth)
                currentHealth = maxHealth;

            OnHeal?.Invoke(currentHealth);
            yield return timeToWait;
        }

        regeneratingHealth = null;
    }

    private IEnumerator Regeneratestamina()
    {
        yield return new WaitForSeconds(timeBeforeStaminaRegenerating);
        WaitForSeconds timeToWait = new WaitForSeconds(StaminaTimeIncrement);

        while (curentStamina < maxStamina)
        {
            if (curentStamina > 0)
                canSprint = true;

            curentStamina += StaminaValueIncrement;

            if (curentStamina > maxStamina)
                curentStamina = maxStamina;

            OnStaminaChange?.Invoke(curentStamina);

            yield return timeToWait;
        }

        regenaratingStamina = null;
    }
}
